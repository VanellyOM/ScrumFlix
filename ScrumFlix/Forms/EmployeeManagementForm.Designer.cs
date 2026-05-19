namespace ScrumFlix.Forms
{
    partial class EmployeeManagementForm
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
            gridEmployees = new DataGridView();
            button1 = new Button();
            button2 = new Button();
            button3 = new Button();
            button4 = new Button();
            ((System.ComponentModel.ISupportInitialize)gridEmployees).BeginInit();
            SuspendLayout();
            // 
            // gridEmployees
            // 
            gridEmployees.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gridEmployees.Location = new Point(13, 14);
            gridEmployees.Name = "gridEmployees";
            gridEmployees.RowHeadersWidth = 53;
            gridEmployees.Size = new Size(1757, 775);
            gridEmployees.TabIndex = 0;
            // 
            // button1
            // 
            button1.Location = new Point(12, 795);
            button1.Name = "button1";
            button1.Size = new Size(98, 30);
            button1.TabIndex = 1;
            button1.Text = "Add";
            button1.UseVisualStyleBackColor = true;
            button1.Click += btnAdd_Click;
            // 
            // button2
            // 
            button2.Location = new Point(116, 795);
            button2.Name = "button2";
            button2.Size = new Size(98, 30);
            button2.TabIndex = 2;
            button2.Text = "Edit";
            button2.UseVisualStyleBackColor = true;
            button2.Click += btnEdit_Click;
            // 
            // button3
            // 
            button3.Location = new Point(324, 795);
            button3.Name = "button3";
            button3.Size = new Size(98, 30);
            button3.TabIndex = 3;
            button3.Text = "Refresh";
            button3.UseVisualStyleBackColor = true;
            button3.Click += btnRefresh_Click;
            // 
            // button4
            // 
            button4.Location = new Point(220, 795);
            button4.Name = "button4";
            button4.Size = new Size(98, 30);
            button4.TabIndex = 4;
            button4.Text = "Delete";
            button4.UseVisualStyleBackColor = true;
            button4.Click += btnDelete_Click;
            // 
            // EmployeeManagementForm
            // 
            AutoScaleDimensions = new SizeF(9F, 21F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1782, 837);
            Controls.Add(button4);
            Controls.Add(button3);
            Controls.Add(button2);
            Controls.Add(button1);
            Controls.Add(gridEmployees);
            Name = "EmployeeManagementForm";
            Text = "EmployeeManagementForm";
            Load += EmployeeManagementForm_Load;
            ((System.ComponentModel.ISupportInitialize)gridEmployees).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private DataGridView gridEmployees;
        private Button button1;
        private Button button2;
        private Button button3;
        private Button button4;
    }
}