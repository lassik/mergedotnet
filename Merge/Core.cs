using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Merge
{
    public class Core
    {
        public const int MaxConnCount = 5;  // 2 <= MaxConnCount <= 64
        public const int WinnerPreserve = -2;
        public const int WinnerInherit  = -1;

        // NOTE: These must be regular files, not directories.
        public string[] IgnoredFileNames = 
        {
            // OS X Finder metadata file
            ".DS_Store",

            // OS X Finder icon file (yes, that's a newline in the file name!)
            "Icon\r",

            // Windows Explorer image thumbnail cache file
            "Thumbs.db",
        };

        public class CannotHappenException : Exception
        {
            public CannotHappenException(string message) : base("Can't happen: " + message)
            {
            }
        }

        public class Info
        {
            [Flags]
            public enum TypeEn
            {
                None  = 0,
                File  = 1,
                Dir   = 2,
                Other = 4,
            }

            public static string[] TypeNames = new string[]
            {
                "none",
                "file",
                "dir",
                "other",
            };

            public static string GetTypeName(ulong type)
            {
                List<string> names = new List<string>();
                MapBits(type, delegate(int i)
                {
                    names.Add((i < TypeNames.Length) ? TypeNames[i] : String.Format("{0}", i));
                });
                return String.Join("|", names.ToArray());
            }

            public static string GetTypeName(TypeEn type)
            {
                return GetTypeName((ulong)type);
            }

            public static void AssertValidType(TypeEn type)
            {
                if ((type == TypeEn.None) ||
                    (type == TypeEn.File) ||
                    (type == TypeEn.Dir)  ||
                    (type == TypeEn.Other))
                {
                    return;
                }
                throw new CannotHappenException(String.Format("Bad type: {0}", GetTypeName(type)));
            }

            public TypeEn Type;
            public ulong Size;
            public DateTime Time;
            private HashSet<string> containedIgnoredFiles;

            public void AddIgnoredFile(string filename)
            {
                if (this.containedIgnoredFiles == null)
                    this.containedIgnoredFiles = new HashSet<string>();
                this.containedIgnoredFiles.Add(filename);
            }

            public IEnumerable<string> ContainedIgnoredFiles
            {
                get 
                {
                    if (this.containedIgnoredFiles == null)
                        return new string[0].AsEnumerable();
                    else
                        return this.containedIgnoredFiles.AsEnumerable();
                }
            }

            private bool TimesEqual(DateTime a, DateTime b)
            {
                // This is complicated because not all file systems record the last modification time
                // as UTC to a precision of one second.  The FAT file system records local time, possibly
                // affected by daylight saving, to two second precision.

                ulong diff = (ulong)Math.Abs((a - b).TotalSeconds); // Compute difference to one-second precision.
                diff -= diff % 2; // Downgrade to two-second precision.
                // Is the time difference exactly some multiple of 15 minutes
                // (the most granular division that timezones have)?
                return (0 == (diff % (15*60))) && (diff <= (24*60*60));
            }

            public bool Equals(Info other)
            {
                return((this.Type == other.Type) &&
                       ((this.Type != TypeEn.File) ||
                        ((this.Size == other.Size) &&
                         TimesEqual(this.Time, other.Time))));
            }
        }

        public class Ent
        {
            public TreeNode Node;
            public Info[] Infos;
            public string Name { get { return Node.Text; } }
            public int Winner = WinnerInherit;

            public int ActualWinner
            {
                get
                {
                    if (Winner != WinnerInherit) return Winner;
                    if (Node.Parent == null)
                        throw new CannotHappenException("Tried to compute ActualWinner for root node");
                    return ((Ent)Node.Parent.Tag).ActualWinner;
                }
            }

            public Ent(Conn[] conns)
            {
                Infos = new Info[MaxConnCount];
                for (int c = 0; c < MaxConnCount; c++)
                    if (conns[c] != null)
                    {
                        var info = new Info();
                        info.Type = Info.TypeEn.None;
                        Infos[c] = info;
                    }
            }

            public bool AllInfosAreDirs
            {
                get
                {
                    foreach (Info info in Infos)
                    {
                        if (info == null) continue;
                        if (info.Type != Info.TypeEn.Dir) return false;
                    }
                    return true;
                }
            }

            public bool AllInfosAreIdenticalFiles
            {
                get
                {
                    Info key = null;
                    foreach(Info info in Infos)
                    {
                        if(info == null)
                            continue;
                        if(info.Type != Info.TypeEn.File)
                            return(false);
                        if(key == null)
                            key = info;
                        if(!key.Equals(info))
                            return(false);
                    }
                    return(key != null);
                }
            }
            
            public void AddUnder(Ent dirent, string name)
            {
                this.Node = dirent.Node.Nodes.Add(name);
                this.Node.Tag = this;
            }

            public void MakeRootOf(TreeNodeCollection nodes)
            {
                nodes.Clear();
                this.Node = nodes.Add("<root>");
                this.Node.Tag = this;
                this.Winner = WinnerPreserve;
            }
        }

        private Conn[] conns = new Conn[MaxConnCount];

        private int NextFreeConnIndex
        {
            get
            {
                for(int c = 0; c < MaxConnCount; c++)
                    if(conns[c] == null)
                        return(c);
                return(-1);
            }
        }

        public void AddConn(Conn conn)
        {
            const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            int i = NextFreeConnIndex;
            conns[i] = conn;
            conn.Title = String.Format("{0}", letters[i]);
        }

        private delegate void MapBitsFn(int i);

        private static void MapBits(ulong bitmask, MapBitsFn fn)
        {
            for(int i = 0; i < 64; i++)
                if(0 != (bitmask & ((ulong)1 << i)))
                    fn(i);
        }

        private delegate bool MapIntoBitsFn(int i);

        private static ulong MapIntoBits(int limit, MapIntoBitsFn fn)
        {
            ulong bitmask = 0;
            for(int i = 0; i < limit; i++)
                if(fn(i))
                    bitmask |= (ulong)1 << i;
            return(bitmask);
        }

        //===========================================================
        // Analyze phase
        //===========================================================

        private bool ShouldIgnore(string name, Info info)
        {
            return ((info.Type == Info.TypeEn.File) &&
                (IgnoredFileNames.Contains(name, StringComparer.InvariantCultureIgnoreCase)));
        }

        private void AnalyzeDir(Ent dirent, ulong dirconnmask)
        {
            var ents = new Dictionary<string, Ent>();
            MapBits(dirconnmask, delegate(int c) {
                    conns[c].MapDirEntries(delegate(string name, Info info) {
                        if (ShouldIgnore(name, info))
                            dirent.Infos[c].AddIgnoredFile(name);
                        else
                        {
                            if (!ents.ContainsKey(name))
                            {
                                ents[name] = new Ent(conns);
                            }
                            ents[name].Infos[c] = info;
                        }
                    });
            });
            foreach (KeyValuePair<string,Ent> kvp in ents)
            {
                string name = kvp.Key;
                Ent ent = kvp.Value;

                if(ent.AllInfosAreIdenticalFiles)
                    continue;

                ent.AddUnder(dirent, name);

                ulong connmask = MapIntoBits(MaxConnCount, delegate(int c) { 
                        return((ent.Infos[c] != null) && (ent.Infos[c].Type == Info.TypeEn.Dir));
                    });
                
                if(connmask != 0)
                {
                    MapBits(connmask, delegate(int c) { conns[c].DescendDir(name); });
                    AnalyzeDir(ent, connmask);
                    MapBits(connmask, delegate(int c) { conns[c].AscendDir(); });
                }

                if (ent.AllInfosAreDirs && (ent.Node.Nodes.Count == 0))
                    ent.Node.Remove();
            }
        }

        private ulong AllConnsMask
        {
            get
            {
                return(MapIntoBits(MaxConnCount, delegate(int c) { return(conns[c] != null); }));
            }
        }

        public void Analyze(TreeNodeCollection nodes)
        {
            Ent rootEnt = new Ent(conns);
            rootEnt.MakeRootOf(nodes);
            for (int i = 0; i < MaxConnCount; i++)
                if (conns[i] != null)
                {
                    rootEnt.Infos[i] = new Info();
                    rootEnt.Infos[i].Type = Info.TypeEn.Dir;
                    conns[i].GoToDir(new string[0]); // Go to root dir.
                }
            AnalyzeDir(rootEnt, AllConnsMask);
        }

        //===========================================================
        // Merge phase
        //===========================================================

        delegate void IntToVoidFunc(int i);

        private void MapInfosThatExist(Ent ent, IntToVoidFunc mapfunc)
        {
            for (int i = 0; i < MaxConnCount; i++)
            {
                if (ent.Infos[i] != null)
                {
                    Info.AssertValidType(ent.Infos[i].Type);
                    mapfunc(i);
                }
            }
        }

        private Info.TypeEn MergeRecurseSubs(Ent ent, List<string> path, List<Op> ops)
        {
            Info.TypeEn subbecomes = 0;
            if (ent.Node.Parent != null)
                path.Add(ent.Name);
            foreach (TreeNode subnode in ent.Node.Nodes)
            {
                Ent subent = (Ent)subnode.Tag;
                subbecomes |= MergeRecurse(subent, path, ops);
            }
            if (ent.Node.Parent != null)
                path.RemoveAt(path.Count - 1);
            return subbecomes;
        }

        private Info.TypeEn MergeRecurse(Ent ent, List<string> path, List<Op> ops)
        {
            Info.TypeEn becomes = 0;
            if (ent.ActualWinner == WinnerPreserve)
            {
                for (int i = 0; i < MaxConnCount; i++)
                    if (ent.Infos[i] != null)
                        becomes |= ent.Infos[i].Type;
                if (0 == (becomes & ~(Info.TypeEn.Dir | Info.TypeEn.Other)))
                    MergeRecurseSubs(ent, path, ops);
                else
                    AssertNoChildren(ent, path, ops);
            }
            else
            {
                becomes = ent.Infos[ent.ActualWinner].Type;
                Info.AssertValidType(becomes);
                switch (becomes)
                {
                    case Info.TypeEn.None:
                        AssertNoChildren(ent, path, ops);
                        MapInfosThatExist(ent, delegate (int i) {
                            AddEntOpsWhenNoneWins(ent, path, ops, i);
                        });
                        break;
                    case Info.TypeEn.File:
                        AssertNoChildren(ent, path, ops);
                        MapInfosThatExist(ent, delegate (int i) {
                            AddEntOpsWhenFileWins(ent, path, ops, i);
                        });
                        break;
                    case Info.TypeEn.Dir:
                        MapInfosThatExist(ent, delegate (int i) {
                            AddEntOpsWhenDirWins(ent, path, ops, i);
                        });
                        MergeRecurseSubs(ent, path, ops);
                        break;
                    case Info.TypeEn.Other:
                        MapInfosThatExist(ent, delegate (int i) {
                            AddEntOpsWhenOtherWins(ent, i);
                        });
                        break;
                }
            }
            return becomes;
        }

        private void AssertNoChildren(Ent ent, List<string> path, List<Op> ops)
        {
            Info.TypeEn subbecomes = MergeRecurseSubs(ent, path, ops);
            if (subbecomes != 0)
            {
                throw new CannotHappenException("Should delete all siblings but for some of them children remain.");
            }
        }

        private void AddEntOpsWhenNoneWins(Ent ent, List<string> path, List<Op> ops, int i)
        {
            switch (ent.Infos[i].Type)
            {
                case Info.TypeEn.None:
                    // Nothing to do.
                    break;
                case Info.TypeEn.File:
                    ops.Add(new Op.DeleteFile(path.ToArray(), conns[i], ent.Name));
                    break;
                case Info.TypeEn.Dir:
                    AddDeleteEmptyDirOp(ops, i, ent, path);
                    break;
                case Info.TypeEn.Other:
                    ops.Add(new Op.DeleteFile(path.ToArray(), conns[i], ent.Name));
                    break;
            }
        }

        private void AddEntOpsWhenFileWins(Ent ent, List<string> path, List<Op> ops, int i)
        {
            switch (ent.Infos[i].Type)
            {
                case Info.TypeEn.None:
                    ops.Add(new Op.CreateFile(path.ToArray(), conns[ent.ActualWinner], conns[i], ent.Name, ent.Infos[ent.ActualWinner].Time));
                    break;
                case Info.TypeEn.File:
                    if (!ent.Infos[i].Equals(ent.Infos[ent.ActualWinner]))
                    {
                        ops.Add(new Op.DeleteFile(path.ToArray(), conns[i], ent.Name));
                        ops.Add(new Op.CreateFile(path.ToArray(), conns[ent.ActualWinner], conns[i], ent.Name, ent.Infos[ent.ActualWinner].Time));
                    }
                    break;
                case Info.TypeEn.Dir:
                    AddDeleteEmptyDirOp(ops, i, ent, path);
                    ops.Add(new Op.CreateFile(path.ToArray(), conns[ent.ActualWinner], conns[i], ent.Name, ent.Infos[ent.ActualWinner].Time));
                    break;
                case Info.TypeEn.Other:
                    ops.Add(new Op.DeleteFile(path.ToArray(), conns[i], ent.Name));
                    break;
            }
        }

        private void AddEntOpsWhenDirWins(Ent ent, List<string> path, List<Op> ops, int i)
        {
            switch (ent.Infos[i].Type)
            {
                case Info.TypeEn.None:
                    ops.Add(new Op.CreateEmptyDir(path.ToArray(), conns[i], ent.Name));
                    break;
                case Info.TypeEn.File:
                    ops.Add(new Op.DeleteFile(path.ToArray(), conns[i], ent.Name));
                    ops.Add(new Op.CreateEmptyDir(path.ToArray(), conns[i], ent.Name));
                    break;
                case Info.TypeEn.Dir:
                    // Nothing to do.
                    break;
                case Info.TypeEn.Other:
                    ops.Add(new Op.DeleteFile(path.ToArray(), conns[i], ent.Name));
                    break;
            }
        }

        private static void AddEntOpsWhenOtherWins(Ent ent, int i)
        {
            switch (ent.Infos[i].Type)
            {
                case Info.TypeEn.None:
                    // Nothing to do.
                    break;
                case Info.TypeEn.File:
                    // Nothing to do.
                    break;
                case Info.TypeEn.Dir:
                    // Nothing to do.
                    break;
                case Info.TypeEn.Other:
                    // Nothing to do.
                    break;
            }
        }

        private void AddDeleteEmptyDirOp(List<Op> ops, int c, Ent ent, List<string> path)
        {
            foreach (string ignoredFileName in ent.Infos[c].ContainedIgnoredFiles)
                ops.Add(new Op.DeleteFile(path.Concat(new string[] {ent.Name}).ToArray(), conns[c], ignoredFileName));
            ops.Add(new Op.DeleteEmptyDir(path.ToArray(), conns[c], ent.Name));
        }

        public List<Op> Merge(TreeNodeCollection nodes)
        {
            List<Op> ops = new List<Op>();
            if (nodes.Count > 0)
            {
                TreeNode rootNode = nodes[0];
                Ent rootEnt = (Ent)rootNode.Tag;
                MergeRecurse(rootEnt, new List<string>(), ops);
            }
            // NOTE: I'm relying on the fact that OrderBy is a stable sort.
            return ops.OrderBy(x => x).ToList();
        }
    }
}
