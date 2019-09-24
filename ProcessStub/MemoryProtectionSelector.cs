using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using RTCV.CorruptCore;
using RTCV.NetCore;
using static RTCV.UI.UI_Extensions;
using RTCV.NetCore.StaticTools;
using RTCV.UI;

namespace ProcessStub
{
	public partial class MemoryProtectionSelector : Form, IAutoColorize
	{
		public MemoryProtectionSelector()
		{
			InitializeComponent();
			this.FormClosing += this.MemoryProtectionSelector_Closing;
            this.Load += MemoryProtectionSelector_Load;
            UICore.SetRTCColor(Color.FromArgb(149, 120, 161), this);
        }

        private void MemoryProtectionSelector_Load(object sender, EventArgs e)
        {
            tablePanel.Controls.Clear();
            foreach (var t in Enum.GetNames(typeof(Jupiter.MemoryProtection)).Where(x => x != Jupiter.MemoryProtection.ZeroAccess.ToString()))
            {
                CheckBox cb = new CheckBox
                {
                    AutoSize = true,
                    Text = t,
                    Name = t,
                    Checked = (ProcessWatch.ProtectMode & (Jupiter.MemoryProtection)Enum.Parse(typeof(Jupiter.MemoryProtection), t)) >= (Jupiter.MemoryProtection)Enum.Parse(typeof(Jupiter.MemoryProtection), t),
                };
                tablePanel.Controls.Add(cb);
            }
            this.Show();
        }

		private void MemoryProtectionSelector_Closing(object sender, FormClosingEventArgs e)
		{
			if (tablePanel.Controls.Cast<CheckBox>().Count(item => item.Checked) == 0)
			{
				e.Cancel = true;
				MessageBox.Show("Select at least one type of memory protection");
				return;
            }

            Jupiter.MemoryProtection a = Jupiter.MemoryProtection.ZeroAccess;
            foreach (CheckBox cb in tablePanel.Controls.Cast<CheckBox>().Where(item => item.Checked))
            {
                a = a | (Jupiter.MemoryProtection) (Enum.Parse(typeof(Jupiter.MemoryProtection), cb.Text));
            }

            ProcessWatch.ProtectMode = a;
            Params.SetParam("PROTECTIONMODE", ((uint)a).ToString());
            ProcessWatch.UpdateDomains();
        }
	}
}
