using System;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using System.Windows.Forms;
using RTCV.NetCore;
using RTCV.NetCore.StaticTools;
using RTCV.UI;
using Vanguard;

namespace ProcessStub
{
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
            UICore.SetRTCColor(Color.FromArgb(149, 120, 161), this);

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

        struct HeapInfo
        {
            public IntPtr Base;
            public IntPtr Length;
        }
        private void BtnBrowseTarget_Click(object sender, EventArgs e)
        {

            Task.Run(() =>
            {
                var server = new NamedPipeServerStream("processstub", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.None);
                server.WaitForConnection();
                StreamWriter writer = new StreamWriter(server);
                writer.WriteLine("HEAPQUERY");
                writer.Flush();
                var buf = new byte[1024];
                var a = server.Read(buf, 0, 16);
                Console.WriteLine(a);
            });

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


                ((ToolStripMenuItem)columnsMenu.Items.Add("Use AutoHook", null, (ob, ev) =>
                {

                    ProcessWatch.AutoHookTimer.Enabled = !ProcessWatch.AutoHookTimer.Enabled;
                    tbAutoAttach.Enabled = ProcessWatch.AutoHookTimer.Enabled;

                })).Checked = ProcessWatch.AutoHookTimer.Enabled;

                ((ToolStripMenuItem)columnsMenu.Items.Add("Use Filtering", null, (ob, ev) =>
                {

                    ProcessWatch.UseFiltering = !ProcessWatch.UseFiltering;
                    Params.SetParam("USEFILTERING", ProcessWatch.UseFiltering.ToString());

                    if (VanguardCore.vanguardConnected)
                        ProcessWatch.UpdateDomains();

                })).Checked = ProcessWatch.UseFiltering;

                ((ToolStripMenuItem)columnsMenu.Items.Add("Use Exception Handler Override", null, (ob, ev) =>
                {

                    ProcessWatch.UseExceptionHandler = !ProcessWatch.UseExceptionHandler;
                    Params.SetParam("USEEXCEPTIONHANDLER", ProcessWatch.UseExceptionHandler.ToString());


                })).Checked = ProcessWatch.UseExceptionHandler;

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
    }
}
