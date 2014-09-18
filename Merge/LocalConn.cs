using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Merge
{
    public class LocalConn : Conn
    {
        private string root;
        private List<string> subs = new List<string>();

        private FileStream fileStream = null;
        private DateTime fileLastWriteTimeUtc = DateTime.MinValue;

        public LocalConn(string root)
        {
            this.root = root;
        }

        private string FullPath
        {
            get
            {
                string path = root;
                foreach(string sub in subs) path = Path.Combine(path, sub);
                return(path);
            }
        }

        private string FullPathWithName(string name)
        {
            return Path.Combine(FullPath, name);
        }

        public override void GoToDir(string[] path)
        {
            this.subs = new List<string>(path);
        }

        public override void AscendDir()
        {
            if (subs.Count < 1)
                throw new Exception("Already at top level");
            subs.RemoveAt(subs.Count-1);
        }

        public override void DescendDir(string name)
        {
            // TODO: check valid name (no "." or ".." etc)
            subs.Add(name);
        }

        private Core.Info.TypeEn TypeFromAttributes(FileAttributes attr)
        {
            if((attr & FileAttributes.Directory) == FileAttributes.Directory) return(Core.Info.TypeEn.Dir);
            if((attr & FileAttributes.Device) == FileAttributes.Device) return(Core.Info.TypeEn.Other);
            return(Core.Info.TypeEn.File);
        }

        public override void MapDirEntries(MapEntriesFn fn)
        {
            DirectoryInfo di = null;
            try
            {
                di = new DirectoryInfo(FullPath);
            }
            catch (PathTooLongException)
            {
                return;
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }
            catch (IOException)
            {
                return;
            }

            foreach(FileSystemInfo fsi in di.GetFileSystemInfos("*"))
            {
                if((fsi.Name == ".") || (fsi.Name == "..")) continue;
                var info = new Core.Info();
                info.Type = TypeFromAttributes(fsi.Attributes);
                if(info.Type == Core.Info.TypeEn.File)
                {
                    info.Time = fsi.LastWriteTimeUtc;
                    info.Size = (ulong)(new FileInfo(fsi.FullName).Length);
                }
                fn(fsi.Name, info);
            }
        }

        public override void DeleteEmptyDir(string name)
        {
            Directory.Delete(FullPathWithName(name), false);
        }

        public override void CreateEmptyDir(string name)
        {
            Directory.CreateDirectory(FullPathWithName(name));
        }

        public override void DeleteFile(string name)
        {
            File.Delete(FullPathWithName(name));
        }

        public override void OpenFileForReading(string name)
        {
            if (fileStream != null) throw new Exception("Can't happen");
            fileStream = File.Open(FullPathWithName(name), FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public override void OpenFileForWriting(string name, DateTime lastWriteTimeUtc)
        {
            if (fileStream != null) throw new Exception("Can't happen");
            fileStream = File.Open(FullPathWithName(name), FileMode.CreateNew, FileAccess.Write);
            fileLastWriteTimeUtc = lastWriteTimeUtc;
        }

        public override bool ReadFromFile(out byte[] buf, out int nbytes)
        {
            if (fileStream == null) throw new Exception("Can't happen");
            buf = new byte[1024 * 64];
            nbytes = fileStream.Read(buf, 0, buf.Length);
            return (nbytes > 0);
        }

        public override void WriteToFile(byte[] buf, int nbytes)
        {
            if (fileStream == null) throw new Exception("Can't happen");
            fileStream.Write(buf, 0, nbytes);
        }

        public override void CloseFile()
        {
            if (fileStream != null)
            {
                bool wasWriting = fileStream.CanWrite && !fileStream.CanRead;
                string fileName = fileStream.Name;
                fileStream.Close();
                if (wasWriting)
                    File.SetLastWriteTimeUtc(fileName, fileLastWriteTimeUtc);
            }
            fileStream = null;
            fileLastWriteTimeUtc = DateTime.MinValue;
        }
    }
}
