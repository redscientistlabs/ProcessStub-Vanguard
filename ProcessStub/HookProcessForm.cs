namespace ProcessStub
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Drawing;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using RTCV.CorruptCore;
    using RTCV.ProcessCorrupt;
    using RTCV.UI;

    public partial class HookProcessForm : Form
    {
        public Process RequestedProcess;

        public HookProcessForm()
        {
            InitializeComponent();
        }

        private void HookProcessForm_Shown(object sender, EventArgs e)
        {
            this.Focus();
        }

        private void HookProcessForm_Load(object sender, EventArgs e)
        {
            Colors.SetRTCColor(Color.FromArgb(149, 120, 161), this);
            lvProcesses.Columns.Add("Process", lvProcesses.Width - 20);

            var inProcesses = Process.GetProcesses();
            lvProcesses.SmallImageList = ProcessWatch.ProcessIcons;
            lvProcesses.HeaderStyle = ColumnHeaderStyle.None;

            Bitmap emptybmp = new Bitmap(32, 32);
            using (Graphics gr = Graphics.FromImage(emptybmp))
            {
                gr.Clear(Color.Transparent);
            }

            foreach (var process in inProcesses.OrderBy(it => $"{it.ProcessName}:{it.Id}"))
            {
                try
                {
                    if (!ProcessWatch.IsExecutableNameBlacklisted(process.ProcessName))
                    {
                        if (!ProcessWatch.ProcessIcons.Images.ContainsKey(process.ProcessName))
                        {
                            var icon = process.GetIcon();
                            if (icon == null)
                            {
                                ProcessWatch.ProcessIcons.Images.Add(process.ProcessName, emptybmp);
                            }
                            else
                                ProcessWatch.ProcessIcons.Images.Add(process.ProcessName, icon);
                        }

                        lvProcesses.Items.Add(new ListViewItem($"{process.ProcessName} : {process.Id}", process.ProcessName) { Tag = process });
                    }
                }
                catch (Win32Exception ex)
                {
                    var name = process?.ProcessName ?? "UNKNOWN";
                    Console.WriteLine($"Couldn't access process {name}. Error {ex.Message}");
                }
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            lvProcesses.Items.Clear();
            DialogResult = DialogResult.Cancel;
        }

        private void btnSendList_Click(object sender, EventArgs e)
        {
            if (lvProcesses.SelectedItems.Count == 0)
            {
                MessageBox.Show("There's no process selected");
                return;
            }

            RequestedProcess = (Process)lvProcesses.SelectedItems[0].Tag;
            var name = RequestedProcess?.ProcessName ?? "UNKNOWN";
            try
            {
                if (RequestedProcess?.HasExited ?? true)
                {
                    MessageBox.Show($"Couldn't access process {name}. The process has already exited");
                    RequestedProcess = null;
                    DialogResult = DialogResult.Abort;
                    Close();
                }

                if (ProcessWatch.IsProcessBlacklisted(RequestedProcess))
                {
                    MessageBox.Show($"Couldn't access process {name}. The process is blacklisted!");
                    RequestedProcess = null;
                    DialogResult = DialogResult.Abort;
                    Close();
                }
            }
            catch (Win32Exception ex)
            {
                MessageBox.Show($"Couldn't access process {name}. Error {ex.Message}");
                RequestedProcess = null;
                DialogResult = DialogResult.Abort;
                Close();
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
