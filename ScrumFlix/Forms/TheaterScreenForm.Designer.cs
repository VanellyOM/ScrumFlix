namespace ScrumFlix.Forms
{
    partial class TheaterScreenForm
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
            gridScreens = new DataGridView();
            btnAdd = new Button();
            btnEdit = new Button();
            btnDelete = new Button();
            btnRefresh = new Button();
            ((System.ComponentModel.ISupportInitialize)gridScreens).BeginInit();
            SuspendLayout();
            // 
            // gridScreens
            // 
            gridScreens.AllowUserToAddRows = false;
            gridScreens.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gridScreens.Location = new Point(12, 12);
            gridScreens.MultiSelect = false;
            gridScreens.Name = "gridScreens";
            gridScreens.ReadOnly = true;
            gridScreens.RowHeadersWidth = 53;
            gridScreens.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridScreens.Size = new Size(1529, 390);
            gridScreens.TabIndex = 0;
            // 
            // btnAdd
            // 
            btnAdd.Location = new Point(12, 408);
            btnAdd.Name = "btnAdd";
            btnAdd.Size = new Size(98, 30);
            btnAdd.TabIndex = 1;
            btnAdd.Text = "Add";
            btnAdd.UseVisualStyleBackColor = true;
            btnAdd.Click += btnAdd_Click;
            // 
            // btnEdit
            // 
            btnEdit.Location = new Point(116, 408);
            btnEdit.Name = "btnEdit";
            btnEdit.Size = new Size(98, 30);
            btnEdit.TabIndex = 2;
            btnEdit.Text = "Edit";
            btnEdit.UseVisualStyleBackColor = true;
            btnEdit.Click += btnEdit_Click;
            // 
            // btnDelete
            // 
            btnDelete.Location = new Point(220, 408);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(98, 30);
            btnDelete.TabIndex = 3;
            btnDelete.Text = "Delete";
            btnDelete.UseVisualStyleBackColor = true;
            btnDelete.Click += btnDelete_Click;
            // 
            // btnRefresh
            // 
            btnRefresh.Location = new Point(324, 408);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new Size(98, 30);
            btnRefresh.TabIndex = 4;
            btnRefresh.Text = "Refresh";
            btnRefresh.UseVisualStyleBackColor = true;
            btnRefresh.Click += btnRefresh_Click;
            // 
            // TheaterScreenForm
            // 
            AutoScaleDimensions = new SizeF(9F, 21F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1553, 450);
            Controls.Add(btnRefresh);
            Controls.Add(btnDelete);
            Controls.Add(btnEdit);
            Controls.Add(btnAdd);
            Controls.Add(gridScreens);
            Name = "TheaterScreenForm";
            Text = "TheaterScreenForm";
            Load += TheaterScreenForm_Load;
            ((System.ComponentModel.ISupportInitialize)gridScreens).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private DataGridView gridScreens;
        private Button btnAdd;
        private Button btnEdit;
        private Button btnDelete;
        private Button btnRefresh;
    }
}