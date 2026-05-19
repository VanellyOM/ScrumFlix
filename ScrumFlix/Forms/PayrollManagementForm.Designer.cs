namespace ScrumFlix.Forms
{
    partial class PayrollManagementForm
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
            btnPayPeriod = new Button();
            comboPayPeriod = new ComboBox();
            btnGenerateTimesheets = new Button();
            gridTimesheets = new DataGridView();
            btnApproveTimesheet = new Button();
            btnUnapproveTimesheet = new Button();
            gridPayrolls = new DataGridView();
            btnGeneratePayroll = new Button();
            lblSelect = new Label();
            gridPayStubs = new DataGridView();
            btnGeneratePayStubs = new Button();
            btnViewPayStub = new Button();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
            label5 = new Label();
            ((System.ComponentModel.ISupportInitialize)gridTimesheets).BeginInit();
            ((System.ComponentModel.ISupportInitialize)gridPayrolls).BeginInit();
            ((System.ComponentModel.ISupportInitialize)gridPayStubs).BeginInit();
            SuspendLayout();
            // 
            // btnPayPeriod
            // 
            btnPayPeriod.Location = new Point(12, 12);
            btnPayPeriod.Name = "btnPayPeriod";
            btnPayPeriod.Size = new Size(161, 30);
            btnPayPeriod.TabIndex = 0;
            btnPayPeriod.Text = "Manage Payperiods";
            btnPayPeriod.UseVisualStyleBackColor = true;
            btnPayPeriod.Click += btnPayPeriod_Click;
            // 
            // comboPayPeriod
            // 
            comboPayPeriod.FormattingEnabled = true;
            comboPayPeriod.Location = new Point(15, 87);
            comboPayPeriod.Name = "comboPayPeriod";
            comboPayPeriod.Size = new Size(158, 29);
            comboPayPeriod.TabIndex = 1;
            comboPayPeriod.SelectedIndexChanged += comboPayPeriod_SelectedIndexChanged;
            // 
            // btnGenerateTimesheets
            // 
            btnGenerateTimesheets.Location = new Point(179, 85);
            btnGenerateTimesheets.Name = "btnGenerateTimesheets";
            btnGenerateTimesheets.Size = new Size(177, 30);
            btnGenerateTimesheets.TabIndex = 2;
            btnGenerateTimesheets.Text = "Generate Timesheets";
            btnGenerateTimesheets.UseVisualStyleBackColor = true;
            btnGenerateTimesheets.Click += btnGenerateTimesheets_Click;
            // 
            // gridTimesheets
            // 
            gridTimesheets.AllowUserToAddRows = false;
            gridTimesheets.AllowUserToDeleteRows = false;
            gridTimesheets.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gridTimesheets.Location = new Point(12, 122);
            gridTimesheets.Name = "gridTimesheets";
            gridTimesheets.ReadOnly = true;
            gridTimesheets.RowHeadersWidth = 53;
            gridTimesheets.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridTimesheets.Size = new Size(776, 195);
            gridTimesheets.TabIndex = 3;
            // 
            // btnApproveTimesheet
            // 
            btnApproveTimesheet.Location = new Point(12, 323);
            btnApproveTimesheet.Name = "btnApproveTimesheet";
            btnApproveTimesheet.Size = new Size(182, 30);
            btnApproveTimesheet.TabIndex = 4;
            btnApproveTimesheet.Text = "Approve timesheet";
            btnApproveTimesheet.UseVisualStyleBackColor = true;
            btnApproveTimesheet.Click += btnApproveTimesheet_Click;
            // 
            // btnUnapproveTimesheet
            // 
            btnUnapproveTimesheet.Location = new Point(200, 323);
            btnUnapproveTimesheet.Name = "btnUnapproveTimesheet";
            btnUnapproveTimesheet.Size = new Size(182, 30);
            btnUnapproveTimesheet.TabIndex = 5;
            btnUnapproveTimesheet.Text = "Unapprove timesheet";
            btnUnapproveTimesheet.UseVisualStyleBackColor = true;
            btnUnapproveTimesheet.Click += btnUnapproveTimesheet_Click;
            // 
            // gridPayrolls
            // 
            gridPayrolls.AllowUserToAddRows = false;
            gridPayrolls.AllowUserToDeleteRows = false;
            gridPayrolls.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gridPayrolls.Location = new Point(15, 359);
            gridPayrolls.MultiSelect = false;
            gridPayrolls.Name = "gridPayrolls";
            gridPayrolls.ReadOnly = true;
            gridPayrolls.RowHeadersWidth = 53;
            gridPayrolls.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridPayrolls.Size = new Size(773, 195);
            gridPayrolls.TabIndex = 6;
            // 
            // btnGeneratePayroll
            // 
            btnGeneratePayroll.Location = new Point(15, 560);
            btnGeneratePayroll.Name = "btnGeneratePayroll";
            btnGeneratePayroll.Size = new Size(179, 30);
            btnGeneratePayroll.TabIndex = 7;
            btnGeneratePayroll.Text = "Generate Payroll";
            btnGeneratePayroll.UseVisualStyleBackColor = true;
            btnGeneratePayroll.Click += btnGeneratePayroll_Click;
            // 
            // lblSelect
            // 
            lblSelect.AutoSize = true;
            lblSelect.Location = new Point(15, 63);
            lblSelect.Name = "lblSelect";
            lblSelect.Size = new Size(295, 21);
            lblSelect.TabIndex = 8;
            lblSelect.Text = "Select a Pay Period for whole payroll flow";
            // 
            // gridPayStubs
            // 
            gridPayStubs.AllowUserToAddRows = false;
            gridPayStubs.AllowUserToDeleteRows = false;
            gridPayStubs.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gridPayStubs.Location = new Point(15, 596);
            gridPayStubs.MultiSelect = false;
            gridPayStubs.Name = "gridPayStubs";
            gridPayStubs.ReadOnly = true;
            gridPayStubs.RowHeadersWidth = 53;
            gridPayStubs.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridPayStubs.Size = new Size(773, 195);
            gridPayStubs.TabIndex = 9;
            // 
            // btnGeneratePayStubs
            // 
            btnGeneratePayStubs.Location = new Point(15, 797);
            btnGeneratePayStubs.Name = "btnGeneratePayStubs";
            btnGeneratePayStubs.Size = new Size(158, 30);
            btnGeneratePayStubs.TabIndex = 10;
            btnGeneratePayStubs.Text = "Generate Pay Stubs";
            btnGeneratePayStubs.UseVisualStyleBackColor = true;
            btnGeneratePayStubs.Click += btnGeneratePayStubs_Click;
            // 
            // btnViewPayStub
            // 
            btnViewPayStub.Location = new Point(289, 829);
            btnViewPayStub.Name = "btnViewPayStub";
            btnViewPayStub.Size = new Size(188, 30);
            btnViewPayStub.TabIndex = 11;
            btnViewPayStub.Text = "View Selected Pay Stub";
            btnViewPayStub.UseVisualStyleBackColor = true;
            btnViewPayStub.Click += btnViewPayStub_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(794, 188);
            label1.Name = "label1";
            label1.Size = new Size(237, 84);
            label1.TabIndex = 12;
            label1.Text = "<- Approve timesheets that are \r\n      generated using employees \r\n      time entries after you press \r\n     \"Generate Timesheets\" button";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(802, 420);
            label2.Name = "label2";
            label2.Size = new Size(229, 63);
            label2.TabIndex = 13;
            label2.Text = "<- Generate payroll based on \r\n      approved timesheets for the\r\n      selected pay period\r\n";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(802, 670);
            label3.Name = "label3";
            label3.Size = new Size(230, 63);
            label3.TabIndex = 14;
            label3.Text = "<- Issue pay stubs for each \r\n     employee using the selected \r\n     pay periods payroll";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(179, 12);
            label4.Name = "label4";
            label4.Size = new Size(389, 21);
            label4.TabIndex = 15;
            label4.Text = "<- Manage the pay periods that employees get paid by";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(483, 829);
            label5.Name = "label5";
            label5.Size = new Size(360, 21);
            label5.TabIndex = 16;
            label5.Text = "<- View the selected pay stub with full information\r\n";
            // 
            // PayrollManagementForm
            // 
            AutoScaleDimensions = new SizeF(9F, 21F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1058, 871);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(btnViewPayStub);
            Controls.Add(btnGeneratePayStubs);
            Controls.Add(gridPayStubs);
            Controls.Add(lblSelect);
            Controls.Add(btnGeneratePayroll);
            Controls.Add(gridPayrolls);
            Controls.Add(btnUnapproveTimesheet);
            Controls.Add(btnApproveTimesheet);
            Controls.Add(gridTimesheets);
            Controls.Add(btnGenerateTimesheets);
            Controls.Add(comboPayPeriod);
            Controls.Add(btnPayPeriod);
            Name = "PayrollManagementForm";
            Text = "PayrollManagementForm";
            Load += PayrollManagementForm_Load;
            ((System.ComponentModel.ISupportInitialize)gridTimesheets).EndInit();
            ((System.ComponentModel.ISupportInitialize)gridPayrolls).EndInit();
            ((System.ComponentModel.ISupportInitialize)gridPayStubs).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnPayPeriod;
        private ComboBox comboPayPeriod;
        private Button btnGenerateTimesheets;
        private DataGridView gridTimesheets;
        private Button btnApproveTimesheet;
        private Button btnUnapproveTimesheet;
        private DataGridView gridPayrolls;
        private Button btnGeneratePayroll;
        private Label lblSelect;
        private DataGridView gridPayStubs;
        private Button btnGeneratePayStubs;
        private Button btnViewPayStub;
        private Label label1;
        private Label label2;
        private Label label3;
        private Label label4;
        private Label label5;
    }
}