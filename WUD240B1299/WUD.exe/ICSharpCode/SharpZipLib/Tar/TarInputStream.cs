﻿namespace ICSharpCode.SharpZipLib.Tar
{
    using System;
    using System.IO;
    using System.Text;

    public class TarInputStream : Stream
    {
        protected TarBuffer buffer;
        private TarEntry currentEntry;
        protected IEntryFactory entryFactory;
        protected long entryOffset;
        protected long entrySize;
        protected bool hasHitEOF;
        private Stream inputStream;
        protected byte[] readBuffer;

        public TarInputStream(Stream inputStream) : this(inputStream, 20)
        {
        }

        public TarInputStream(Stream inputStream, int blockFactor)
        {
            this.inputStream = inputStream;
            this.buffer = TarBuffer.CreateInputTarBuffer(inputStream, blockFactor);
        }

        public override void Close()
        {
            this.buffer.Close();
        }

        public void CopyEntryContents(Stream outputStream)
        {
            byte[] buffer = new byte[0x8000];
            while (true)
            {
                int count = this.Read(buffer, 0, buffer.Length);
                if (count <= 0)
                {
                    return;
                }
                outputStream.Write(buffer, 0, count);
            }
        }

        public override void Flush()
        {
            this.inputStream.Flush();
        }

        public TarEntry GetNextEntry()
        {
            if (this.hasHitEOF)
            {
                return null;
            }
            if (this.currentEntry != null)
            {
                this.SkipToNextEntry();
            }
            byte[] block = this.buffer.ReadBlock();
            if (block == null)
            {
                this.hasHitEOF = true;
            }
            else if (this.buffer.IsEOFBlock(block))
            {
                this.hasHitEOF = true;
            }
            if (this.hasHitEOF)
            {
                this.currentEntry = null;
            }
            else
            {
                try
                {
                    TarHeader header = new TarHeader();
                    header.ParseBuffer(block);
                    if (!header.IsChecksumValid)
                    {
                        throw new TarException("Header checksum is invalid");
                    }
                    this.entryOffset = 0L;
                    this.entrySize = header.Size;
                    StringBuilder builder = null;
                    if (header.TypeFlag == 0x4c)
                    {
                        byte[] buffer = new byte[0x200];
                        long entrySize = this.entrySize;
                        builder = new StringBuilder();
                        while (entrySize > 0L)
                        {
                            int length = this.Read(buffer, 0, (entrySize > buffer.Length) ? buffer.Length : ((int) entrySize));
                            if (length == -1)
                            {
                                throw new InvalidHeaderException("Failed to read long name entry");
                            }
                            builder.Append(TarHeader.ParseName(buffer, 0, length).ToString());
                            entrySize -= length;
                        }
                        this.SkipToNextEntry();
                        block = this.buffer.ReadBlock();
                    }
                    else if (header.TypeFlag == 0x67)
                    {
                        this.SkipToNextEntry();
                        block = this.buffer.ReadBlock();
                    }
                    else if (header.TypeFlag == 120)
                    {
                        this.SkipToNextEntry();
                        block = this.buffer.ReadBlock();
                    }
                    else if (header.TypeFlag == 0x56)
                    {
                        this.SkipToNextEntry();
                        block = this.buffer.ReadBlock();
                    }
                    else if (((header.TypeFlag != 0x30) && (header.TypeFlag != 0)) && (header.TypeFlag != 0x35))
                    {
                        this.SkipToNextEntry();
                        block = this.buffer.ReadBlock();
                    }
                    if (this.entryFactory == null)
                    {
                        this.currentEntry = new TarEntry(block);
                        if (builder != null)
                        {
                            this.currentEntry.Name = builder.ToString();
                        }
                    }
                    else
                    {
                        this.currentEntry = this.entryFactory.CreateEntry(block);
                    }
                    this.entryOffset = 0L;
                    this.entrySize = this.currentEntry.Size;
                }
                catch (InvalidHeaderException exception)
                {
                    this.entrySize = 0L;
                    this.entryOffset = 0L;
                    this.currentEntry = null;
                    throw new InvalidHeaderException($"Bad header in record {this.buffer.CurrentRecord} block {this.buffer.CurrentBlock} {exception.Message}");
                }
            }
            return this.currentEntry;
        }

        [Obsolete("Use RecordSize property instead")]
        public int GetRecordSize() => 
            this.buffer.RecordSize;

        public void Mark(int markLimit)
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            int num = 0;
            if (this.entryOffset >= this.entrySize)
            {
                return 0;
            }
            long num2 = count;
            if ((num2 + this.entryOffset) > this.entrySize)
            {
                num2 = this.entrySize - this.entryOffset;
            }
            if (this.readBuffer != null)
            {
                int length = (num2 > this.readBuffer.Length) ? this.readBuffer.Length : ((int) num2);
                Array.Copy(this.readBuffer, 0, buffer, offset, length);
                if (length >= this.readBuffer.Length)
                {
                    this.readBuffer = null;
                }
                else
                {
                    int num4 = this.readBuffer.Length - length;
                    byte[] destinationArray = new byte[num4];
                    Array.Copy(this.readBuffer, length, destinationArray, 0, num4);
                    this.readBuffer = destinationArray;
                }
                num += length;
                num2 -= length;
                offset += length;
            }
            while (num2 > 0L)
            {
                byte[] sourceArray = this.buffer.ReadBlock();
                if (sourceArray == null)
                {
                    throw new TarException("unexpected EOF with " + num2 + " bytes unread");
                }
                int num5 = (int) num2;
                int num6 = sourceArray.Length;
                if (num6 > num5)
                {
                    Array.Copy(sourceArray, 0, buffer, offset, num5);
                    this.readBuffer = new byte[num6 - num5];
                    Array.Copy(sourceArray, num5, this.readBuffer, 0, num6 - num5);
                }
                else
                {
                    num5 = num6;
                    Array.Copy(sourceArray, 0, buffer, offset, num6);
                }
                num += num5;
                num2 -= num5;
                offset += num5;
            }
            this.entryOffset += num;
            return num;
        }

        public override int ReadByte()
        {
            byte[] buffer = new byte[1];
            if (this.Read(buffer, 0, 1) <= 0)
            {
                return -1;
            }
            return buffer[0];
        }

        public void Reset()
        {
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("TarInputStream Seek not supported");
        }

        public void SetEntryFactory(IEntryFactory factory)
        {
            this.entryFactory = factory;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("TarInputStream SetLength not supported");
        }

        public void Skip(long skipCount)
        {
            int num3;
            byte[] buffer = new byte[0x2000];
            for (long i = skipCount; i > 0L; i -= num3)
            {
                int count = (i > buffer.Length) ? buffer.Length : ((int) i);
                num3 = this.Read(buffer, 0, count);
                if (num3 == -1)
                {
                    return;
                }
            }
        }

        private void SkipToNextEntry()
        {
            long skipCount = this.entrySize - this.entryOffset;
            if (skipCount > 0L)
            {
                this.Skip(skipCount);
            }
            this.readBuffer = null;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("TarInputStream Write not supported");
        }

        public override void WriteByte(byte value)
        {
            throw new NotSupportedException("TarInputStream WriteByte not supported");
        }

        public long Available =>
            (this.entrySize - this.entryOffset);

        public override bool CanRead =>
            this.inputStream.CanRead;

        public override bool CanSeek =>
            false;

        public override bool CanWrite =>
            false;

        public bool IsMarkSupported =>
            false;

        public override long Length =>
            this.inputStream.Length;

        public override long Position
        {
            get => 
                this.inputStream.Position;
            set
            {
                throw new NotSupportedException("TarInputStream Seek not supported");
            }
        }

        public int RecordSize =>
            this.buffer.RecordSize;

        public class EntryFactoryAdapter : TarInputStream.IEntryFactory
        {
            public TarEntry CreateEntry(string name) => 
                TarEntry.CreateTarEntry(name);

            public TarEntry CreateEntry(byte[] headerBuffer) => 
                new TarEntry(headerBuffer);

            public TarEntry CreateEntryFromFile(string fileName) => 
                TarEntry.CreateEntryFromFile(fileName);
        }

        public interface IEntryFactory
        {
            TarEntry CreateEntry(string name);
            TarEntry CreateEntry(byte[] headerBuf);
            TarEntry CreateEntryFromFile(string fileName);
        }
    }
}

