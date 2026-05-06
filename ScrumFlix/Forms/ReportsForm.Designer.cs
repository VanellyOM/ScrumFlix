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
            SuspendLayout();
            // 
            // btnTicketSalesReport
            // 
            btnTicketSalesReport.Location = new Point(278, 232);
            btnTicketSalesReport.Name = "btnTicketSalesReport";
            btnTicketSalesReport.Size = new Size(98, 30);
            btnTicketSalesReport.TabIndex = 0;
            btnTicketSalesReport.Text = "Tickets";
            btnTicketSalesReport.UseVisualStyleBackColor = true;
            btnTicketSalesReport.Click += btnTicketSalesReport_Click;
            // 
            // btnConcessionSalesReport
            // 
            btnConcessionSalesReport.Location = new Point(417, 232);
            btnConcessionSalesReport.Name = "btnConcessionSalesReport";
            btnConcessionSalesReport.Size = new Size(98, 30);
            btnConcessionSalesReport.TabIndex = 1;
            btnConcessionSalesReport.Text = "Concessions";
            btnConcessionSalesReport.UseVisualStyleBackColor = true;
            btnConcessionSalesReport.Click += btnConcessionSalesReport_Click;
            // 
            // btnPayrollReport
            // 
            btnPayrollReport.Location = new Point(560, 232);
            btnPayrollReport.Name = "btnPayrollReport";
            btnPayrollReport.Size = new Size(98, 30);
            btnPayrollReport.TabIndex = 2;
            btnPayrollReport.Text = "Payroll";
            btnPayrollReport.UseVisualStyleBackColor = true;
            btnPayrollReport.Click += btnPayrollReport_Click;
            // 
            // dateStart
            // 
            dateStart.Location = new Point(255, 366);
            dateStart.Name = "dateStart";
            dateStart.Size = new Size(260, 29);
            dateStart.TabIndex = 3;
            // 
            // dateEnd
            // 
            dateEnd.Location = new Point(547, 366);
            dateEnd.Name = "dateEnd";
            dateEnd.Size = new Size(260, 29);
            dateEnd.TabIndex = 4;
            // 
            // ReportsForm
            // 
            AutoScaleDimensions = new SizeF(9F, 21F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1151, 635);
            Controls.Add(dateEnd);
            Controls.Add(dateStart);
            Controls.Add(btnPayrollReport);
            Controls.Add(btnConcessionSalesReport);
            Controls.Add(btnTicketSalesReport);
            Name = "ReportsForm";
            Text = "ReportsForm";
            ResumeLayout(false);
        }

        #endregion

        private Button btnTicketSalesReport;
        private Button btnConcessionSalesReport;
        private Button btnPayrollReport;
        private DateTimePicker dateStart;
        private DateTimePicker dateEnd;
    }
}