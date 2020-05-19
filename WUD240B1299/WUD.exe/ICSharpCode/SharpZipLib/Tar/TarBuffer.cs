﻿namespace ICSharpCode.SharpZipLib.Tar
{
    using System;
    using System.IO;

    public class TarBuffer
    {
        private int blockFactor = 20;
        public const int BlockSize = 0x200;
        private int currentBlockIndex;
        private int currentRecordIndex;
        public const int DefaultBlockFactor = 20;
        public const int DefaultRecordSize = 0x2800;
        private Stream inputStream;
        private Stream outputStream;
        private byte[] recordBuffer;
        private int recordSize = 0x2800;

        protected TarBuffer()
        {
        }

        public void Close()
        {
            if (this.outputStream != null)
            {
                this.Flush();
                this.outputStream.Close();
                this.outputStream = null;
            }
            else if (this.inputStream != null)
            {
                this.inputStream.Close();
                this.inputStream = null;
            }
        }

        public static TarBuffer CreateInputTarBuffer(Stream inputStream)
        {
            if (inputStream == null)
            {
                throw new ArgumentNullException("inputStream");
            }
            return CreateInputTarBuffer(inputStream, 20);
        }

        public static TarBuffer CreateInputTarBuffer(Stream inputStream, int blockFactor)
        {
            if (inputStream == null)
            {
                throw new ArgumentNullException("inputStream");
            }
            if (blockFactor <= 0)
            {
                throw new ArgumentOutOfRangeException("blockFactor", "Factor cannot be negative");
            }
            TarBuffer buffer = new TarBuffer {
                inputStream = inputStream,
                outputStream = null
            };
            buffer.Initialize(blockFactor);
            return buffer;
        }

        public static TarBuffer CreateOutputTarBuffer(Stream outputStream)
        {
            if (outputStream == null)
            {
                throw new ArgumentNullException("outputStream");
            }
            return CreateOutputTarBuffer(outputStream, 20);
        }

        public static TarBuffer CreateOutputTarBuffer(Stream outputStream, int blockFactor)
        {
            if (outputStream == null)
            {
                throw new ArgumentNullException("outputStream");
            }
            if (blockFactor <= 0)
            {
                throw new ArgumentOutOfRangeException("blockFactor", "Factor cannot be negative");
            }
            TarBuffer buffer = new TarBuffer {
                inputStream = null,
                outputStream = outputStream
            };
            buffer.Initialize(blockFactor);
            return buffer;
        }

        private void Flush()
        {
            if (this.outputStream == null)
            {
                throw new TarException("TarBuffer.Flush no output stream defined");
            }
            if (this.currentBlockIndex > 0)
            {
                int index = this.currentBlockIndex * 0x200;
                Array.Clear(this.recordBuffer, index, this.RecordSize - index);
                this.WriteRecord();
            }
            this.outputStream.Flush();
        }

        [Obsolete("Use BlockFactor property instead")]
        public int GetBlockFactor() => 
            this.blockFactor;

        [Obsolete("Use CurrentBlock property instead")]
        public int GetCurrentBlockNum() => 
            this.currentBlockIndex;

        [Obsolete("Use CurrentRecord property instead")]
        public int GetCurrentRecordNum() => 
            this.currentRecordIndex;

        [Obsolete("Use RecordSize property instead")]
        public int GetRecordSize() => 
            this.recordSize;

        private void Initialize(int blockFactor)
        {
            this.blockFactor = blockFactor;
            this.recordSize = blockFactor * 0x200;
            this.recordBuffer = new byte[this.RecordSize];
            if (this.inputStream != null)
            {
                this.currentRecordIndex = -1;
                this.currentBlockIndex = this.BlockFactor;
            }
            else
            {
                this.currentRecordIndex = 0;
                this.currentBlockIndex = 0;
            }
        }

        public bool IsEOFBlock(byte[] block)
        {
            if (block == null)
            {
                throw new ArgumentNullException("block");
            }
            if (block.Length != 0x200)
            {
                throw new ArgumentException("block length is invalid");
            }
            for (int i = 0; i < 0x200; i++)
            {
                if (block[i] != 0)
                {
                    return false;
                }
            }
            return true;
        }

        public byte[] ReadBlock()
        {
            if (this.inputStream == null)
            {
                throw new TarException("TarBuffer.ReadBlock - no input stream defined");
            }
            if ((this.currentBlockIndex >= this.BlockFactor) && !this.ReadRecord())
            {
                throw new TarException("Failed to read a record");
            }
            byte[] destinationArray = new byte[0x200];
            Array.Copy(this.recordBuffer, this.currentBlockIndex * 0x200, destinationArray, 0, 0x200);
            this.currentBlockIndex++;
            return destinationArray;
        }

        private bool ReadRecord()
        {
            long num3;
            if (this.inputStream == null)
            {
                throw new TarException("no input stream stream defined");
            }
            this.currentBlockIndex = 0;
            int offset = 0;
            for (int i = this.RecordSize; i > 0; i -= (int) num3)
            {
                num3 = this.inputStream.Read(this.recordBuffer, offset, i);
                if (num3 <= 0L)
                {
                    break;
                }
                offset += (int) num3;
            }
            this.currentRecordIndex++;
            return true;
        }

        public void SkipBlock()
        {
            if (this.inputStream == null)
            {
                throw new TarException("no input stream defined");
            }
            if ((this.currentBlockIndex >= this.BlockFactor) && !this.ReadRecord())
            {
                throw new TarException("Failed to read a record");
            }
            this.currentBlockIndex++;
        }

        public void WriteBlock(byte[] block)
        {
            if (block == null)
            {
                throw new ArgumentNullException("block");
            }
            if (this.outputStream == null)
            {
                throw new TarException("TarBuffer.WriteBlock - no output stream defined");
            }
            if (block.Length != 0x200)
            {
                throw new TarException($"TarBuffer.WriteBlock - block to write has length '{block.Length}' which is not the block size of '{0x200}'");
            }
            if (this.currentBlockIndex >= this.BlockFactor)
            {
                this.WriteRecord();
            }
            Array.Copy(block, 0, this.recordBuffer, this.currentBlockIndex * 0x200, 0x200);
            this.currentBlockIndex++;
        }

        public void WriteBlock(byte[] buffer, int offset)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (this.outputStream == null)
            {
                throw new TarException("TarBuffer.WriteBlock - no output stream stream defined");
            }
            if ((offset < 0) || (offset >= buffer.Length))
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            if ((offset + 0x200) > buffer.Length)
            {
                throw new TarException($"TarBuffer.WriteBlock - record has length '{buffer.Length}' with offset '{offset}' which is less than the record size of '{this.recordSize}'");
            }
            if (this.currentBlockIndex >= this.BlockFactor)
            {
                this.WriteRecord();
            }
            Array.Copy(buffer, offset, this.recordBuffer, this.currentBlockIndex * 0x200, 0x200);
            this.currentBlockIndex++;
        }

        private void WriteRecord()
        {
            if (this.outputStream == null)
            {
                throw new TarException("TarBuffer.WriteRecord no output stream defined");
            }
            this.outputStream.Write(this.recordBuffer, 0, this.RecordSize);
            this.outputStream.Flush();
            this.currentBlockIndex = 0;
            this.currentRecordIndex++;
        }

        public int BlockFactor =>
            this.blockFactor;

        public int CurrentBlock =>
            this.currentBlockIndex;

        public int CurrentRecord =>
            this.currentRecordIndex;

        public int RecordSize =>
            this.recordSize;
    }
}

