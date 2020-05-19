﻿namespace ICSharpCode.SharpZipLib.Zip
{
    using System;
    using System.IO;

    internal class StaticDiskDataSource : IStaticDataSource
    {
        private string fileName_;

        public StaticDiskDataSource(string fileName)
        {
            this.fileName_ = fileName;
        }

        public Stream GetSource() => 
            File.OpenRead(this.fileName_);
    }
}

