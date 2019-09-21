using RTCV.CorruptCore;
using RTCV.NetCore;
using RTCV.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Vanguard;

namespace ProcessStub
{
    public partial class StubForm : Form
    {

        public StubForm()
        {
            InitializeComponent();

            SyncObjectSingleton.SyncObject = this;


            Text += " " + ProcessWatch.ProcessStubVersion;

        }

        private void StubForm_Load(object sender, EventArgs e)
        {
            UICore.SetRTCColor(Color.Crimson, this);

            ProcessWatch.Start();
        }

        public void RunProgressBar(string progressLabel, int maxProgress, Action<object, EventArgs> action, Action<object, EventArgs> postAction = null)
        {

            if (ProcessWatch.progressForm != null)
            {
                ProcessWatch.progressForm.Close();
                this.Controls.Remove(ProcessWatch.progressForm);
                ProcessWatch.progressForm = null;
            }

            ProcessWatch.progressForm = new ProgressForm(progressLabel, maxProgress, action, postAction);
            ProcessWatch.progressForm.Run();

        }


        Size originalLbTargetSize;
        Point originalLbTargetLocation;
        public void EnableTargetInterface()
        {
            var diff = lbTarget.Location.X - btnBrowseTarget.Location.X;
            originalLbTargetLocation = lbTarget.Location;
            lbTarget.Location = btnBrowseTarget.Location;
            lbTarget.Visible = true;

            btnTargetSettings.Visible = false;

            btnBrowseTarget.Visible = false;
            originalLbTargetSize = lbTarget.Size;
            lbTarget.Size = new Size(lbTarget.Size.Width + diff, lbTarget.Size.Height);
            btnUnloadTarget.Visible = true;



            ProcessWatch.EnableInterface();
        }

        public void DisableTargetInterface()
        {
            btnUnloadTarget.Visible = false;
            btnBrowseTarget.Visible = true;
            lbTarget.Size = originalLbTargetSize;
            lbTarget.Location = originalLbTargetLocation;
            lbTarget.Visible = false;

            btnTargetSettings.Visible = true;


            lbTarget.Text = "No target selected";
            lbTargetStatus.Text = "No target selected";
        }

        private void BtnBrowseTarget_Click(object sender, EventArgs e)
        {

            if (!ProcessWatch.LoadTarget())
                return;

            if (!VanguardCore.vanguardConnected)
                VanguardCore.Start();

            EnableTargetInterface();

        }

        private void BtnReleaseTarget_Click(object sender, EventArgs e)
        {
            if (!ProcessWatch.CloseTarget())
                return;
            DisableTargetInterface();
        }

        private void BtnTargetSettings_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Control c = (Control)sender;
                Point locate = new Point(c.Location.X + e.Location.X, ((Control)sender).Location.Y + e.Location.Y);

                ContextMenuStrip columnsMenu = new ContextMenuStrip();

                columnsMenu.Show(this, locate);
            }
        }

        private void StubForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!ProcessWatch.CloseTarget(false))
                e.Cancel = true;
        }
    }
}
