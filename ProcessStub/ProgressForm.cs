namespace ProcessStub
{
    using System;
    using System.ComponentModel;
    using System.Windows.Forms;
    using RTCV.Common;
    using RTCV.NetCore;

    public partial class ProgressForm : Form
    {
        public static Action<object, EventArgs> postAction;
        public BackgroundWorker bw = new BackgroundWorker();
        private readonly string defaultLabel;

        public ProgressForm(string lbText, int maxprogress, Action<object, EventArgs> actionRegistrant, Action<object, EventArgs> postActionRegistrant = null)
        {
            InitializeComponent();

            BackColor = S.GET<StubForm>().BackColor;

            bw.WorkerReportsProgress = true;
            bw.WorkerSupportsCancellation = true;

            defaultLabel = lbText;
            lbProgress.Text = defaultLabel;

            pbProgress.Minimum = 0;
            pbProgress.Value = 0;
            pbProgress.Maximum = maxprogress;

            bw.DoWork += actionRegistrant.Invoke;
            bw.ProgressChanged += Bw_ProgressChanged;
            bw.RunWorkerCompleted += Bw_RunWorkerCompleted;

            postAction = postActionRegistrant;

            TopLevel = false;
            Size = S.GET<StubForm>().Size;

            S.GET<StubForm>().Controls.Add(this);
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
            Show();
            BringToFront();
        }

        public void Run()
        {
            bw.RunWorkerAsync();
        }

        private void Bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            bw.Dispose();
            Hide();
            ProcessWatch.progressForm = null;

            if (postAction != null)
                SyncObjectSingleton.FormExecute(() =>
                {
                    postAction.Invoke(null, null);
                });
        }

        private void Bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage != 0)
            {
                if (pbProgress.Value + 1000 < pbProgress.Maximum)
                    pbProgress.Value += 1000;
                else
                    pbProgress.Value = pbProgress.Maximum;
            }

            if (e.UserState != null && e.UserState is string)
            {
                if (e.UserState as string == "DEFAULT")
                    lbProgress.Text = defaultLabel;
                else
                    lbProgress.Text = e.UserState as string;
            }
        }
    }
}
