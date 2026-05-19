namespace ScrumFlix.Forms
{
    partial class ReportsForm
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
            btnTicketSalesReport = new Button();
            btnConcessionSalesReport = new Button();
            btnPayrollReport = new Button();
            dateStart = new DateTimePicker();
            dateEnd = new DateTimePicker();
            label1 = new Label();
            comboLocation = new ComboBox();
            SuspendLayout();
            // 
            // btnTicketSalesReport
            // 
            btnTicketSalesReport.Location = new Point(12, 12);
            btnTicketSalesReport.Name = "btnTicketSalesReport";
            btnTicketSalesReport.Size = new Size(236, 57);
            btnTicketSalesReport.TabIndex = 0;
            btnTicketSalesReport.Text = "Export Ticket Sales";
            btnTicketSalesReport.UseVisualStyleBackColor = true;
            btnTicketSalesReport.Click += btnTicketSalesReport_Click;
            // 
            // btnConcessionSalesReport
            // 
            btnConcessionSalesReport.Location = new Point(255, 12);
            btnConcessionSalesReport.Name = "btnConcessionSalesReport";
            btnConcessionSalesReport.Size = new Size(236, 57);
            btnConcessionSalesReport.TabIndex = 1;
            btnConcessionSalesReport.Text = "Export Concessions Sales";
            btnConcessionSalesReport.UseVisualStyleBackColor = true;
            btnConcessionSalesReport.Click += btnConcessionSalesReport_Click;
            // 
            // btnPayrollReport
            // 
            btnPayrollReport.Location = new Point(497, 12);
            btnPayrollReport.Name = "btnPayrollReport";
            btnPayrollReport.Size = new Size(236, 57);
            btnPayrollReport.TabIndex = 2;
            btnPayrollReport.Text = "Export Payroll Report";
            btnPayrollReport.UseVisualStyleBackColor = true;
            btnPayrollReport.Click += btnPayrollReport_Click;
            // 
            // dateStart
            // 
            dateStart.Location = new Point(12, 137);
            dateStart.Name = "dateStart";
            dateStart.Size = new Size(260, 29);
            dateStart.TabIndex = 3;
            // 
            // dateEnd
            // 
            dateEnd.Location = new Point(317, 137);
            dateEnd.Name = "dateEnd";
            dateEnd.Size = new Size(260, 29);
            dateEnd.TabIndex = 4;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 97);
            label1.Name = "label1";
            label1.Size = new Size(803, 21);
            label1.TabIndex = 5;
            label1.Text = "Select a date range for reports then click which report you'd like to export as CSV (You can open this in excel easily)";
            // 
            // comboLocation
            // 
            comboLocation.FormattingEnabled = true;
            comboLocation.Location = new Point(603, 137);
            comboLocation.Name = "comboLocation";
            comboLocation.Size = new Size(212, 29);
            comboLocation.TabIndex = 6;
            // 
            // ReportsForm
            // 
            AutoScaleDimensions = new SizeF(9F, 21F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(850, 207);
            Controls.Add(comboLocation);
            Controls.Add(label1);
            Controls.Add(dateEnd);
            Controls.Add(dateStart);
            Controls.Add(btnPayrollReport);
            Controls.Add(btnConcessionSalesReport);
            Controls.Add(btnTicketSalesReport);
            Name = "ReportsForm";
            Text = "ReportsForm";
            Load += ReportsForm_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnTicketSalesReport;
        private Button btnConcessionSalesReport;
        private Button btnPayrollReport;
        private DateTimePicker dateStart;
        private DateTimePicker dateEnd;
        private Label label1;
        private ComboBox comboLocation;
    }
}