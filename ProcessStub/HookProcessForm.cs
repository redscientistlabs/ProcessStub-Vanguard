using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Security;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Linq;
using RTCV.CorruptCore;
using RTCV.UI;

namespace ProcessStub
{
    public partial class HookProcessForm : Form
    {
        public Process RequestedProcess;
        public HookProcessForm()
        {
            InitializeComponent();
        }

		private void HookProcessForm_Load(object sender, EventArgs e)
        {

            UICore.SetRTCColor(Color.FromArgb(149, 120, 161), this);

            lbProcesses.DisplayMember = "Name";
            lbProcesses.ValueMember = "Value";
            var p = Process.GetProcesses().OrderBy(it => $"{it.ProcessName}:{it.Id}").ToArray();
            foreach (var process in p)
            {
                try
                {
                    if (ProcessWatch.IsProcessBlacklisted(process))
                        continue;
                    lbProcesses.Items.Add(new ComboBoxItem<Process>($"{process.ProcessName}:{process.Id}", process));
                }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    var name = process?.ProcessName ?? "UNKNOWN";
                    Console.WriteLine($"Couldn't access process {name}. Error {ex.Message}");
                }
            }
        }

        
        private void btnCancel_Click(object sender, EventArgs e)
        {
            lbProcesses.Items.Clear();
            this.DialogResult = DialogResult.Cancel;;
        }

		private void btnSendList_Click(object sender, EventArgs e)
		{

			if (lbProcesses.SelectedIndex == -1)
			{
				MessageBox.Show("There's no process selected");
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }

            RequestedProcess = ((ComboBoxItem<Process>)lbProcesses.SelectedItem).Value;
            var name = RequestedProcess?.ProcessName ?? "UNKNOWN";
            try
            {
                if (RequestedProcess?.HasExited ?? true)
                {
                    MessageBox.Show($"Couldn't access process {name}. The process has already exited");
                    RequestedProcess = null;
                    this.DialogResult = DialogResult.Abort;
                    this.Close();
                }
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                MessageBox.Show($"Couldn't access process {name}. Error {ex.Message}");
                RequestedProcess = null;
                this.DialogResult = DialogResult.Abort;
                this.Close();
            }
            
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}