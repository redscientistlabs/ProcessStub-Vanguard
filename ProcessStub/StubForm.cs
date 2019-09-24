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
using RTCV.NetCore.StaticTools;
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
            UICore.SetRTCColor(Color.FromArgb(149, 120, 161), this);

            tbFilterText.DeselectAll();
            tbAutoAttach.DeselectAll();
            this.Focus();

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


            btnBrowseTarget.Visible = false;
            originalLbTargetSize = lbTarget.Size;
            lbTarget.Size = new Size(lbTarget.Size.Width + diff, lbTarget.Size.Height);
            btnUnloadTarget.Visible = true;
            btnRefreshDomains.Visible = true;

            ProcessWatch.EnableInterface();
        }

        public void DisableTargetInterface()
        {
            btnUnloadTarget.Visible = false;
            btnRefreshDomains.Visible = false;
            btnBrowseTarget.Visible = true;
            
            lbTarget.Size = originalLbTargetSize;
            lbTarget.Location = originalLbTargetLocation;
            lbTarget.Visible = false;
            

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


        private void StubForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!ProcessWatch.CloseTarget(false))
                e.Cancel = true;
        }

        private void BtnTargetSettings_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Control c = (Control)sender;
                Point locate = new Point(c.Location.X + e.Location.X, ((Control)sender).Location.Y + e.Location.Y);

                ContextMenuStrip columnsMenu = new ContextMenuStrip();


                ((ToolStripMenuItem)columnsMenu.Items.Add("Use AutoHook", null, new EventHandler((ob, ev) =>
                {

                    ProcessWatch.AutoHookTimer.Enabled = !ProcessWatch.AutoHookTimer.Enabled;
                    tbAutoAttach.Enabled = ProcessWatch.AutoHookTimer.Enabled;

                }))).Checked = ProcessWatch.AutoHookTimer.Enabled;

                ((ToolStripMenuItem)columnsMenu.Items.Add("Use Filtering", null, new EventHandler((ob, ev) =>
                {

                    ProcessWatch.UseFiltering = !ProcessWatch.UseFiltering;
                    Params.SetParam("USEFILTERING", ProcessWatch.UseFiltering.ToString());

                    if (VanguardCore.vanguardConnected)
                        ProcessWatch.UpdateDomains();

                }))).Checked = ProcessWatch.UseFiltering;

                ((ToolStripMenuItem)columnsMenu.Items.Add("Use Exception Handler Override", null, new EventHandler((ob, ev) =>
                {

                    ProcessWatch.UseExceptionHandler = !ProcessWatch.UseExceptionHandler;
                    Params.SetParam("USEEXCEPTIONHANDLER", ProcessWatch.UseExceptionHandler.ToString());


                }))).Checked = ProcessWatch.UseExceptionHandler;

                ((ToolStripMenuItem)columnsMenu.Items.Add("Use Blacklist", null, new EventHandler((ob, ev) =>
                {

                    ProcessWatch.UseBlacklist = !ProcessWatch.UseBlacklist;
                    Params.SetParam("USEBLACKLIST", ProcessWatch.UseBlacklist.ToString());


                }))).Checked = ProcessWatch.UseBlacklist;
                
                columnsMenu.Items.Add("Select Memory Protection Modes", null, new EventHandler((ob, ev) =>
                {
                    S.GET<MemoryProtectionSelector>().ShowDialog();
                }));


                columnsMenu.Show(this, locate);
            }
        }

        private void BtnRefreshDomains_Click(object sender, EventArgs e)
        {
            if (VanguardCore.vanguardConnected)
                ProcessWatch.UpdateDomains();
        }
    }
}
