using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Renci.SshNet;
using Renci.SshNet.Common;
using System.Diagnostics;

namespace Merge
{
    public partial class SftpConnectForm : Form
    {
        public SftpConnectForm()
        {
            InitializeComponent();
        }

        private SftpConn conn;

        public delegate void AcceptConnFunc(SftpConn conn);
        public event AcceptConnFunc AcceptConn;

        private string GetNodeUnixPath(TreeNode node)
        {
            string[] path = new string[node.Level+1];
            for (int i = node.Level; i > 0; i--)
            {
                path[i] = node.Text;
                node = node.Parent;
            }
            path[0] = "";
            return String.Join("/", path);
        }

        private TreeNode AddDirNode(TreeNodeCollection nodes, string filename)
        {
            TreeNode[] childNodeMatches = nodes.Find(filename, false);
            TreeNode childNode = null;
            if (childNodeMatches.Length > 0)
                childNode = childNodeMatches[0];
            else
                childNode = nodes.Add(filename);
            childNode.Name = filename;
            childNode.Text = filename;
            childNode.Tag = (object)true;
            return childNode;
        }

        private void PopulateNode(TreeNode node)
        {
            if (node.Tag != null)
            {
                Debug.Write("Populating " + GetNodeUnixPath(node));
                node.Tag = null;
                conn.GoToDir(GetNodeUnixPath(node).Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries));
                conn.MapDirEntries(delegate (string filename, Core.Info info)
                {
                    if (info.Type == Core.Info.TypeEn.Dir)
                        AddDirNode(node.Nodes, filename);
                });
            }
        }

        private void rootDirTreeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            //foreach (TreeNode child in e.Node.Nodes)
            //    PopulateNode(child);
        }

        private void rootDirTreeView_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            PopulateNode(e.Node);
        }

        private void rootDirTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            rootDirTextBox.Text = GetNodeUnixPath(e.Node);
        }

        private void connectButton_Click(object sender, EventArgs e)
        {
            if (usernameTextBox.Text.Length == 0)
            {
                MessageBox.Show("Username cannot be blank");
                return;
            }
            if (conn != null)
            {
                // TODO: close conn here
                conn = null;
            }
            rootDirTreeView.Nodes.Clear();
            try
            {
                conn = new SftpConn(hostnameTextBox.Text, usernameTextBox.Text, passwordTextBox.Text);
            }
            catch (SshException err)
            {
                MessageBox.Show(err.Message);
                return;
            }
            catch (System.Net.Sockets.SocketException err)
            {
                MessageBox.Show(err.Message);
                return;
            }
            TreeNode node = AddDirNode(rootDirTreeView.Nodes, "/");
            foreach (string pathPart in conn.FullPath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries))
                node = AddDirNode(node.Nodes, pathPart);
            node.EnsureVisible();
            rootDirTreeView.SelectedNode = node;
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            if (conn == null)
            {
                MessageBox.Show("Connect to a server first");
                return;
            }
            conn.GoToDir(rootDirTextBox.Text.Split(new char[] {'/'}, StringSplitOptions.RemoveEmptyEntries));
            conn.SetRootDirToCurrent();
            AcceptConn(conn);
            Close();
            Dispose();
        }
    }
}
