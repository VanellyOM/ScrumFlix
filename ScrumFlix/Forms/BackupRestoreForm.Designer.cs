namespace ScrumFlix.Forms
{
    partial class BackupRestoreForm
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
            btnBackup = new Button();
            btnRestore = new Button();
            btnRefresh = new Button();
            gridBackups = new DataGridView();
            progressBar = new ProgressBar();
            ((System.ComponentModel.ISupportInitialize)gridBackups).BeginInit();
            SuspendLayout();
            // 
            // btnBackup
            // 
            btnBackup.Location = new Point(12, 317);
            btnBackup.Name = "btnBackup";
            btnBackup.Size = new Size(98, 30);
            btnBackup.TabIndex = 0;
            btnBackup.Text = "Backup";
            btnBackup.UseVisualStyleBackColor = true;
            btnBackup.Click += btnBackup_Click;
            // 
            // btnRestore
            // 
            btnRestore.Location = new Point(116, 317);
            btnRestore.Name = "btnRestore";
            btnRestore.Size = new Size(98, 30);
            btnRestore.TabIndex = 1;
            btnRestore.Text = "Restore";
            btnRestore.UseVisualStyleBackColor = true;
            btnRestore.Click += btnRestore_Click;
            // 
            // btnRefresh
            // 
            btnRefresh.Location = new Point(220, 317);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new Size(98, 30);
            btnRefresh.TabIndex = 2;
            btnRefresh.Text = "Refresh";
            btnRefresh.UseVisualStyleBackColor = true;
            btnRefresh.Click += btnRefresh_Click;
            // 
            // gridBackups
            // 
            gridBackups.AllowUserToAddRows = false;
            gridBackups.AllowUserToDeleteRows = false;
            gridBackups.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gridBackups.Location = new Point(12, 12);
            gridBackups.MultiSelect = false;
            gridBackups.Name = "gridBackups";
            gridBackups.ReadOnly = true;
            gridBackups.RowHeadersWidth = 53;
            gridBackups.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridBackups.Size = new Size(652, 299);
            gridBackups.TabIndex = 3;
            // 
            // progressBar
            // 
            progressBar.Location = new Point(324, 317);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(340, 30);
            progressBar.TabIndex = 4;
            progressBar.Visible = false;
            // 
            // BackupRestoreForm
            // 
            AutoScaleDimensions = new SizeF(9F, 21F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(676, 352);
            Controls.Add(progressBar);
            Controls.Add(gridBackups);
            Controls.Add(btnRefresh);
            Controls.Add(btnRestore);
            Controls.Add(btnBackup);
            Name = "BackupRestoreForm";
            Text = "BackupRestoreForm";
            Load += BackupRestoreForm_Load;
            ((System.ComponentModel.ISupportInitialize)gridBackups).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Button btnBackup;
        private Button btnRestore;
        private Button btnRefresh;
        private DataGridView gridBackups;
        private ProgressBar progressBar;
    }
}