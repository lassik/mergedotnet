using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Merge
{
    public abstract class Conn
    {
        public delegate void MapEntriesFn(string name, Core.Info info);

        public string Title;

        public override string ToString()
        {
            return Title;
        }

        public abstract void GoToDir(string[] path);
        public abstract void AscendDir();
        public abstract void DescendDir(string name);
        public abstract void MapDirEntries(MapEntriesFn fn);
        public abstract void DeleteEmptyDir(string name);
        public abstract void CreateEmptyDir(string name);
        public abstract void DeleteFile(string name);
        public abstract void OpenFileForReading(string name);
        public abstract void OpenFileForWriting(string name, DateTime lastWriteTimeUtc);
        public abstract bool ReadFromFile(out byte[] buf, out int nbytes);
        public abstract void WriteToFile(byte[] buf, int nbytes);
        public abstract void CloseFile();
    }
}
