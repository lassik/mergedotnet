using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Merge
{
    public partial class MainForm : Form
    {
        private Core core = new Core();

        public MainForm()
        {
            InitializeComponent();
        }

        private void Log(string msg)
        {
            // TODO: Logging is disabled for now, maybe re-enable it later?
        }

        private void Recurse(string path)
        {
            string old = Directory.GetCurrentDirectory();
            try
            {
                for (; ; )
                {
                    try
                    {
                        Directory.SetCurrentDirectory(path);
                        break;
                    }
                    catch (IOException)
                    {
                        if (MessageBox.Show("IO error, try again?", "Error", MessageBoxButtons.RetryCancel) == DialogResult.Cancel)
                            return;
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                Log("UnauthorizedAccessException for " + Path.Combine(old, path));
                return;
            }
            catch (PathTooLongException)
            {
                Log("PathTooLongException for " + Path.Combine(old, path));
                return;
            }

            try
            {
                DirectoryInfo di = new DirectoryInfo(".");
                foreach (FileSystemInfo ent in di.GetFileSystemInfos())
                {
                    if ((ent.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                        if ((ent.Name != ".") && (ent.Name != ".."))
                            Recurse(ent.Name);
                }
            }
            finally
            {
                Directory.SetCurrentDirectory(old);
            }
        }

        private void ShowMenuUnderButton(ContextMenuStrip mnu, Button btn)
        {
            Point pt = PointToScreen(btn.Location);
            pt.Y += btn.Height;
            mnu.Show(pt);
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            ShowMenuUnderButton(mnuAdd, btnAdd);
        }

        private void btnDecide_Click(object sender, EventArgs e)
        {
            ShowMenuUnderButton(mnuDecide, btnDecide);
        }

        private void localToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                core.AddConn(new LocalConn(folderBrowserDialog.SelectedPath));
            }
        }

        private void sFTPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var connectForm = new SftpConnectForm();
            connectForm.AcceptConn += core.AddConn;
            connectForm.Show();
        }

        private void btnAnalyze_Click(object sender, EventArgs e)
        {
            core.Analyze(treeView.Nodes);
        }

        private string HumanizedByteSize(ulong size)
        {
            double d = (double)size;

            if (d > (double)1024*1024*1024)
                return (String.Format("{0:0.#} GiB", d/((double)1024*1024*1024)));
            if (d > (double)1024*1024)
                return (String.Format("{0:0.#} MiB", d/((double)1024*1024)));
            if (d > (double)1024)
                return (String.Format("{0:0.#} KiB", d/((double)1024)));
            return (String.Format("{0} B", d));
        }

        private string HumanizedDate(DateTime utcTime)
        {
            return((DateTime.UtcNow - utcTime).Days.ToString() + "d ago");
        }

        private string DescribeInfo(Core.Info info)
        {
            if (info.Type == Core.Info.TypeEn.File)
                return HumanizedByteSize(info.Size) + " " + HumanizedDate(info.Time);
            else
                return info.Type.ToString();
        }

        private void GetInfoFontAndBrush(Core.Ent ent, int i, out Font font, out Brush brush)
        {
            bool direct = (ent.ActualWinner == ent.Winner);
            if (ent.ActualWinner == Core.WinnerPreserve)
            {
                font = treeView.Font;
                brush = new SolidBrush(direct ? Color.Black : Color.Gray);
            }
            else if (ent.ActualWinner == i)
            {
                font = treeView.Font;
                brush = new SolidBrush(direct ? Color.Blue : Color.DarkBlue);
            }
            else
            {
                font = new Font(treeView.Font, FontStyle.Strikeout);
                brush = new SolidBrush(direct ? Color.Red : Color.DarkRed);
            }
        }

        private void treeView_DrawNode(object sender, DrawTreeNodeEventArgs e)
        {
            int colstart = 500;
            int colwidth = 100;
            Core.Ent ent = (Core.Ent)e.Node.Tag;

            e.Graphics.FillRectangle(new SolidBrush(treeView.BackColor), e.Bounds);
            e.Graphics.DrawString(ent.Name, treeView.Font, new SolidBrush(SelectedNodes.Contains(e.Node) ? Color.Green : Color.Black), new Point(e.Bounds.X, e.Bounds.Y));
            for (int i = 0; i < ent.Infos.Length; i++)
            {
                Font font;
                Brush brush;
                GetInfoFontAndBrush(ent, i, out font, out brush);
                if (ent.Infos[i] != null)
                    e.Graphics.DrawString(DescribeInfo(ent.Infos[i]), font, brush,
                        new Point(colstart + i * colwidth, e.Bounds.Y));
            }
        }

        private void mnuDecide_Wins_Click(object sender, EventArgs e)
        {
            foreach (TreeNode node in SelectedNodes)
            {
                var ent = (Core.Ent)node.Tag;
                int newWinner = (int)((ToolStripMenuItem)sender).Tag;
                ent.Winner = newWinner;
                treeView.Refresh();
            }
        }

        private void AddTaggedMenuItem(ContextMenuStrip menu, string text, EventHandler click, object tag)
        {
            var item = new ToolStripMenuItem();
            item.Text = text;
            item.Tag = tag;
            item.Click += click;
            menu.Items.Add(item);
        }

        private void mnuDecide_Opening(object sender, CancelEventArgs e)
        {
            mnuDecide.Items.Clear();
            AddTaggedMenuItem(mnuDecide, "Inherit", mnuDecide_Wins_Click, Core.WinnerInherit);
            AddTaggedMenuItem(mnuDecide, "Preserve", mnuDecide_Wins_Click, Core.WinnerPreserve);
            for (int c = 0; c < Core.MaxConnCount; c++)
                AddTaggedMenuItem(mnuDecide, String.Format("Conn {0} wins", c), mnuDecide_Wins_Click, c);
        }

        private void btnMerge_Click(object sender, EventArgs e)
        {
            List<Op> ops = core.Merge(treeView.Nodes);
            OpForm opForm = new OpForm();
            opForm.Populate(ops);
            opForm.Show();
        }

        //====================================================================================

        // http://www.arstdesign.com/articles/treeviewms.html

        protected List<TreeNode> m_coll;
        protected TreeNode       m_lastNode, m_firstNode;

        public List<TreeNode> SelectedNodes
        {
            get
            {
                return m_coll;
            }
            set
            {
                removePaintFromNodes();
                m_coll.Clear();
                m_coll = value;
                paintSelectedNodes();
            }
        }

        private bool isParent(TreeNode possibleParent, TreeNode child)
        {
            if (child.Parent == possibleParent)
                return true;
            if (possibleParent == null)
                return false;
            return isParent(possibleParent.Parent, child);
        }

        private void treeView_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            // e.Node is the current node exposed by the base TreeView control
            //base.OnBeforeSelect(e);

            bool bControl = (ModifierKeys == Keys.Control);
            bool bShift = (ModifierKeys == Keys.Shift);

            // selecting twice the node while pressing CTRL ?
            if (bControl && m_coll.Contains(e.Node))
            {
                // unselect it (let framework know we don't want selection this time)
                e.Cancel = true;

                // update nodes
                removePaintFromNodes();
                m_coll.Remove(e.Node);
                paintSelectedNodes();
                return;
            }

            m_lastNode = e.Node;
            if (!bShift) m_firstNode = e.Node; // store begin of shift sequence
        }

        private void treeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            // e.Node is the current node exposed by the base TreeView control

            //base.OnAfterSelect(e);

            bool bControl = (ModifierKeys == Keys.Control);
            bool bShift = (ModifierKeys == Keys.Shift);

            if (bControl)
            {
                if (!m_coll.Contains(e.Node)) // new node ?
                {
                    m_coll.Add(e.Node);
                }
                else  // not new, remove it from the collection
                {
                    removePaintFromNodes();
                    m_coll.Remove(e.Node);
                }
                paintSelectedNodes();
            }
            else
            {
                if (bShift)
                {
                    Queue<TreeNode> myQueue = new Queue<TreeNode>();

                    TreeNode uppernode = m_firstNode;
                    TreeNode bottomnode = e.Node;

                    // case 1 : begin and end nodes are parent
                    bool bParent = isParent(m_firstNode, e.Node); // is m_firstNode parent (direct or not) of e.Node
                    if (!bParent)
                    {
                        bParent = isParent(bottomnode, uppernode);
                        if (bParent) // swap nodes
                        {
                            TreeNode t = uppernode;
                            uppernode = bottomnode;
                            bottomnode = t;
                        }
                    }
                    if (bParent)
                    {
                        TreeNode n = bottomnode;
                        while (n != uppernode.Parent)
                        {
                            if (!m_coll.Contains(n)) // new node ?
                                myQueue.Enqueue(n);

                            n = n.Parent;
                        }
                    }
                    // case 2 : nor the begin nor the end node are descendant one another
                    else
                    {
                        if ((uppernode.Parent == null && bottomnode.Parent == null) || (uppernode.Parent != null && uppernode.Parent.Nodes.Contains(bottomnode))) // are they siblings ?
                        {
                            int nIndexUpper = uppernode.Index;
                            int nIndexBottom = bottomnode.Index;
                            if (nIndexBottom < nIndexUpper) // reversed?
                            {
                                TreeNode t = uppernode;
                                uppernode = bottomnode;
                                bottomnode = t;
                                nIndexUpper = uppernode.Index;
                                nIndexBottom = bottomnode.Index;
                            }

                            TreeNode n = uppernode;
                            while (nIndexUpper <= nIndexBottom)
                            {
                                if (!m_coll.Contains(n)) // new node ?
                                    myQueue.Enqueue(n);

                                n = n.NextNode;

                                nIndexUpper++;
                            } // end while

                        }
                        else
                        {
                            if (!m_coll.Contains(uppernode)) myQueue.Enqueue(uppernode);
                            if (!m_coll.Contains(bottomnode)) myQueue.Enqueue(bottomnode);
                        }

                    }

                    m_coll.AddRange(myQueue);

                    paintSelectedNodes();
                    m_firstNode = e.Node; // let us chain several SHIFTs if we like it

                } // end if m_bShift
                else
                {
                    // in the case of a simple click, just add this item
                    if (m_coll != null && m_coll.Count > 0)
                    {
                        removePaintFromNodes();
                        m_coll.Clear();
                    }
                    m_coll.Add(e.Node);
                }
            }
        }

        protected void paintSelectedNodes()
        {
            foreach (TreeNode n in m_coll)
            {
                n.BackColor = SystemColors.Highlight;
                n.ForeColor = SystemColors.HighlightText;
            }
        }

        protected void removePaintFromNodes()
        {
            if (m_coll.Count == 0) return;

            TreeNode n0 = (TreeNode)m_coll[0];
            if ((n0 == null) || (n0.TreeView == null))
                return;

            Color back = n0.TreeView.BackColor;
            Color fore = n0.TreeView.ForeColor;

            foreach (TreeNode n in m_coll)
            {
                n.BackColor = back;
                n.ForeColor = fore;
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            m_coll = new List<TreeNode>();
            m_firstNode = null;
            m_lastNode = null;
        }
    }
}
