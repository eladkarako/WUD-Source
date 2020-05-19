﻿namespace ICSharpCode.SharpZipLib.BZip2
{
    using ICSharpCode.SharpZipLib.Checksums;
    using System;
    using System.IO;

    public class BZip2OutputStream : Stream
    {
        private int allowableBlockSize;
        private Stream baseStream;
        private byte[] block;
        private uint blockCRC;
        private bool blockRandomised;
        private int blockSize100k;
        private int bsBuff;
        private int bsLive;
        private int bytesOut;
        private const int CLEARMASK = -2097153;
        private uint combinedCRC;
        private int currentChar;
        private const int DEPTH_THRESH = 10;
        private bool disposed_;
        private bool firstAttempt;
        private int[] ftab;
        private const int GREATER_ICOST = 15;
        private readonly int[] increments;
        private bool[] inUse;
        private bool isStreamOwner;
        private int last;
        private const int LESSER_ICOST = 0;
        private IChecksum mCrc;
        private int[] mtfFreq;
        private int nBlocksRandomised;
        private int nInUse;
        private int nMTF;
        private int origPtr;
        private const int QSORT_STACK_SIZE = 0x3e8;
        private int[] quadrant;
        private int runLength;
        private char[] selector;
        private char[] selectorMtf;
        private char[] seqToUnseq;
        private const int SETMASK = 0x200000;
        private const int SMALL_THRESH = 20;
        private short[] szptr;
        private char[] unseqToSeq;
        private int workDone;
        private int workFactor;
        private int workLimit;
        private int[] zptr;

        public BZip2OutputStream(Stream stream) : this(stream, 9)
        {
        }

        public BZip2OutputStream(Stream stream, int blockSize)
        {
            this.increments = new int[] { 1, 4, 13, 40, 0x79, 0x16c, 0x445, 0xcd0, 0x2671, 0x7354, 0x159fd, 0x40df8, 0xc29e9, 0x247dbc };
            this.isStreamOwner = true;
            this.mCrc = new StrangeCRC();
            this.inUse = new bool[0x100];
            this.seqToUnseq = new char[0x100];
            this.unseqToSeq = new char[0x100];
            this.selector = new char[0x4652];
            this.selectorMtf = new char[0x4652];
            this.mtfFreq = new int[0x102];
            this.currentChar = -1;
            this.BsSetStream(stream);
            this.workFactor = 50;
            if (blockSize > 9)
            {
                blockSize = 9;
            }
            if (blockSize < 1)
            {
                blockSize = 1;
            }
            this.blockSize100k = blockSize;
            this.AllocateCompressStructures();
            this.Initialize();
            this.InitBlock();
        }

        private void AllocateCompressStructures()
        {
            int num = 0x186a0 * this.blockSize100k;
            this.block = new byte[(num + 1) + 20];
            this.quadrant = new int[num + 20];
            this.zptr = new int[num];
            this.ftab = new int[0x10001];
            if (((this.block != null) && (this.quadrant != null)) && (this.zptr != null))
            {
                int[] ftab = this.ftab;
            }
            this.szptr = new short[2 * num];
        }

        private void BsFinishedWithStream()
        {
            while (this.bsLive > 0)
            {
                int num = this.bsBuff >> 0x18;
                this.baseStream.WriteByte((byte) num);
                this.bsBuff = this.bsBuff << 8;
                this.bsLive -= 8;
                this.bytesOut++;
            }
        }

        private void BsPutint(int u)
        {
            this.BsW(8, (u >> 0x18) & 0xff);
            this.BsW(8, (u >> 0x10) & 0xff);
            this.BsW(8, (u >> 8) & 0xff);
            this.BsW(8, u & 0xff);
        }

        private void BsPutIntVS(int numBits, int c)
        {
            this.BsW(numBits, c);
        }

        private void BsPutUChar(int c)
        {
            this.BsW(8, c);
        }

        private void BsSetStream(Stream stream)
        {
            this.baseStream = stream;
            this.bsLive = 0;
            this.bsBuff = 0;
            this.bytesOut = 0;
        }

        private void BsW(int n, int v)
        {
            while (this.bsLive >= 8)
            {
                int num = this.bsBuff >> 0x18;
                this.baseStream.WriteByte((byte) num);
                this.bsBuff = this.bsBuff << 8;
                this.bsLive -= 8;
                this.bytesOut++;
            }
            this.bsBuff |= v << ((0x20 - this.bsLive) - n);
            this.bsLive += n;
        }

        public override void Close()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                base.Dispose(disposing);
                if (!this.disposed_)
                {
                    this.disposed_ = true;
                    if (this.runLength > 0)
                    {
                        this.WriteRun();
                    }
                    this.currentChar = -1;
                    this.EndBlock();
                    this.EndCompression();
                    this.Flush();
                }
            }
            finally
            {
                if (disposing && this.IsStreamOwner)
                {
                    this.baseStream.Close();
                }
            }
        }

        private void DoReversibleTransformation()
        {
            this.workLimit = this.workFactor * this.last;
            this.workDone = 0;
            this.blockRandomised = false;
            this.firstAttempt = true;
            this.MainSort();
            if ((this.workDone > this.workLimit) && this.firstAttempt)
            {
                this.RandomiseBlock();
                this.workLimit = this.workDone = 0;
                this.blockRandomised = true;
                this.firstAttempt = false;
                this.MainSort();
            }
            this.origPtr = -1;
            for (int i = 0; i <= this.last; i++)
            {
                if (this.zptr[i] == 0)
                {
                    this.origPtr = i;
                    break;
                }
            }
            if (this.origPtr == -1)
            {
                Panic();
            }
        }

        private void EndBlock()
        {
            if (this.last >= 0)
            {
                this.blockCRC = (uint) this.mCrc.Value;
                this.combinedCRC = (this.combinedCRC << 1) | (this.combinedCRC >> 0x1f);
                this.combinedCRC ^= this.blockCRC;
                this.DoReversibleTransformation();
                this.BsPutUChar(0x31);
                this.BsPutUChar(0x41);
                this.BsPutUChar(0x59);
                this.BsPutUChar(0x26);
                this.BsPutUChar(0x53);
                this.BsPutUChar(0x59);
                this.BsPutint((int) this.blockCRC);
                if (this.blockRandomised)
                {
                    this.BsW(1, 1);
                    this.nBlocksRandomised++;
                }
                else
                {
                    this.BsW(1, 0);
                }
                this.MoveToFrontCodeAndSend();
            }
        }

        private void EndCompression()
        {
            this.BsPutUChar(0x17);
            this.BsPutUChar(0x72);
            this.BsPutUChar(0x45);
            this.BsPutUChar(0x38);
            this.BsPutUChar(80);
            this.BsPutUChar(0x90);
            this.BsPutint((int) this.combinedCRC);
            this.BsFinishedWithStream();
        }

        ~BZip2OutputStream()
        {
            this.Dispose(false);
        }

        public override void Flush()
        {
            this.baseStream.Flush();
        }

        private bool FullGtU(int i1, int i2)
        {
            byte num2 = this.block[i1 + 1];
            byte num3 = this.block[i2 + 1];
            if (num2 != num3)
            {
                return (num2 > num3);
            }
            i1++;
            i2++;
            num2 = this.block[i1 + 1];
            num3 = this.block[i2 + 1];
            if (num2 != num3)
            {
                return (num2 > num3);
            }
            i1++;
            i2++;
            num2 = this.block[i1 + 1];
            num3 = this.block[i2 + 1];
            if (num2 != num3)
            {
                return (num2 > num3);
            }
            i1++;
            i2++;
            num2 = this.block[i1 + 1];
            num3 = this.block[i2 + 1];
            if (num2 != num3)
            {
                return (num2 > num3);
            }
            i1++;
            i2++;
            num2 = this.block[i1 + 1];
            num3 = this.block[i2 + 1];
            if (num2 != num3)
            {
                return (num2 > num3);
            }
            i1++;
            i2++;
            num2 = this.block[i1 + 1];
            num3 = this.block[i2 + 1];
            if (num2 != num3)
            {
                return (num2 > num3);
            }
            i1++;
            i2++;
            int num = this.last + 1;
            do
            {
                num2 = this.block[i1 + 1];
                num3 = this.block[i2 + 1];
                if (num2 != num3)
                {
                    return (num2 > num3);
                }
                int num4 = this.quadrant[i1];
                int num5 = this.quadrant[i2];
                if (num4 != num5)
                {
                    return (num4 > num5);
                }
                i1++;
                i2++;
                num2 = this.block[i1 + 1];
                num3 = this.block[i2 + 1];
                if (num2 != num3)
                {
                    return (num2 > num3);
                }
                num4 = this.quadrant[i1];
                num5 = this.quadrant[i2];
                if (num4 != num5)
                {
                    return (num4 > num5);
                }
                i1++;
                i2++;
                num2 = this.block[i1 + 1];
                num3 = this.block[i2 + 1];
                if (num2 != num3)
                {
                    return (num2 > num3);
                }
                num4 = this.quadrant[i1];
                num5 = this.quadrant[i2];
                if (num4 != num5)
                {
                    return (num4 > num5);
                }
                i1++;
                i2++;
                num2 = this.block[i1 + 1];
                num3 = this.block[i2 + 1];
                if (num2 != num3)
                {
                    return (num2 > num3);
                }
                num4 = this.quadrant[i1];
                num5 = this.quadrant[i2];
                if (num4 != num5)
                {
                    return (num4 > num5);
                }
                i1++;
                i2++;
                if (i1 > this.last)
                {
                    i1 -= this.last;
                    i1--;
                }
                if (i2 > this.last)
                {
                    i2 -= this.last;
                    i2--;
                }
                num -= 4;
                this.workDone++;
            }
            while (num >= 0);
            return false;
        }

        private void GenerateMTFValues()
        {
            int num;
            char[] chArray = new char[0x100];
            this.MakeMaps();
            int index = this.nInUse + 1;
            for (num = 0; num <= index; num++)
            {
                this.mtfFreq[num] = 0;
            }
            int num4 = 0;
            int num3 = 0;
            for (num = 0; num < this.nInUse; num++)
            {
                chArray[num] = (char) num;
            }
            for (num = 0; num <= this.last; num++)
            {
                char ch3 = this.unseqToSeq[this.block[this.zptr[num]]];
                int num2 = 0;
                char ch = chArray[num2];
                while (ch3 != ch)
                {
                    num2++;
                    char ch2 = ch;
                    ch = chArray[num2];
                    chArray[num2] = ch2;
                }
                chArray[0] = ch;
                if (num2 == 0)
                {
                    num3++;
                    continue;
                }
                if (num3 <= 0)
                {
                    goto Label_0126;
                }
                num3--;
            Label_00A9:
                switch ((num3 % 2))
                {
                    case 0:
                        this.szptr[num4] = 0;
                        num4++;
                        this.mtfFreq[0]++;
                        break;

                    case 1:
                        this.szptr[num4] = 1;
                        num4++;
                        this.mtfFreq[1]++;
                        break;
                }
                if (num3 >= 2)
                {
                    num3 = (num3 - 2) / 2;
                    goto Label_00A9;
                }
                num3 = 0;
            Label_0126:
                this.szptr[num4] = (short) (num2 + 1);
                num4++;
                this.mtfFreq[num2 + 1]++;
            }
            if (num3 <= 0)
            {
                goto Label_01EC;
            }
            num3--;
        Label_0172:
            switch ((num3 % 2))
            {
                case 0:
                    this.szptr[num4] = 0;
                    num4++;
                    this.mtfFreq[0]++;
                    break;

                case 1:
                    this.szptr[num4] = 1;
                    num4++;
                    this.mtfFreq[1]++;
                    break;
            }
            if (num3 >= 2)
            {
                num3 = (num3 - 2) / 2;
                goto Label_0172;
            }
        Label_01EC:
            this.szptr[num4] = (short) index;
            num4++;
            this.mtfFreq[index]++;
            this.nMTF = num4;
        }

        private static void HbAssignCodes(int[] code, char[] length, int minLen, int maxLen, int alphaSize)
        {
            int num = 0;
            for (int i = minLen; i <= maxLen; i++)
            {
                for (int j = 0; j < alphaSize; j++)
                {
                    if (length[j] == i)
                    {
                        code[j] = num;
                        num++;
                    }
                }
                num = num << 1;
            }
        }

        private static void HbMakeCodeLengths(char[] len, int[] freq, int alphaSize, int maxLen)
        {
            int[] numArray = new int[260];
            int[] numArray2 = new int[0x204];
            int[] numArray3 = new int[0x204];
            for (int i = 0; i < alphaSize; i++)
            {
                numArray2[i + 1] = ((freq[i] == null) ? 1 : freq[i]) << 8;
            }
            while (true)
            {
                int num5;
                int index = alphaSize;
                int num2 = 0;
                numArray[0] = 0;
                numArray2[0] = 0;
                numArray3[0] = -2;
                for (int j = 1; j <= alphaSize; j++)
                {
                    numArray3[j] = -1;
                    num2++;
                    numArray[num2] = j;
                    int num9 = num2;
                    int num10 = numArray[num9];
                    while (numArray2[num10] < numArray2[numArray[num9 >> 1]])
                    {
                        numArray[num9] = numArray[num9 >> 1];
                        num9 = num9 >> 1;
                    }
                    numArray[num9] = num10;
                }
                if (num2 >= 260)
                {
                    Panic();
                }
                while (num2 > 1)
                {
                    int num3 = numArray[1];
                    numArray[1] = numArray[num2];
                    num2--;
                    int num11 = 1;
                    int num12 = 0;
                    int num13 = numArray[num11];
                Label_00E7:
                    num12 = num11 << 1;
                    if (num12 <= num2)
                    {
                        if ((num12 < num2) && (numArray2[numArray[num12 + 1]] < numArray2[numArray[num12]]))
                        {
                            num12++;
                        }
                        if (numArray2[num13] >= numArray2[numArray[num12]])
                        {
                            numArray[num11] = numArray[num12];
                            num11 = num12;
                            goto Label_00E7;
                        }
                    }
                    numArray[num11] = num13;
                    int num4 = numArray[1];
                    numArray[1] = numArray[num2];
                    num2--;
                    num11 = 1;
                    num12 = 0;
                    num13 = numArray[num11];
                Label_0155:
                    num12 = num11 << 1;
                    if (num12 <= num2)
                    {
                        if ((num12 < num2) && (numArray2[numArray[num12 + 1]] < numArray2[numArray[num12]]))
                        {
                            num12++;
                        }
                        if (numArray2[num13] >= numArray2[numArray[num12]])
                        {
                            numArray[num11] = numArray[num12];
                            num11 = num12;
                            goto Label_0155;
                        }
                    }
                    numArray[num11] = num13;
                    index++;
                    numArray3[num3] = numArray3[num4] = index;
                    numArray2[index] = ((int) ((numArray2[num3] & 0xffffff00L) + (numArray2[num4] & 0xffffff00L))) | (1 + (((numArray2[num3] & 0xff) > (numArray2[num4] & 0xff)) ? (numArray2[num3] & 0xff) : (numArray2[num4] & 0xff)));
                    numArray3[index] = -1;
                    num2++;
                    numArray[num2] = index;
                    num11 = num2;
                    num13 = numArray[num11];
                    while (numArray2[num13] < numArray2[numArray[num11 >> 1]])
                    {
                        numArray[num11] = numArray[num11 >> 1];
                        num11 = num11 >> 1;
                    }
                    numArray[num11] = num13;
                }
                if (index >= 0x204)
                {
                    Panic();
                }
                bool flag = false;
                for (int k = 1; k <= alphaSize; k++)
                {
                    num5 = 0;
                    int num6 = k;
                    while (numArray3[num6] >= 0)
                    {
                        num6 = numArray3[num6];
                        num5++;
                    }
                    len[k - 1] = (char) num5;
                    if (num5 > maxLen)
                    {
                        flag = true;
                    }
                }
                if (!flag)
                {
                    return;
                }
                for (int m = 1; m < alphaSize; m++)
                {
                    num5 = numArray2[m] >> 8;
                    num5 = 1 + (num5 / 2);
                    numArray2[m] = num5 << 8;
                }
            }
        }

        private void InitBlock()
        {
            this.mCrc.Reset();
            this.last = -1;
            for (int i = 0; i < 0x100; i++)
            {
                this.inUse[i] = false;
            }
            this.allowableBlockSize = (0x186a0 * this.blockSize100k) - 20;
        }

        private void Initialize()
        {
            this.bytesOut = 0;
            this.nBlocksRandomised = 0;
            this.BsPutUChar(0x42);
            this.BsPutUChar(90);
            this.BsPutUChar(0x68);
            this.BsPutUChar(0x30 + this.blockSize100k);
            this.combinedCRC = 0;
        }

        private void MainSort()
        {
            int num;
            int[] numArray = new int[0x100];
            int[] numArray2 = new int[0x100];
            bool[] flagArray = new bool[0x100];
            for (num = 0; num < 20; num++)
            {
                this.block[(this.last + num) + 2] = this.block[(num % (this.last + 1)) + 1];
            }
            for (num = 0; num <= (this.last + 20); num++)
            {
                this.quadrant[num] = 0;
            }
            this.block[0] = this.block[this.last + 1];
            if (this.last < 0xfa0)
            {
                for (num = 0; num <= this.last; num++)
                {
                    this.zptr[num] = num;
                }
                this.firstAttempt = false;
                this.workDone = this.workLimit = 0;
                this.SimpleSort(0, this.last, 0);
            }
            else
            {
                int num2;
                int num6;
                int num7 = 0;
                for (num = 0; num <= 0xff; num++)
                {
                    flagArray[num] = false;
                }
                for (num = 0; num <= 0x10000; num++)
                {
                    this.ftab[num] = 0;
                }
                int index = this.block[0];
                for (num = 0; num <= this.last; num++)
                {
                    num6 = this.block[num + 1];
                    this.ftab[(index << 8) + num6]++;
                    index = num6;
                }
                for (num = 1; num <= 0x10000; num++)
                {
                    this.ftab[num] += this.ftab[num - 1];
                }
                index = this.block[1];
                for (num = 0; num < this.last; num++)
                {
                    num6 = this.block[num + 2];
                    num2 = (index << 8) + num6;
                    index = num6;
                    this.ftab[num2]--;
                    this.zptr[this.ftab[num2]] = num;
                }
                num2 = (this.block[this.last + 1] << 8) + this.block[1];
                this.ftab[num2]--;
                this.zptr[this.ftab[num2]] = this.last;
                num = 0;
                while (num <= 0xff)
                {
                    numArray[num] = num;
                    num++;
                }
                int num9 = 1;
                do
                {
                    num9 = (3 * num9) + 1;
                }
                while (num9 <= 0x100);
                do
                {
                    num9 /= 3;
                    num = num9;
                    while (num <= 0xff)
                    {
                        int num8 = numArray[num];
                        num2 = num;
                        while ((this.ftab[(numArray[num2 - num9] + 1) << 8] - this.ftab[numArray[num2 - num9] << 8]) > (this.ftab[(num8 + 1) << 8] - this.ftab[num8 << 8]))
                        {
                            numArray[num2] = numArray[num2 - num9];
                            num2 -= num9;
                            if (num2 <= (num9 - 1))
                            {
                                break;
                            }
                        }
                        numArray[num2] = num8;
                        num++;
                    }
                }
                while (num9 != 1);
                for (num = 0; num <= 0xff; num++)
                {
                    int num3 = numArray[num];
                    num2 = 0;
                    while (num2 <= 0xff)
                    {
                        int num4 = (num3 << 8) + num2;
                        if ((this.ftab[num4] & 0x200000) != 0x200000)
                        {
                            int loSt = this.ftab[num4] & -2097153;
                            int hiSt = (this.ftab[num4 + 1] & -2097153) - 1;
                            if (hiSt > loSt)
                            {
                                this.QSort3(loSt, hiSt, 2);
                                num7 += (hiSt - loSt) + 1;
                                if ((this.workDone > this.workLimit) && this.firstAttempt)
                                {
                                    return;
                                }
                            }
                            this.ftab[num4] |= 0x200000;
                        }
                        num2++;
                    }
                    flagArray[num3] = true;
                    if (num < 0xff)
                    {
                        int num12 = this.ftab[num3 << 8] & -2097153;
                        int num13 = (this.ftab[(num3 + 1) << 8] & -2097153) - num12;
                        int num14 = 0;
                        while ((num13 >> num14) > 0xfffe)
                        {
                            num14++;
                        }
                        num2 = 0;
                        while (num2 < num13)
                        {
                            int num15 = this.zptr[num12 + num2];
                            int num16 = num2 >> num14;
                            this.quadrant[num15] = num16;
                            if (num15 < 20)
                            {
                                this.quadrant[(num15 + this.last) + 1] = num16;
                            }
                            num2++;
                        }
                        if (((num13 - 1) >> num14) > 0xffff)
                        {
                            Panic();
                        }
                    }
                    num2 = 0;
                    while (num2 <= 0xff)
                    {
                        numArray2[num2] = this.ftab[(num2 << 8) + num3] & -2097153;
                        num2++;
                    }
                    num2 = this.ftab[num3 << 8] & -2097153;
                    while (num2 < (this.ftab[(num3 + 1) << 8] & -2097153))
                    {
                        index = this.block[this.zptr[num2]];
                        if (!flagArray[index])
                        {
                            this.zptr[numArray2[index]] = (this.zptr[num2] == 0) ? this.last : (this.zptr[num2] - 1);
                            numArray2[index]++;
                        }
                        num2++;
                    }
                    for (num2 = 0; num2 <= 0xff; num2++)
                    {
                        this.ftab[(num2 << 8) + num3] |= 0x200000;
                    }
                }
            }
        }

        private void MakeMaps()
        {
            this.nInUse = 0;
            for (int i = 0; i < 0x100; i++)
            {
                if (this.inUse[i])
                {
                    this.seqToUnseq[this.nInUse] = (char) i;
                    this.unseqToSeq[i] = (char) this.nInUse;
                    this.nInUse++;
                }
            }
        }

        private static byte Med3(byte a, byte b, byte c)
        {
            byte num;
            if (a > b)
            {
                num = a;
                a = b;
                b = num;
            }
            if (b > c)
            {
                num = b;
                b = c;
                c = num;
            }
            if (a > b)
            {
                b = a;
            }
            return b;
        }

        private void MoveToFrontCodeAndSend()
        {
            this.BsPutIntVS(0x18, this.origPtr);
            this.GenerateMTFValues();
            this.SendMTFValues();
        }

        private static void Panic()
        {
            throw new BZip2Exception("BZip2 output stream panic");
        }

        private void QSort3(int loSt, int hiSt, int dSt)
        {
            StackElement[] elementArray = new StackElement[0x3e8];
            for (int i = 0; i < 0x3e8; i++)
            {
                elementArray[i] = new StackElement();
            }
            int index = 0;
            elementArray[index].ll = loSt;
            elementArray[index].hh = hiSt;
            elementArray[index].dd = dSt;
            index++;
            while (index > 0)
            {
                int num3;
                int num4;
                int num6;
                if (index >= 0x3e8)
                {
                    Panic();
                }
                index--;
                int ll = elementArray[index].ll;
                int hh = elementArray[index].hh;
                int dd = elementArray[index].dd;
                if (((hh - ll) < 20) || (dd > 10))
                {
                    this.SimpleSort(ll, hh, dd);
                    if ((this.workDone > this.workLimit) && this.firstAttempt)
                    {
                        return;
                    }
                    continue;
                }
                int num5 = Med3(this.block[(this.zptr[ll] + dd) + 1], this.block[(this.zptr[hh] + dd) + 1], this.block[(this.zptr[(ll + hh) >> 1] + dd) + 1]);
                int num = num3 = ll;
                int num2 = num4 = hh;
            Label_011E:
                if (num <= num2)
                {
                    num6 = this.block[(this.zptr[num] + dd) + 1] - num5;
                    if (num6 == 0)
                    {
                        int num13 = 0;
                        num13 = this.zptr[num];
                        this.zptr[num] = this.zptr[num3];
                        this.zptr[num3] = num13;
                        num3++;
                        num++;
                        goto Label_011E;
                    }
                    if (num6 <= 0)
                    {
                        num++;
                        goto Label_011E;
                    }
                }
            Label_017B:
                if (num <= num2)
                {
                    num6 = this.block[(this.zptr[num2] + dd) + 1] - num5;
                    if (num6 == 0)
                    {
                        int num14 = 0;
                        num14 = this.zptr[num2];
                        this.zptr[num2] = this.zptr[num4];
                        this.zptr[num4] = num14;
                        num4--;
                        num2--;
                        goto Label_017B;
                    }
                    if (num6 >= 0)
                    {
                        num2--;
                        goto Label_017B;
                    }
                }
                if (num <= num2)
                {
                    int num15 = this.zptr[num];
                    this.zptr[num] = this.zptr[num2];
                    this.zptr[num2] = num15;
                    num++;
                    num2--;
                    goto Label_011E;
                }
                if (num4 < num3)
                {
                    elementArray[index].ll = ll;
                    elementArray[index].hh = hh;
                    elementArray[index].dd = dd + 1;
                    index++;
                }
                else
                {
                    num6 = ((num3 - ll) < (num - num3)) ? (num3 - ll) : (num - num3);
                    this.Vswap(ll, num - num6, num6);
                    int n = ((hh - num4) < (num4 - num2)) ? (hh - num4) : (num4 - num2);
                    this.Vswap(num, (hh - n) + 1, n);
                    num6 = ((ll + num) - num3) - 1;
                    n = (hh - (num4 - num2)) + 1;
                    elementArray[index].ll = ll;
                    elementArray[index].hh = num6;
                    elementArray[index].dd = dd;
                    index++;
                    elementArray[index].ll = num6 + 1;
                    elementArray[index].hh = n - 1;
                    elementArray[index].dd = dd + 1;
                    index++;
                    elementArray[index].ll = n;
                    elementArray[index].hh = hh;
                    elementArray[index].dd = dd;
                    index++;
                }
            }
        }

        private void RandomiseBlock()
        {
            int num;
            int num2 = 0;
            int index = 0;
            for (num = 0; num < 0x100; num++)
            {
                this.inUse[num] = false;
            }
            for (num = 0; num <= this.last; num++)
            {
                if (num2 == 0)
                {
                    num2 = BZip2Constants.rNums[index];
                    index++;
                    if (index == 0x200)
                    {
                        index = 0;
                    }
                }
                num2--;
                this.block[num + 1] = (byte) (this.block[num + 1] ^ ((num2 == 1) ? ((byte) 1) : ((byte) 0)));
                this.block[num + 1] = (byte) (this.block[num + 1] & 0xff);
                this.inUse[this.block[num + 1]] = true;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("BZip2OutputStream Read not supported");
        }

        public override int ReadByte()
        {
            throw new NotSupportedException("BZip2OutputStream ReadByte not supported");
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("BZip2OutputStream Seek not supported");
        }

        private void SendMTFValues()
        {
            int num3;
            int num13;
            char[][] chArray = new char[6][];
            for (int i = 0; i < 6; i++)
            {
                chArray[i] = new char[0x102];
            }
            int index = 0;
            int alphaSize = this.nInUse + 2;
            for (int j = 0; j < 6; j++)
            {
                for (int num15 = 0; num15 < alphaSize; num15++)
                {
                    chArray[j][num15] = '\x000f';
                }
            }
            if (this.nMTF <= 0)
            {
                Panic();
            }
            if (this.nMTF < 200)
            {
                num13 = 2;
            }
            else if (this.nMTF < 600)
            {
                num13 = 3;
            }
            else if (this.nMTF < 0x4b0)
            {
                num13 = 4;
            }
            else if (this.nMTF < 0x960)
            {
                num13 = 5;
            }
            else
            {
                num13 = 6;
            }
            int num16 = num13;
            int nMTF = this.nMTF;
            int num2 = 0;
            while (num16 > 0)
            {
                int num18 = nMTF / num16;
                int num19 = 0;
                num3 = num2 - 1;
                while ((num19 < num18) && (num3 < (alphaSize - 1)))
                {
                    num3++;
                    num19 += this.mtfFreq[num3];
                }
                if (((num3 > num2) && (num16 != num13)) && ((num16 != 1) && (((num13 - num16) % 2) == 1)))
                {
                    num19 -= this.mtfFreq[num3];
                    num3--;
                }
                for (int num20 = 0; num20 < alphaSize; num20++)
                {
                    if ((num20 >= num2) && (num20 <= num3))
                    {
                        chArray[num16 - 1][num20] = '\0';
                    }
                    else
                    {
                        chArray[num16 - 1][num20] = '\x000f';
                    }
                }
                num16--;
                num2 = num3 + 1;
                nMTF -= num19;
            }
            int[][] numArray = new int[6][];
            for (int k = 0; k < 6; k++)
            {
                numArray[k] = new int[0x102];
            }
            int[] numArray2 = new int[6];
            short[] numArray3 = new short[6];
            for (int m = 0; m < 4; m++)
            {
                for (int num22 = 0; num22 < num13; num22++)
                {
                    numArray2[num22] = 0;
                }
                for (int num23 = 0; num23 < num13; num23++)
                {
                    for (int num24 = 0; num24 < alphaSize; num24++)
                    {
                        numArray[num23][num24] = 0;
                    }
                }
                index = 0;
                int num4 = 0;
                num2 = 0;
                while (true)
                {
                    if (num2 >= this.nMTF)
                    {
                        break;
                    }
                    num3 = (num2 + 50) - 1;
                    if (num3 >= this.nMTF)
                    {
                        num3 = this.nMTF - 1;
                    }
                    for (int num25 = 0; num25 < num13; num25++)
                    {
                        numArray3[num25] = 0;
                    }
                    if (num13 == 6)
                    {
                        short num27;
                        short num28;
                        short num29;
                        short num30;
                        short num31;
                        short num26 = num27 = num28 = num29 = num30 = (short) (num31 = 0);
                        for (int num32 = num2; num32 <= num3; num32++)
                        {
                            short num33 = this.szptr[num32];
                            num26 = (short) (num26 + ((short) chArray[0][num33]));
                            num27 = (short) (num27 + ((short) chArray[1][num33]));
                            num28 = (short) (num28 + ((short) chArray[2][num33]));
                            num29 = (short) (num29 + ((short) chArray[3][num33]));
                            num30 = (short) (num30 + ((short) chArray[4][num33]));
                            num31 = (short) (num31 + ((short) chArray[5][num33]));
                        }
                        numArray3[0] = num26;
                        numArray3[1] = num27;
                        numArray3[2] = num28;
                        numArray3[3] = num29;
                        numArray3[4] = num30;
                        numArray3[5] = num31;
                    }
                    else
                    {
                        for (int num34 = num2; num34 <= num3; num34++)
                        {
                            short num35 = this.szptr[num34];
                            for (int num36 = 0; num36 < num13; num36++)
                            {
                                numArray3[num36] = (short) (numArray3[num36] + ((short) chArray[num36][num35]));
                            }
                        }
                    }
                    int num6 = 0x3b9ac9ff;
                    int num5 = -1;
                    for (int num37 = 0; num37 < num13; num37++)
                    {
                        if (numArray3[num37] < num6)
                        {
                            num6 = numArray3[num37];
                            num5 = num37;
                        }
                    }
                    num4 += num6;
                    numArray2[num5]++;
                    this.selector[index] = (char) num5;
                    index++;
                    for (int num38 = num2; num38 <= num3; num38++)
                    {
                        numArray[num5][this.szptr[num38]]++;
                    }
                    num2 = num3 + 1;
                }
                for (int num39 = 0; num39 < num13; num39++)
                {
                    HbMakeCodeLengths(chArray[num39], numArray[num39], alphaSize, 20);
                }
            }
            numArray = null;
            numArray2 = null;
            numArray3 = null;
            if (num13 >= 8)
            {
                Panic();
            }
            if ((index >= 0x8000) || (index > 0x4652))
            {
                Panic();
            }
            char[] chArray2 = new char[6];
            for (int n = 0; n < num13; n++)
            {
                chArray2[n] = (char) n;
            }
            for (int num41 = 0; num41 < index; num41++)
            {
                char ch = this.selector[num41];
                int num42 = 0;
                char ch3 = chArray2[num42];
                while (ch != ch3)
                {
                    num42++;
                    char ch2 = ch3;
                    ch3 = chArray2[num42];
                    chArray2[num42] = ch2;
                }
                chArray2[0] = ch3;
                this.selectorMtf[num41] = (char) num42;
            }
            int[][] numArray4 = new int[6][];
            for (int num43 = 0; num43 < 6; num43++)
            {
                numArray4[num43] = new int[0x102];
            }
            for (int num44 = 0; num44 < num13; num44++)
            {
                int minLen = 0x20;
                int maxLen = 0;
                for (int num45 = 0; num45 < alphaSize; num45++)
                {
                    if (chArray[num44][num45] > maxLen)
                    {
                        maxLen = chArray[num44][num45];
                    }
                    if (chArray[num44][num45] < minLen)
                    {
                        minLen = chArray[num44][num45];
                    }
                }
                if (maxLen > 20)
                {
                    Panic();
                }
                if (minLen < 1)
                {
                    Panic();
                }
                HbAssignCodes(numArray4[num44], chArray[num44], minLen, maxLen, alphaSize);
            }
            bool[] flagArray = new bool[0x10];
            for (int num46 = 0; num46 < 0x10; num46++)
            {
                flagArray[num46] = false;
                for (int num47 = 0; num47 < 0x10; num47++)
                {
                    if (this.inUse[(num46 * 0x10) + num47])
                    {
                        flagArray[num46] = true;
                    }
                }
            }
            for (int num48 = 0; num48 < 0x10; num48++)
            {
                if (flagArray[num48])
                {
                    this.BsW(1, 1);
                }
                else
                {
                    this.BsW(1, 0);
                }
            }
            for (int num49 = 0; num49 < 0x10; num49++)
            {
                if (flagArray[num49])
                {
                    for (int num50 = 0; num50 < 0x10; num50++)
                    {
                        if (this.inUse[(num49 * 0x10) + num50])
                        {
                            this.BsW(1, 1);
                        }
                        else
                        {
                            this.BsW(1, 0);
                        }
                    }
                }
            }
            this.BsW(3, num13);
            this.BsW(15, index);
            for (int num51 = 0; num51 < index; num51++)
            {
                for (int num52 = 0; num52 < this.selectorMtf[num51]; num52++)
                {
                    this.BsW(1, 1);
                }
                this.BsW(1, 0);
            }
            for (int num53 = 0; num53 < num13; num53++)
            {
                int v = chArray[num53][0];
                this.BsW(5, v);
                int num55 = 0;
                goto Label_0691;
            Label_064F:
                this.BsW(2, 2);
                v++;
            Label_065D:
                if (v < chArray[num53][num55])
                {
                    goto Label_064F;
                }
                while (v > chArray[num53][num55])
                {
                    this.BsW(2, 3);
                    v--;
                }
                this.BsW(1, 0);
                num55++;
            Label_0691:
                if (num55 < alphaSize)
                {
                    goto Label_065D;
                }
            }
            int num12 = 0;
            num2 = 0;
            while (true)
            {
                if (num2 >= this.nMTF)
                {
                    break;
                }
                num3 = (num2 + 50) - 1;
                if (num3 >= this.nMTF)
                {
                    num3 = this.nMTF - 1;
                }
                for (int num56 = num2; num56 <= num3; num56++)
                {
                    this.BsW(chArray[this.selector[num12]][this.szptr[num56]], numArray4[this.selector[num12]][this.szptr[num56]]);
                }
                num2 = num3 + 1;
                num12++;
            }
            if (num12 != index)
            {
                Panic();
            }
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("BZip2OutputStream SetLength not supported");
        }

        private void SimpleSort(int lo, int hi, int d)
        {
            int num4 = (hi - lo) + 1;
            if (num4 >= 2)
            {
                int index = 0;
                while (this.increments[index] < num4)
                {
                    index++;
                }
                index--;
                while (index >= 0)
                {
                    int num3 = this.increments[index];
                    int num = lo + num3;
                    do
                    {
                        if (num > hi)
                        {
                            goto Label_0160;
                        }
                        int num6 = this.zptr[num];
                        int num2 = num;
                        while (this.FullGtU(this.zptr[num2 - num3] + d, num6 + d))
                        {
                            this.zptr[num2] = this.zptr[num2 - num3];
                            num2 -= num3;
                            if (num2 <= ((lo + num3) - 1))
                            {
                                break;
                            }
                        }
                        this.zptr[num2] = num6;
                        num++;
                        if (num > hi)
                        {
                            goto Label_0160;
                        }
                        num6 = this.zptr[num];
                        num2 = num;
                        while (this.FullGtU(this.zptr[num2 - num3] + d, num6 + d))
                        {
                            this.zptr[num2] = this.zptr[num2 - num3];
                            num2 -= num3;
                            if (num2 <= ((lo + num3) - 1))
                            {
                                break;
                            }
                        }
                        this.zptr[num2] = num6;
                        num++;
                        if (num > hi)
                        {
                            goto Label_0160;
                        }
                        num6 = this.zptr[num];
                        num2 = num;
                        while (this.FullGtU(this.zptr[num2 - num3] + d, num6 + d))
                        {
                            this.zptr[num2] = this.zptr[num2 - num3];
                            num2 -= num3;
                            if (num2 <= ((lo + num3) - 1))
                            {
                                break;
                            }
                        }
                        this.zptr[num2] = num6;
                        num++;
                    }
                    while ((this.workDone <= this.workLimit) || !this.firstAttempt);
                    return;
                Label_0160:
                    index--;
                }
            }
        }

        private void Vswap(int p1, int p2, int n)
        {
            int num = 0;
            while (n > 0)
            {
                num = this.zptr[p1];
                this.zptr[p1] = this.zptr[p2];
                this.zptr[p2] = num;
                p1++;
                p2++;
                n--;
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            if ((buffer.Length - offset) < count)
            {
                throw new ArgumentException("Offset/count out of range");
            }
            for (int i = 0; i < count; i++)
            {
                this.WriteByte(buffer[offset + i]);
            }
        }

        public override void WriteByte(byte value)
        {
            int num = (0x100 + value) % 0x100;
            if (this.currentChar != -1)
            {
                if (this.currentChar != num)
                {
                    this.WriteRun();
                    this.runLength = 1;
                    this.currentChar = num;
                }
                else
                {
                    this.runLength++;
                    if (this.runLength > 0xfe)
                    {
                        this.WriteRun();
                        this.currentChar = -1;
                        this.runLength = 0;
                    }
                }
            }
            else
            {
                this.currentChar = num;
                this.runLength++;
            }
        }

        private void WriteRun()
        {
            if (this.last < this.allowableBlockSize)
            {
                this.inUse[this.currentChar] = true;
                for (int i = 0; i < this.runLength; i++)
                {
                    this.mCrc.Update(this.currentChar);
                }
                switch (this.runLength)
                {
                    case 1:
                        this.last++;
                        this.block[this.last + 1] = (byte) this.currentChar;
                        return;

                    case 2:
                        this.last++;
                        this.block[this.last + 1] = (byte) this.currentChar;
                        this.last++;
                        this.block[this.last + 1] = (byte) this.currentChar;
                        return;

                    case 3:
                        this.last++;
                        this.block[this.last + 1] = (byte) this.currentChar;
                        this.last++;
                        this.block[this.last + 1] = (byte) this.currentChar;
                        this.last++;
                        this.block[this.last + 1] = (byte) this.currentChar;
                        return;
                }
                this.inUse[this.runLength - 4] = true;
                this.last++;
                this.block[this.last + 1] = (byte) this.currentChar;
                this.last++;
                this.block[this.last + 1] = (byte) this.currentChar;
                this.last++;
                this.block[this.last + 1] = (byte) this.currentChar;
                this.last++;
                this.block[this.last + 1] = (byte) this.currentChar;
                this.last++;
                this.block[this.last + 1] = (byte) (this.runLength - 4);
            }
            else
            {
                this.EndBlock();
                this.InitBlock();
                this.WriteRun();
            }
        }

        public int BytesWritten =>
            this.bytesOut;

        public override bool CanRead =>
            false;

        public override bool CanSeek =>
            false;

        public override bool CanWrite =>
            this.baseStream.CanWrite;

        public bool IsStreamOwner
        {
            get => 
                this.isStreamOwner;
            set
            {
                this.isStreamOwner = value;
            }
        }

        public override long Length =>
            this.baseStream.Length;

        public override long Position
        {
            get => 
                this.baseStream.Position;
            set
            {
                throw new NotSupportedException("BZip2OutputStream position cannot be set");
            }
        }

        private class StackElement
        {
            public int dd;
            public int hh;
            public int ll;
        }
    }
}

