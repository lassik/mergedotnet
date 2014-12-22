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

        public override Stream OutputStream
        {
            get
            {
                if (fileStream == null)
                    throw new Exception("Can't happen: No open stream");
                if (!fileStream.CanWrite)
                    throw new Exception("Can't happen: Open stream not writable");
                return fileStream;
            }
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

        private static void EnsureNoAttributes(string fullPath, FileAttributes unwantedAttributes)
        {
            FileAttributes fileAttr = File.GetAttributes(fullPath);
            if ((fileAttr & unwantedAttributes) != 0)
                File.SetAttributes(fullPath, (fileAttr & ~unwantedAttributes));
        }

        public override void DeleteFile(string name)
        {
            string fullPath = FullPathWithName(name);
            EnsureNoAttributes(fullPath, FileAttributes.ReadOnly);
            File.Delete(fullPath);
        }

        public override void OpenFileForReading(string name)
        {
            if (fileStream != null) throw new Exception("Can't happen");
            fileStream = File.Open(FullPathWithName(name), FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public override void ReadFileIntoStream(string name, Stream output)
        {
            if (fileStream != null) throw new Exception("Can't happen");
            fileStream = File.Open(FullPathWithName(name), FileMode.Open, FileAccess.Read, FileShare.Read);
            fileStream.CopyTo(output);
            CloseFile();
        }

        public override void OpenFileForWriting(string name, DateTime lastWriteTimeUtc)
        {
            if (fileStream != null) throw new Exception("Can't happen");
            fileStream = File.Open(FullPathWithName(name), FileMode.CreateNew, FileAccess.Write);
            fileLastWriteTimeUtc = lastWriteTimeUtc;
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
