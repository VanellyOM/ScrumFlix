namespace ScrumFlix.Forms
{
    partial class MoviesForm
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
            btnAdd = new Button();
            gridMovies = new DataGridView();
            btnEdit = new Button();
            btnDelete = new Button();
            btnRefresh = new Button();
            ((System.ComponentModel.ISupportInitialize)gridMovies).BeginInit();
            SuspendLayout();
            // 
            // btnAdd
            // 
            btnAdd.Location = new Point(12, 421);
            btnAdd.Name = "btnAdd";
            btnAdd.Size = new Size(141, 49);
            btnAdd.TabIndex = 1;
            btnAdd.Text = "Add";
            btnAdd.UseVisualStyleBackColor = true;
            btnAdd.Click += btnAdd_Click;
            // 
            // gridMovies
            // 
            gridMovies.AllowUserToAddRows = false;
            gridMovies.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gridMovies.Location = new Point(12, 12);
            gridMovies.MultiSelect = false;
            gridMovies.Name = "gridMovies";
            gridMovies.ReadOnly = true;
            gridMovies.RowHeadersWidth = 53;
            gridMovies.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridMovies.Size = new Size(1746, 403);
            gridMovies.TabIndex = 2;
            // 
            // btnEdit
            // 
            btnEdit.Location = new Point(159, 421);
            btnEdit.Name = "btnEdit";
            btnEdit.Size = new Size(141, 49);
            btnEdit.TabIndex = 3;
            btnEdit.Text = "Edit";
            btnEdit.UseVisualStyleBackColor = true;
            btnEdit.Click += btnEdit_Click;
            // 
            // btnDelete
            // 
            btnDelete.Location = new Point(306, 421);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(141, 49);
            btnDelete.TabIndex = 4;
            btnDelete.Text = "Delete";
            btnDelete.UseVisualStyleBackColor = true;
            btnDelete.Click += btnDelete_Click;
            // 
            // btnRefresh
            // 
            btnRefresh.Location = new Point(453, 421);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new Size(141, 49);
            btnRefresh.TabIndex = 5;
            btnRefresh.Text = "Refresh";
            btnRefresh.UseVisualStyleBackColor = true;
            btnRefresh.Click += btnRefresh_Click;
            // 
            // MoviesForm
            // 
            AutoScaleDimensions = new SizeF(9F, 21F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1770, 482);
            Controls.Add(btnRefresh);
            Controls.Add(btnDelete);
            Controls.Add(btnEdit);
            Controls.Add(gridMovies);
            Controls.Add(btnAdd);
            Name = "MoviesForm";
            Text = "MoviesForm";
            Load += MoviesForm_Load;
            ((System.ComponentModel.ISupportInitialize)gridMovies).EndInit();
            ResumeLayout(false);
        }

        #endregion
        private Button btnAdd;
        private DataGridView gridMovies;
        private Button btnEdit;
        private Button btnDelete;
        private Button btnRefresh;
    }
}