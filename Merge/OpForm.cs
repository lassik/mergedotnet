using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Merge
{
    public partial class OpForm : Form
    {
        public OpForm()
        {
            InitializeComponent();
        }

        private List<Op> ops;

        public void Populate(List<Op> ops)
        {
            this.ops = ops;
            logListBox.Items.Clear();
            foreach (Op op in ops)
            {
                logListBox.Items.Add(op.ToString());
            }
        }

        private void btnPerform_Click(object sender, EventArgs e)
        {
            int nDone = 0;
            int nTotal = ops.Count;
            var displayLastUpdated = DateTime.Now;

            foreach (Op op in ops)
            {
                var timeNow = DateTime.Now;
                if ((timeNow - displayLastUpdated).TotalSeconds >= 5)
                {
                    displayLastUpdated = timeNow;
                    this.Text = String.Format("{0} of {1} operations done", nDone, nTotal);
                    this.Update();
                }
                try
                {
                    op.Perform();
                }
                catch (Exception err)
                {
                    errorLogTextBox.AppendText(String.Format("Error {0}: {1}\n", op.ToString(), err.ToString()));
                }
                nDone++;
            }
            this.Text = "All done!";
            this.Update();
            MessageBox.Show("All done!");
            logListBox.Items.Clear();
            this.ops = null;
        }
    }
}
