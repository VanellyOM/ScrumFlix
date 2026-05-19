namespace ScrumFlix.Forms
{
    partial class ScheduleManagementForm
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
            lblShifts = new Label();
            dateTimeStartShift = new DateTimePicker();
            dateTimeEndShift = new DateTimePicker();
            btnAdd = new Button();
            btnUpdate = new Button();
            btnDelete = new Button();
            btnRefresh = new Button();
            gridShifts = new DataGridView();
            lblAssignments = new Label();
            gridScheduleAssignments = new DataGridView();
            comboEmployee = new ComboBox();
            btnAdd2 = new Button();
            btnUpdate2 = new Button();
            btnDelete2 = new Button();
            btnRefresh2 = new Button();
            comboShowtime = new ComboBox();
            panelSchedule = new Panel();
            comboMonth = new ComboBox();
            btnLoadSchedule = new Button();
            comboRole = new ComboBox();
            comboLocation = new ComboBox();
            txtAssignmentName = new TextBox();
            comboShiftLocation = new ComboBox();
            label1 = new Label();
            ((System.ComponentModel.ISupportInitialize)gridShifts).BeginInit();
            ((System.ComponentModel.ISupportInitialize)gridScheduleAssignments).BeginInit();
            SuspendLayout();
            // 
            // lblShifts
            // 
            lblShifts.AutoSize = true;
            lblShifts.Location = new Point(12, 19);
            lblShifts.Name = "lblShifts";
            lblShifts.Size = new Size(109, 21);
            lblShifts.TabIndex = 0;
            lblShifts.Text = "Manage Shifts";
            // 
            // dateTimeStartShift
            // 
            dateTimeStartShift.Location = new Point(12, 439);
            dateTimeStartShift.Name = "dateTimeStartShift";
            dateTimeStartShift.Size = new Size(260, 29);
            dateTimeStartShift.TabIndex = 1;
            // 
            // dateTimeEndShift
            // 
            dateTimeEndShift.Location = new Point(278, 439);
            dateTimeEndShift.Name = "dateTimeEndShift";
            dateTimeEndShift.Size = new Size(260, 29);
            dateTimeEndShift.TabIndex = 2;
            // 
            // btnAdd
            // 
            btnAdd.Location = new Point(12, 474);
            btnAdd.Name = "btnAdd";
            btnAdd.Size = new Size(98, 30);
            btnAdd.TabIndex = 3;
            btnAdd.Text = "Add";
            btnAdd.UseVisualStyleBackColor = true;
            btnAdd.Click += btnAdd_Click;
            // 
            // btnUpdate
            // 
            btnUpdate.Location = new Point(116, 474);
            btnUpdate.Name = "btnUpdate";
            btnUpdate.Size = new Size(98, 30);
            btnUpdate.TabIndex = 4;
            btnUpdate.Text = "Update";
            btnUpdate.UseVisualStyleBackColor = true;
            btnUpdate.Click += btnUpdate_Click;
            // 
            // btnDelete
            // 
            btnDelete.Location = new Point(220, 474);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(98, 30);
            btnDelete.TabIndex = 5;
            btnDelete.Text = "Delete";
            btnDelete.UseVisualStyleBackColor = true;
            btnDelete.Click += btnDelete_Click;
            // 
            // btnRefresh
            // 
            btnRefresh.Location = new Point(324, 474);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new Size(98, 30);
            btnRefresh.TabIndex = 6;
            btnRefresh.Text = "Refresh";
            btnRefresh.UseVisualStyleBackColor = true;
            btnRefresh.Click += btnRefresh_Click;
            // 
            // gridShifts
            // 
            gridShifts.AllowUserToAddRows = false;
            gridShifts.AllowUserToDeleteRows = false;
            gridShifts.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gridShifts.Location = new Point(12, 43);
            gridShifts.MultiSelect = false;
            gridShifts.Name = "gridShifts";
            gridShifts.ReadOnly = true;
            gridShifts.RowHeadersWidth = 53;
            gridShifts.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridShifts.Size = new Size(986, 383);
            gridShifts.TabIndex = 8;
            gridShifts.CellClick += gridShifts_CellClick;
            // 
            // lblAssignments
            // 
            lblAssignments.AutoSize = true;
            lblAssignments.Location = new Point(12, 560);
            lblAssignments.Name = "lblAssignments";
            lblAssignments.Size = new Size(631, 21);
            lblAssignments.TabIndex = 9;
            lblAssignments.Text = "Schedule Assignments to Employees (Select the shift above then put in assignment details";
            // 
            // gridScheduleAssignments
            // 
            gridScheduleAssignments.AllowUserToAddRows = false;
            gridScheduleAssignments.AllowUserToDeleteRows = false;
            gridScheduleAssignments.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gridScheduleAssignments.Location = new Point(12, 660);
            gridScheduleAssignments.MultiSelect = false;
            gridScheduleAssignments.Name = "gridScheduleAssignments";
            gridScheduleAssignments.ReadOnly = true;
            gridScheduleAssignments.RowHeadersWidth = 53;
            gridScheduleAssignments.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridScheduleAssignments.Size = new Size(1307, 361);
            gridScheduleAssignments.TabIndex = 10;
            // 
            // comboEmployee
            // 
            comboEmployee.FormattingEnabled = true;
            comboEmployee.Location = new Point(428, 621);
            comboEmployee.Name = "comboEmployee";
            comboEmployee.Size = new Size(158, 29);
            comboEmployee.TabIndex = 12;
            // 
            // btnAdd2
            // 
            btnAdd2.Location = new Point(12, 619);
            btnAdd2.Name = "btnAdd2";
            btnAdd2.Size = new Size(98, 30);
            btnAdd2.TabIndex = 13;
            btnAdd2.Text = "Add";
            btnAdd2.UseVisualStyleBackColor = true;
            btnAdd2.Click += btnAdd2_Click;
            // 
            // btnUpdate2
            // 
            btnUpdate2.Location = new Point(116, 619);
            btnUpdate2.Name = "btnUpdate2";
            btnUpdate2.Size = new Size(98, 30);
            btnUpdate2.TabIndex = 14;
            btnUpdate2.Text = "Update";
            btnUpdate2.UseVisualStyleBackColor = true;
            btnUpdate2.Click += btnUpdate2_Click;
            // 
            // btnDelete2
            // 
            btnDelete2.Location = new Point(220, 619);
            btnDelete2.Name = "btnDelete2";
            btnDelete2.Size = new Size(98, 30);
            btnDelete2.TabIndex = 15;
            btnDelete2.Text = "Delete";
            btnDelete2.UseVisualStyleBackColor = true;
            btnDelete2.Click += btnDelete2_Click;
            // 
            // btnRefresh2
            // 
            btnRefresh2.Location = new Point(324, 619);
            btnRefresh2.Name = "btnRefresh2";
            btnRefresh2.Size = new Size(98, 30);
            btnRefresh2.TabIndex = 16;
            btnRefresh2.Text = "Refresh";
            btnRefresh2.UseVisualStyleBackColor = true;
            btnRefresh2.Click += btnRefresh2_Click;
            // 
            // comboShowtime
            // 
            comboShowtime.FormattingEnabled = true;
            comboShowtime.Location = new Point(592, 621);
            comboShowtime.Name = "comboShowtime";
            comboShowtime.Size = new Size(158, 29);
            comboShowtime.TabIndex = 18;
            // 
            // panelSchedule
            // 
            panelSchedule.AutoScroll = true;
            panelSchedule.Location = new Point(1018, 32);
            panelSchedule.Name = "panelSchedule";
            panelSchedule.Size = new Size(1288, 460);
            panelSchedule.TabIndex = 19;
            // 
            // comboMonth
            // 
            comboMonth.FormattingEnabled = true;
            comboMonth.Location = new Point(1484, 498);
            comboMonth.Name = "comboMonth";
            comboMonth.Size = new Size(158, 29);
            comboMonth.TabIndex = 20;
            // 
            // btnLoadSchedule
            // 
            btnLoadSchedule.Location = new Point(1654, 498);
            btnLoadSchedule.Name = "btnLoadSchedule";
            btnLoadSchedule.Size = new Size(98, 30);
            btnLoadSchedule.TabIndex = 21;
            btnLoadSchedule.Text = "Load";
            btnLoadSchedule.UseVisualStyleBackColor = true;
            btnLoadSchedule.Click += btnLoadSchedule_Click;
            // 
            // comboRole
            // 
            comboRole.FormattingEnabled = true;
            comboRole.Location = new Point(428, 476);
            comboRole.Name = "comboRole";
            comboRole.Size = new Size(110, 29);
            comboRole.TabIndex = 22;
            // 
            // comboLocation
            // 
            comboLocation.FormattingEnabled = true;
            comboLocation.Location = new Point(627, 442);
            comboLocation.Name = "comboLocation";
            comboLocation.Size = new Size(158, 29);
            comboLocation.TabIndex = 23;
            comboLocation.SelectedIndexChanged += comboLocation_SelectedIndexChanged;
            // 
            // txtAssignmentName
            // 
            txtAssignmentName.Location = new Point(12, 584);
            txtAssignmentName.Name = "txtAssignmentName";
            txtAssignmentName.PlaceholderText = "Assignment Details";
            txtAssignmentName.Size = new Size(738, 29);
            txtAssignmentName.TabIndex = 24;
            // 
            // comboShiftLocation
            // 
            comboShiftLocation.FormattingEnabled = true;
            comboShiftLocation.Location = new Point(1320, 498);
            comboShiftLocation.Name = "comboShiftLocation";
            comboShiftLocation.Size = new Size(158, 29);
            comboShiftLocation.TabIndex = 25;
            comboShiftLocation.SelectedIndexChanged += comboShiftLocation_SelectedIndexChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(791, 442);
            label1.Name = "label1";
            label1.Size = new Size(191, 42);
            label1.TabIndex = 26;
            label1.Text = "<- Select the location you \r\n     are trying to manage";
            // 
            // ScheduleManagementForm
            // 
            AutoScaleDimensions = new SizeF(9F, 21F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(2351, 1036);
            Controls.Add(label1);
            Controls.Add(comboShiftLocation);
            Controls.Add(txtAssignmentName);
            Controls.Add(comboLocation);
            Controls.Add(comboRole);
            Controls.Add(btnLoadSchedule);
            Controls.Add(comboMonth);
            Controls.Add(panelSchedule);
            Controls.Add(comboShowtime);
            Controls.Add(btnRefresh2);
            Controls.Add(btnDelete2);
            Controls.Add(btnUpdate2);
            Controls.Add(btnAdd2);
            Controls.Add(comboEmployee);
            Controls.Add(gridScheduleAssignments);
            Controls.Add(lblAssignments);
            Controls.Add(gridShifts);
            Controls.Add(btnRefresh);
            Controls.Add(btnDelete);
            Controls.Add(btnUpdate);
            Controls.Add(btnAdd);
            Controls.Add(dateTimeEndShift);
            Controls.Add(dateTimeStartShift);
            Controls.Add(lblShifts);
            Name = "ScheduleManagementForm";
            Text = "ScheduleManagementForm";
            Load += ScheduleManagementForm_Load;
            ((System.ComponentModel.ISupportInitialize)gridShifts).EndInit();
            ((System.ComponentModel.ISupportInitialize)gridScheduleAssignments).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblShifts;
        private DateTimePicker dateTimeStartShift;
        private DateTimePicker dateTimeEndShift;
        private Button btnAdd;
        private Button btnUpdate;
        private Button btnDelete;
        private Button btnRefresh;
        private DataGridView gridShifts;
        private Label lblAssignments;
        private DataGridView gridScheduleAssignments;
        private ComboBox comboEmployee;
        private Button btnAdd2;
        private Button btnUpdate2;
        private Button btnDelete2;
        private Button btnRefresh2;
        private ComboBox comboShowtime;
        private Panel panelSchedule;
        private ComboBox comboMonth;
        private Button btnLoadSchedule;
        private ComboBox comboRole;
        private ComboBox comboLocation;
        private TextBox txtAssignmentName;
        private ComboBox comboShiftLocation;
        private Label label1;
    }
}