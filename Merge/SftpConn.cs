using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Renci.SshNet;
using Renci.SshNet.Common;
using System.Windows.Forms;
using Renci.SshNet.Sftp;

namespace Merge
{
    public class SftpConn : Conn
    {
        private SftpClient sftpClient = null;
        private List<string> rootDirParts = new List<string>();
        private List<string> subDirParts = new List<string>();

        private string fileName = null;
        private SftpFileStream fileStream = null;
        private DateTime fileLastWriteTimeUtc = DateTime.MinValue;

        public SftpConn(string hostname, string username, string password)
        {
            var keyboardInteractive = new KeyboardInteractiveAuthenticationMethod(username);
            keyboardInteractive.AuthenticationPrompt += delegate(object sender, AuthenticationPromptEventArgs e)
            {
                foreach (var prompt in e.Prompts)
                {
                    if (prompt.Request.StartsWith("password", StringComparison.InvariantCultureIgnoreCase))
                        prompt.Response = password;
                    else
                        prompt.Response = "";
                }
            };
            var connectionInfo = new ConnectionInfo(hostname, 22, username,
                keyboardInteractive,
                new PasswordAuthenticationMethod(username, password));
            //connectionInfo.Encoding = Encoding.UTF8;
            connectionInfo.Encoding = Encoding.GetEncoding(1252);
            sftpClient = new SftpClient(connectionInfo);
            try
            {
                sftpClient.Connect();
            }
            catch (SshAuthenticationException)
            {
                MessageBox.Show("Authentication failed (wrong password?)", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
            string wd = sftpClient.WorkingDirectory;
            subDirParts = new List<string>(wd.Split(new char[] {'/'}, StringSplitOptions.RemoveEmptyEntries));
        }

        public string FullPath
        {
            get
            {
                return ("/" + String.Join("/", rootDirParts.Concat(subDirParts).ToArray()));
            }
        }

        public string FullPathWithName(string name)
        {
            return ("/" + String.Join("/", rootDirParts.Concat(subDirParts).Concat(new string[] {name}).ToArray()));
        }

        public override void GoToDir(string[] path)
        {
            this.subDirParts = new List<string>(path);
        }

        public void SetRootDirToCurrent()
        {
            this.rootDirParts = new List<string>(this.rootDirParts.Concat(this.subDirParts));
        }

        public override void AscendDir()
        {
            if (subDirParts.Count < 1)
                throw new Exception("Already at top level");
            subDirParts.RemoveAt(subDirParts.Count-1);
        }

        public override void DescendDir(string name)
        {
            // TODO: check valid name (no "." or ".." etc)
            subDirParts.Add(name);
        }

        private string WireEncodedString(string s)
        {
            return Encoding.GetEncoding(1252).GetString(Encoding.UTF8.GetBytes(s.Normalize(NormalizationForm.FormD)));
            //return s;
        }

        public override void MapDirEntries(MapEntriesFn fn)
        {
            foreach (var file in sftpClient.ListDirectory(WireEncodedString(FullPath), null))
            {
                if ((file.Name == ".") || (file.Name == ".."))
                    continue;
                var info = new Core.Info();
                if (file.IsSymbolicLink)
                {
                    info.Type = Core.Info.TypeEn.Other;
                }
                else if (file.IsDirectory)
                {
                    info.Type = Core.Info.TypeEn.Dir;
                }
                else if (file.IsRegularFile)
                {
                    info.Type = Core.Info.TypeEn.File;
                    info.Time = file.LastWriteTimeUtc;
                    info.Size = (ulong)file.Length;
                }
                else
                    info.Type = Core.Info.TypeEn.Other;
                string realName = Encoding.UTF8.GetString(Encoding.GetEncoding(1252).GetBytes(file.Name)).Normalize(NormalizationForm.FormC);
                //string realName = file.Name;
                fn(realName, info);
            }
        }

        public override void DeleteEmptyDir(string name)
        {
            sftpClient.DeleteDirectory(WireEncodedString(FullPathWithName(name)));
        }

        public override void CreateEmptyDir(string name)
        {
            sftpClient.CreateDirectory(WireEncodedString(FullPathWithName(name)));
        }

        public override void DeleteFile(string name)
        {
            sftpClient.DeleteFile(WireEncodedString(FullPathWithName(name)));
        }

        public override void OpenFileForReading(string name)
        {
            if (fileStream != null) throw new Exception("Can't happen");
            fileName = FullPathWithName(name);
            fileStream = sftpClient.OpenRead(WireEncodedString(fileName));
            fileLastWriteTimeUtc = DateTime.MinValue;
        }

        public override void OpenFileForWriting(string name, DateTime lastWriteTimeUtc)
        {
            if (fileStream != null) throw new Exception("Can't happen");
            fileName = FullPathWithName(name);
            fileStream = sftpClient.OpenWrite(WireEncodedString(fileName));
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
                fileStream.Close();
                if (wasWriting)
                    sftpClient.SetLastWriteTimeUtc(WireEncodedString(fileName), fileLastWriteTimeUtc);
            }
            fileName = null;
            fileStream = null;
            fileLastWriteTimeUtc = DateTime.MinValue;
        }
    }
}
