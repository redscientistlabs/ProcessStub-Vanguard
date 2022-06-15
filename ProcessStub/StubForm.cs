namespace ProcessStub
{
    using System;
    using System.Diagnostics;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Windows.Forms;
    using System.Linq;
    using RTCV.Common;
    using RTCV.NetCore;
    using RTCV.UI;
    using Vanguard;
    using System.Threading;

    public partial class StubForm : Form
    {
        private Point originalLbTargetLocation;

        private Size originalLbTargetSize;

        public StubForm()
        {
            InitializeComponent();

            SyncObjectSingleton.SyncObject = this;

            Text += " " + ProcessWatch.ProcessStubVersion;
        }

        private void StubForm_Load(object sender, EventArgs e)
        {
            Colors.SetRTCColor(Color.FromArgb(149, 120, 161), this);

            tbFilterText.DeselectAll();
            tbAutoAttach.DeselectAll();
            Focus();

            ProcessWatch.Start();
        }

        public void RunProgressBar(string progressLabel, int maxProgress, Action<object, EventArgs> action, Action<object, EventArgs> postAction = null)
        {
            if (ProcessWatch.progressForm != null)
            {
                ProcessWatch.progressForm.Close();
                Controls.Remove(ProcessWatch.progressForm);
                ProcessWatch.progressForm = null;
            }

            ProcessWatch.progressForm = new ProgressForm(progressLabel, maxProgress, action, postAction);
            ProcessWatch.progressForm.Run();
        }

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
            btnRehook.Visible = true;
            btnRefreshDomains.Visible = true;

            ProcessWatch.EnableInterface();
        }

        public void DisableTargetInterface()
        {
            btnUnloadTarget.Visible = false;
            btnRehook.Visible = false;

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

                ((ToolStripMenuItem)columnsMenu.Items.Add("Enable Auto Re-Hook", null, (ob, ev) =>
                {
                    ProcessWatch.AutoHookTimer.Enabled = !ProcessWatch.AutoHookTimer.Enabled;
                })).Checked = ProcessWatch.AutoHookTimer.Enabled;

                ((ToolStripMenuItem)columnsMenu.Items.Add("Use Filtering", null, (ob, ev) =>
                {
                    ProcessWatch.UseFiltering = !ProcessWatch.UseFiltering;
                    Params.SetParam("USEFILTERING", ProcessWatch.UseFiltering.ToString());

                    if (VanguardCore.vanguardConnected)
                        ProcessWatch.UpdateDomains();
                })).Checked = ProcessWatch.UseFiltering;

                /*
                ((ToolStripMenuItem)columnsMenu.Items.Add("Use Exception Handler Override", null, (ob, ev) =>
                {

                    ProcessWatch.UseExceptionHandler = !ProcessWatch.UseExceptionHandler;
                    Params.SetParam("USEEXCEPTIONHANDLER", ProcessWatch.UseExceptionHandler.ToString());


                })).Checked = ProcessWatch.UseExceptionHandler;
                */
                ((ToolStripMenuItem)columnsMenu.Items.Add("Use Blacklist", null, (ob, ev) =>
                {
                    ProcessWatch.UseBlacklist = !ProcessWatch.UseBlacklist;
                    Params.SetParam("USEBLACKLIST", ProcessWatch.UseBlacklist.ToString());
                })).Checked = ProcessWatch.UseBlacklist;
                ((ToolStripMenuItem)columnsMenu.Items.Add("Suspend Process on Corrupt", null, (ob, ev) =>
                {
                    ProcessWatch.SuspendProcess = !ProcessWatch.SuspendProcess;
                    Params.SetParam("SUSPENDPROCESS", ProcessWatch.SuspendProcess.ToString());
                })).Checked = ProcessWatch.SuspendProcess;

                columnsMenu.Items.Add(new ToolStripSeparator());
                columnsMenu.Items.Add("Select Memory Protection Modes to Corrupt", null, (ob, ev) =>
                {
                    S.GET<MemoryProtectionSelector>().ShowDialog();
                });

                columnsMenu.Show(this, locate);
            }
        }

        private void BtnRefreshDomains_Click(object sender, EventArgs e)
        {
            if (VanguardCore.vanguardConnected)
                ProcessWatch.UpdateDomains();
        }

        private void btnRehook_Click(object sender, EventArgs e)
        {
            string currentTarget = S.GET<StubForm>().tbAutoAttach.Text;

            try
            {
            ProcessWatch.CloseTarget();

            Thread.Sleep(2000); //Give the process 2 seconds

            var inProcesses = Process.GetProcesses();
            var listProcesses = new List<Process>(inProcesses);
            Process p = listProcesses.FirstOrDefault(it => it.ProcessName == currentTarget);

            //fetch new process here

            if (p == null)
                return;

            //re-hook
            ProcessWatch.LoadTarget(p);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to Re-hook process {currentTarget}\n\n{ex}");
            }
        }
    }
}
