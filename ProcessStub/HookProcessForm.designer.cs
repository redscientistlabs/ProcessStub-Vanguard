namespace ProcessStub
{
    
    partial class HookProcessForm
	{
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HookProcessForm));
            this.lvProcesses = new System.Windows.Forms.ListView();
            this.btnSendList = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // lvProcesses
            // 
            this.lvProcesses.BackColor = System.Drawing.Color.Black;
            this.lvProcesses.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.lvProcesses.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvProcesses.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.lvProcesses.ForeColor = System.Drawing.Color.White;
            this.lvProcesses.FullRowSelect = true;
            this.lvProcesses.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.lvProcesses.HideSelection = false;
            this.lvProcesses.Location = new System.Drawing.Point(0, 0);
            this.lvProcesses.MultiSelect = false;
            this.lvProcesses.Name = "lvProcesses";
            this.lvProcesses.Size = new System.Drawing.Size(319, 378);
            this.lvProcesses.TabIndex = 0;
            this.lvProcesses.Tag = "color:dark3";
            this.lvProcesses.UseCompatibleStateImageBehavior = false;
            this.lvProcesses.View = System.Windows.Forms.View.Details;
            // 
            // btnSendList
            // 
            this.btnSendList.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSendList.BackColor = System.Drawing.Color.Black;
            this.btnSendList.FlatAppearance.BorderSize = 0;
            this.btnSendList.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSendList.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnSendList.ForeColor = System.Drawing.Color.OrangeRed;
            this.btnSendList.Location = new System.Drawing.Point(111, 48);
            this.btnSendList.Name = "btnSendList";
            this.btnSendList.Size = new System.Drawing.Size(196, 23);
            this.btnSendList.TabIndex = 1;
            this.btnSendList.Tag = "color:dark2";
            this.btnSendList.Text = "Hook Process";
            this.btnSendList.UseVisualStyleBackColor = false;
            this.btnSendList.Click += new System.EventHandler(this.btnSendList_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnCancel.BackColor = System.Drawing.Color.Black;
            this.btnCancel.FlatAppearance.BorderSize = 0;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnCancel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.btnCancel.Location = new System.Drawing.Point(14, 48);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(63, 23);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Tag = "color:dark2";
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = false;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(12, 10);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(269, 26);
            this.label1.TabIndex = 3;
            this.label1.Text = "WARNING: Corrupting a game process with online \r\nfeatures can result in a ban.  D" +
    "on\'t be dumb.";
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.label2.ForeColor = System.Drawing.Color.White;
            this.label2.Location = new System.Drawing.Point(9, -171);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(0, 13);
            this.label2.TabIndex = 4;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.btnSendList);
            this.panel1.Controls.Add(this.btnCancel);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 378);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(319, 83);
            this.panel1.TabIndex = 5;
            // 
            // HookProcessForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(32)))));
            this.ClientSize = new System.Drawing.Size(319, 461);
            this.Controls.Add(this.lvProcesses);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.label2);
            this.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximumSize = new System.Drawing.Size(1000, 1000);
            this.MinimumSize = new System.Drawing.Size(335, 300);
            this.Name = "HookProcessForm";
            this.Tag = "color:dark1";
            this.Text = "Select Process";
            this.Load += new System.EventHandler(this.HookProcessForm_Load);
            this.Shown += new System.EventHandler(this.HookProcessForm_Shown);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListView lvProcesses;
        private System.Windows.Forms.Button btnSendList;
        private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Panel panel1;
    }
}