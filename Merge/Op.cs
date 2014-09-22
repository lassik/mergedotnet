using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Merge
{
    public abstract class Op : IComparable<Op>
    {
        public abstract void Perform();
        public string[] path;
        
        public int Rank
        {
            get
            {
                if (this is Op.DeleteFile) return 0;
                if (this is Op.DeleteEmptyDir) return 1;
                if (this is Op.CreateEmptyDir) return 2;
                if (this is Op.CreateFile) return 3;
                throw new Exception("Can't happen");
            }
        }

        public int CompareTo(Op other)
        {
            return this.Rank.CompareTo(other.Rank);
        }
             
        public class DeleteEmptyDir : Op
        {
            Conn conn;
            string name;

            public DeleteEmptyDir(string[] path, Conn conn, string name)
            {
                this.path = path;
                this.conn = conn;
                this.name = name;
            }

            public override string ToString()
            {
                return String.Format("{0} {1} {2}", conn, this.GetType().Name, String.Join("/", path) + "/" + name);
            }

            public override void Perform()
            {
                conn.GoToDir(path);
                conn.DeleteEmptyDir(name);
            }
        }

        public class CreateEmptyDir : Op
        {
            Conn conn;
            string name;

            public CreateEmptyDir(string[] path, Conn conn, string name)
            {
                this.path = path;
                this.conn = conn;
                this.name = name;
            }

            public override string ToString()
            {
                return String.Format("{0} {1} {2}", conn, this.GetType().Name, String.Join("/", path) + "/" + name);
            }

            public override void Perform()
            {
                conn.GoToDir(path);
                conn.CreateEmptyDir(name);
            }
        }

        public class DeleteFile : Op
        {
            Conn conn;
            string name;

            public DeleteFile(string[] path, Conn conn, string name)
            {
                this.path = path;
                this.conn = conn;
                this.name = name;
            }

            public override string ToString()
            {
                return String.Format("{0} {1} {2}", conn, this.GetType().Name, String.Join("/", path) + "/" + name);
            }

            public override void Perform()
            {
                conn.GoToDir(path);
                conn.DeleteFile(name);
            }
        }

        public class CreateFile : Op
        {
            Conn srcConn;
            Conn dstConn;
            string name;
            DateTime lastWriteTimeUtc;

            public CreateFile(string[] path, Conn srcConn, Conn dstConn, string name, DateTime lastWriteTimeUtc)
            {
                this.path = path;
                this.srcConn = srcConn;
                this.dstConn = dstConn;
                this.name = name;
                this.lastWriteTimeUtc = lastWriteTimeUtc;
            }

            public override string ToString()
            {
                return String.Format("{0} {1} {2} based on {3}", dstConn, this.GetType().Name, String.Join("/", path) + "/" + name, srcConn);
            }

            public override void Perform()
            {
                srcConn.GoToDir(path);
                dstConn.GoToDir(path);

                srcConn.OpenFileForReading(name);
                try
                {
                    dstConn.OpenFileForWriting(name, lastWriteTimeUtc);
                    try
                    {
                        byte[] buf;
                        int nbytes;
                        while (srcConn.ReadFromFile(out buf, out nbytes))
                            dstConn.WriteToFile(buf, nbytes);
                    }
                    finally
                    {
                        dstConn.CloseFile();
                    }
                }
                finally
                {
                    srcConn.CloseFile();
                }
            }
        }
    }
}
