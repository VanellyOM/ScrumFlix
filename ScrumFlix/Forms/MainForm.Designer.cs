namespace ScrumFlix
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            btnMovies = new Button();
            btnShowtimes = new Button();
            btnTheaterScreen = new Button();
            btnLocation = new Button();
            PicLogo = new PictureBox();
            btnUsers = new Button();
            btnConcessions = new Button();
            lblTitle = new Label();
            btnEmployees = new Button();
            lblStockAlert = new Label();
            lblTheatres = new Label();
            lblStaff = new Label();
            btnDashboard = new Button();
            btnLogout = new Button();
            btnSchedules = new Button();
            btnPayroll = new Button();
            btnReports = new Button();
            btnPOS = new Button();
            btnBackupRestore = new Button();
            ((System.ComponentModel.ISupportInitialize)PicLogo).BeginInit();
            SuspendLayout();
            // 
            // btnMovies
            // 
            btnMovies.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            btnMovies.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnMovies.Font = new Font("Microsoft Sans Serif", 17.855999F, FontStyle.Bold);
            btnMovies.Location = new Point(247, 161);
            btnMovies.Name = "btnMovies";
            btnMovies.Size = new Size(183, 105);
            btnMovies.TabIndex = 0;
            btnMovies.Text = "Manage Movies";
            btnMovies.UseVisualStyleBackColor = true;
            btnMovies.Click += btnMovies_Click;
            // 
            // btnShowtimes
            // 
            btnShowtimes.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            btnShowtimes.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnShowtimes.Font = new Font("Microsoft Sans Serif", 17.855999F, FontStyle.Bold);
            btnShowtimes.Location = new Point(436, 161);
            btnShowtimes.Name = "btnShowtimes";
            btnShowtimes.Size = new Size(196, 105);
            btnShowtimes.TabIndex = 1;
            btnShowtimes.Text = "Manage Showtimes";
            btnShowtimes.UseVisualStyleBackColor = true;
            btnShowtimes.Click += btnShowtimes_Click;
            // 
            // btnTheaterScreen
            // 
            btnTheaterScreen.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            btnTheaterScreen.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnTheaterScreen.Font = new Font("Microsoft Sans Serif", 17.855999F, FontStyle.Bold);
            btnTheaterScreen.Location = new Point(247, 272);
            btnTheaterScreen.Name = "btnTheaterScreen";
            btnTheaterScreen.Size = new Size(183, 107);
            btnTheaterScreen.TabIndex = 2;
            btnTheaterScreen.Text = "Manage Screens";
            btnTheaterScreen.UseVisualStyleBackColor = true;
            btnTheaterScreen.Click += btnTheaterScreen_Click;
            // 
            // btnLocation
            // 
            btnLocation.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            btnLocation.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnLocation.Font = new Font("Microsoft Sans Serif", 17.855999F, FontStyle.Bold);
            btnLocation.Location = new Point(436, 272);
            btnLocation.Name = "btnLocation";
            btnLocation.Size = new Size(196, 107);
            btnLocation.TabIndex = 3;
            btnLocation.Text = "Manage Locations";
            btnLocation.UseVisualStyleBackColor = true;
            btnLocation.Click += button1_Click;
            // 
            // PicLogo
            // 
            PicLogo.BackColor = Color.Transparent;
            PicLogo.Image = (Image)resources.GetObject("PicLogo.Image");
            PicLogo.Location = new Point(-21, -12);
            PicLogo.Name = "PicLogo";
            PicLogo.Size = new Size(240, 278);
            PicLogo.SizeMode = PictureBoxSizeMode.Zoom;
            PicLogo.TabIndex = 4;
            PicLogo.TabStop = false;
            // 
            // btnUsers
            // 
            btnUsers.Font = new Font("Microsoft Sans Serif", 17.855999F, FontStyle.Bold);
            btnUsers.Location = new Point(802, 296);
            btnUsers.Name = "btnUsers";
            btnUsers.Size = new Size(183, 126);
            btnUsers.TabIndex = 5;
            btnUsers.Text = "Manage User Accounts";
            btnUsers.UseVisualStyleBackColor = true;
            btnUsers.Click += btnUsers_Click;
            // 
            // btnConcessions
            // 
            btnConcessions.Font = new Font("Microsoft Sans Serif", 17.855999F, FontStyle.Bold);
            btnConcessions.Location = new Point(436, 385);
            btnConcessions.Name = "btnConcessions";
            btnConcessions.Size = new Size(196, 105);
            btnConcessions.TabIndex = 6;
            btnConcessions.Text = "Manage Concessions";
            btnConcessions.UseVisualStyleBackColor = true;
            btnConcessions.Click += btnConcessions_Click;
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Franklin Gothic Medium Cond", 47.808F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblTitle.ForeColor = Color.White;
            lblTitle.Location = new Point(489, 9);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(352, 104);
            lblTitle.TabIndex = 7;
            lblTitle.Text = "Welcome!";
            // 
            // btnEmployees
            // 
            btnEmployees.Font = new Font("Microsoft Sans Serif", 17.855999F, FontStyle.Bold);
            btnEmployees.Location = new Point(779, 161);
            btnEmployees.Name = "btnEmployees";
            btnEmployees.Size = new Size(260, 129);
            btnEmployees.TabIndex = 8;
            btnEmployees.Text = "Manage Employees";
            btnEmployees.UseVisualStyleBackColor = true;
            btnEmployees.Click += btnEmployees_Click;
            // 
            // lblStockAlert
            // 
            lblStockAlert.AutoSize = true;
            lblStockAlert.BackColor = Color.Red;
            lblStockAlert.Location = new Point(1111, 107);
            lblStockAlert.Name = "lblStockAlert";
            lblStockAlert.Size = new Size(0, 21);
            lblStockAlert.TabIndex = 9;
            // 
            // lblTheatres
            // 
            lblTheatres.AutoSize = true;
            lblTheatres.Font = new Font("Segoe UI", 21.8879986F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTheatres.ForeColor = Color.White;
            lblTheatres.Location = new Point(328, 107);
            lblTheatres.Name = "lblTheatres";
            lblTheatres.Size = new Size(176, 51);
            lblTheatres.TabIndex = 10;
            lblTheatres.Text = "Theatres";
            // 
            // lblStaff
            // 
            lblStaff.AutoSize = true;
            lblStaff.Font = new Font("Segoe UI", 21.8879986F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblStaff.ForeColor = Color.White;
            lblStaff.Location = new Point(862, 107);
            lblStaff.Name = "lblStaff";
            lblStaff.Size = new Size(107, 51);
            lblStaff.TabIndex = 11;
            lblStaff.Text = "Staff";
            // 
            // btnDashboard
            // 
            btnDashboard.Font = new Font("Microsoft Sans Serif", 17.855999F, FontStyle.Bold);
            btnDashboard.Location = new Point(12, 633);
            btnDashboard.Name = "btnDashboard";
            btnDashboard.Size = new Size(190, 87);
            btnDashboard.TabIndex = 13;
            btnDashboard.Text = "Operations Dashboard";
            btnDashboard.UseVisualStyleBackColor = true;
            btnDashboard.Click += btnDashboard_Click;
            // 
            // btnLogout
            // 
            btnLogout.Font = new Font("Microsoft Sans Serif", 17.855999F, FontStyle.Bold);
            btnLogout.Location = new Point(1092, 9);
            btnLogout.Name = "btnLogout";
            btnLogout.Size = new Size(190, 87);
            btnLogout.TabIndex = 14;
            btnLogout.Text = "Log Out";
            btnLogout.UseVisualStyleBackColor = true;
            btnLogout.Click += btnLogout_Click;
            // 
            // btnSchedules
            // 
            btnSchedules.Font = new Font("Microsoft Sans Serif", 17.855999F, FontStyle.Bold);
            btnSchedules.Location = new Point(802, 428);
            btnSchedules.Name = "btnSchedules";
            btnSchedules.Size = new Size(183, 126);
            btnSchedules.TabIndex = 15;
            btnSchedules.Text = "Manage Schedules";
            btnSchedules.UseVisualStyleBackColor = true;
            btnSchedules.Click += btnSchedules_Click;
            // 
            // btnPayroll
            // 
            btnPayroll.Font = new Font("Microsoft Sans Serif", 17.855999F, FontStyle.Bold);
            btnPayroll.Location = new Point(652, 608);
            btnPayroll.Name = "btnPayroll";
            btnPayroll.Size = new Size(264, 87);
            btnPayroll.TabIndex = 16;
            btnPayroll.Text = "Payroll";
            btnPayroll.UseVisualStyleBackColor = true;
            btnPayroll.Click += btnPayroll_Click;
            // 
            // btnReports
            // 
            btnReports.Font = new Font("Microsoft Sans Serif", 17.855999F, FontStyle.Bold);
            btnReports.Location = new Point(368, 608);
            btnReports.Name = "btnReports";
            btnReports.Size = new Size(264, 87);
            btnReports.TabIndex = 17;
            btnReports.Text = "Reports";
            btnReports.UseVisualStyleBackColor = true;
            btnReports.Click += btnReports_Click;
            // 
            // btnPOS
            // 
            btnPOS.Enabled = false;
            btnPOS.Font = new Font("Microsoft Sans Serif", 17.855999F, FontStyle.Bold);
            btnPOS.Location = new Point(61, 448);
            btnPOS.Name = "btnPOS";
            btnPOS.Size = new Size(264, 87);
            btnPOS.TabIndex = 18;
            btnPOS.Text = "Manage POS";
            btnPOS.UseVisualStyleBackColor = true;
            btnPOS.Visible = false;
            btnPOS.Click += btnPOS_Click;
            // 
            // btnBackupRestore
            // 
            btnBackupRestore.Font = new Font("Microsoft Sans Serif", 9.792F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnBackupRestore.Location = new Point(1194, 645);
            btnBackupRestore.Name = "btnBackupRestore";
            btnBackupRestore.Size = new Size(88, 74);
            btnBackupRestore.TabIndex = 19;
            btnBackupRestore.Text = "Backup/Restore";
            btnBackupRestore.UseVisualStyleBackColor = true;
            btnBackupRestore.Click += btnBackupRestore_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(9F, 21F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Teal;
            ClientSize = new Size(1294, 732);
            Controls.Add(btnBackupRestore);
            Controls.Add(btnPOS);
            Controls.Add(btnReports);
            Controls.Add(btnPayroll);
            Controls.Add(btnSchedules);
            Controls.Add(btnLogout);
            Controls.Add(btnDashboard);
            Controls.Add(lblStaff);
            Controls.Add(lblTheatres);
            Controls.Add(lblStockAlert);
            Controls.Add(btnEmployees);
            Controls.Add(lblTitle);
            Controls.Add(btnConcessions);
            Controls.Add(btnUsers);
            Controls.Add(PicLogo);
            Controls.Add(btnLocation);
            Controls.Add(btnTheaterScreen);
            Controls.Add(btnShowtimes);
            Controls.Add(btnMovies);
            Name = "MainForm";
            Text = "ScrumFlix";
            FormClosing += MainForm_FormClosing;
            FormClosed += MainForm_FormClosed;
            Load += MainForm_Load;
            ((System.ComponentModel.ISupportInitialize)PicLogo).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnMovies;
        private Button btnShowtimes;
        private Button btnTheaterScreen;
        private Button btnLocation;
        private PictureBox PicLogo;
        private Button btnUsers;
        private Button btnConcessions;
        private Label lblTitle;
        private Button btnEmployees;
        private Label lblStockAlert;
        private Label lblTheatres;
        private Label lblStaff;
        private Button btnDashboard;
        private Button btnLogout;
        private Button btnSchedules;
        private Button btnPayroll;
        private Button btnReports;
        private Button btnPOS;
        private Button btnBackupRestore;
    }
}
