namespace ScrumFlix.Forms
{
    partial class EmployeeEditForm
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
            txtFirst = new TextBox();
            txtMiddle = new TextBox();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
            label5 = new Label();
            label6 = new Label();
            label7 = new Label();
            txtLast = new TextBox();
            txtPhone = new TextBox();
            sqliteCommand1 = new Microsoft.Data.Sqlite.SqliteCommand();
            dateDOB = new DateTimePicker();
            txtEmail = new TextBox();
            txtAddress = new TextBox();
            btnSubmit = new Button();
            btnCancel = new Button();
            txtPayRate = new TextBox();
            label8 = new Label();
            comboLocation = new ComboBox();
            label9 = new Label();
            SuspendLayout();
            // 
            // txtFirst
            // 
            txtFirst.Location = new Point(12, 33);
            txtFirst.Name = "txtFirst";
            txtFirst.Size = new Size(213, 29);
            txtFirst.TabIndex = 0;
            // 
            // txtMiddle
            // 
            txtMiddle.Location = new Point(231, 33);
            txtMiddle.Name = "txtMiddle";
            txtMiddle.Size = new Size(130, 29);
            txtMiddle.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 9);
            label1.Name = "label1";
            label1.Size = new Size(86, 21);
            label1.TabIndex = 2;
            label1.Text = "First Name";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(231, 9);
            label2.Name = "label2";
            label2.Size = new Size(104, 21);
            label2.TabIndex = 3;
            label2.Text = "Middle Name";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(367, 9);
            label3.Name = "label3";
            label3.Size = new Size(81, 21);
            label3.TabIndex = 4;
            label3.Text = "Last name";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(12, 65);
            label4.Name = "label4";
            label4.Size = new Size(42, 21);
            label4.TabIndex = 5;
            label4.Text = "DOB";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(297, 65);
            label5.Name = "label5";
            label5.Size = new Size(113, 21);
            label5.TabIndex = 6;
            label5.Text = "Phone number";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(12, 121);
            label6.Name = "label6";
            label6.Size = new Size(48, 21);
            label6.TabIndex = 7;
            label6.Text = "Email";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(295, 121);
            label7.Name = "label7";
            label7.Size = new Size(66, 21);
            label7.TabIndex = 8;
            label7.Text = "Address";
            // 
            // txtLast
            // 
            txtLast.Location = new Point(367, 33);
            txtLast.Name = "txtLast";
            txtLast.Size = new Size(239, 29);
            txtLast.TabIndex = 9;
            // 
            // txtPhone
            // 
            txtPhone.Location = new Point(295, 89);
            txtPhone.Name = "txtPhone";
            txtPhone.Size = new Size(311, 29);
            txtPhone.TabIndex = 10;
            // 
            // sqliteCommand1
            // 
            sqliteCommand1.CommandTimeout = 30;
            sqliteCommand1.Connection = null;
            sqliteCommand1.Transaction = null;
            sqliteCommand1.UpdatedRowSource = System.Data.UpdateRowSource.None;
            // 
            // dateDOB
            // 
            dateDOB.Location = new Point(12, 89);
            dateDOB.Name = "dateDOB";
            dateDOB.Size = new Size(260, 29);
            dateDOB.TabIndex = 11;
            // 
            // txtEmail
            // 
            txtEmail.Location = new Point(12, 145);
            txtEmail.Name = "txtEmail";
            txtEmail.Size = new Size(277, 29);
            txtEmail.TabIndex = 12;
            // 
            // txtAddress
            // 
            txtAddress.Location = new Point(295, 145);
            txtAddress.Name = "txtAddress";
            txtAddress.Size = new Size(311, 29);
            txtAddress.TabIndex = 13;
            // 
            // btnSubmit
            // 
            btnSubmit.DialogResult = DialogResult.OK;
            btnSubmit.Location = new Point(208, 241);
            btnSubmit.Name = "btnSubmit";
            btnSubmit.Size = new Size(98, 30);
            btnSubmit.TabIndex = 14;
            btnSubmit.Text = "Submit";
            btnSubmit.UseVisualStyleBackColor = true;
            btnSubmit.Click += btnSubmit_Click;
            // 
            // btnCancel
            // 
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(312, 241);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(98, 30);
            btnCancel.TabIndex = 15;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // txtPayRate
            // 
            txtPayRate.Location = new Point(12, 201);
            txtPayRate.Name = "txtPayRate";
            txtPayRate.Size = new Size(277, 29);
            txtPayRate.TabIndex = 16;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(12, 177);
            label8.Name = "label8";
            label8.Size = new Size(69, 21);
            label8.TabIndex = 17;
            label8.Text = "Pay Rate";
            // 
            // comboLocation
            // 
            comboLocation.FormattingEnabled = true;
            comboLocation.Location = new Point(295, 201);
            comboLocation.Name = "comboLocation";
            comboLocation.Size = new Size(311, 29);
            comboLocation.TabIndex = 18;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(292, 177);
            label9.Name = "label9";
            label9.Size = new Size(69, 21);
            label9.TabIndex = 19;
            label9.Text = "Location";
            // 
            // EmployeeEditForm
            // 
            AutoScaleDimensions = new SizeF(9F, 21F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(618, 287);
            Controls.Add(label9);
            Controls.Add(comboLocation);
            Controls.Add(label8);
            Controls.Add(txtPayRate);
            Controls.Add(btnCancel);
            Controls.Add(btnSubmit);
            Controls.Add(txtAddress);
            Controls.Add(txtEmail);
            Controls.Add(dateDOB);
            Controls.Add(txtPhone);
            Controls.Add(txtLast);
            Controls.Add(label7);
            Controls.Add(label6);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(txtMiddle);
            Controls.Add(txtFirst);
            Name = "EmployeeEditForm";
            Text = "EmployeeEditForm";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox txtFirst;
        private TextBox txtMiddle;
        private Label label1;
        private Label label2;
        private Label label3;
        private Label label4;
        private Label label5;
        private Label label6;
        private Label label7;
        private TextBox txtLast;
        private TextBox txtPhone;
        private Microsoft.Data.Sqlite.SqliteCommand sqliteCommand1;
        private DateTimePicker dateDOB;
        private TextBox txtEmail;
        private TextBox txtAddress;
        private Button btnSubmit;
        private Button btnCancel;
        private TextBox txtPayRate;
        private Label label8;
        private ComboBox comboLocation;
        private Label label9;
    }
}