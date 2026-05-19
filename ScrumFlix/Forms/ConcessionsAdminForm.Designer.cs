namespace ScrumFlix.Forms
{
    partial class ConcessionsAdminForm
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
            gridConcessions = new DataGridView();
            txtItemName = new TextBox();
            txtPrice = new TextBox();
            txtQuantity = new TextBox();
            txtMinimum = new TextBox();
            btnAdd = new Button();
            btnUpdate = new Button();
            btnDeactivate = new Button();
            btnRefresh = new Button();
            comboConcessionItem = new ComboBox();
            txtStockQuantity = new TextBox();
            btnIncreaseStock = new Button();
            btnDecreaseStock = new Button();
            btnSaveStock = new Button();
            btnReactivate = new Button();
            txtNotes = new TextBox();
            lblNotes = new Label();
            label2 = new Label();
            comboLocation = new ComboBox();
            ((System.ComponentModel.ISupportInitialize)gridConcessions).BeginInit();
            SuspendLayout();
            // 
            // gridConcessions
            // 
            gridConcessions.AllowUserToAddRows = false;
            gridConcessions.AllowUserToDeleteRows = false;
            gridConcessions.AllowUserToResizeColumns = false;
            gridConcessions.AllowUserToResizeRows = false;
            gridConcessions.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            gridConcessions.Location = new Point(26, 23);
            gridConcessions.MultiSelect = false;
            gridConcessions.Name = "gridConcessions";
            gridConcessions.ReadOnly = true;
            gridConcessions.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders;
            gridConcessions.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridConcessions.Size = new Size(813, 195);
            gridConcessions.TabIndex = 0;
            gridConcessions.CellClick += gridConcessions_CellClick;
            // 
            // txtItemName
            // 
            txtItemName.Location = new Point(26, 255);
            txtItemName.Name = "txtItemName";
            txtItemName.PlaceholderText = "Item Name";
            txtItemName.Size = new Size(130, 29);
            txtItemName.TabIndex = 1;
            // 
            // txtPrice
            // 
            txtPrice.Location = new Point(175, 255);
            txtPrice.Name = "txtPrice";
            txtPrice.PlaceholderText = "Price";
            txtPrice.Size = new Size(130, 29);
            txtPrice.TabIndex = 2;
            // 
            // txtQuantity
            // 
            txtQuantity.Location = new Point(26, 304);
            txtQuantity.Name = "txtQuantity";
            txtQuantity.PlaceholderText = "Quantity";
            txtQuantity.Size = new Size(130, 29);
            txtQuantity.TabIndex = 3;
            // 
            // txtMinimum
            // 
            txtMinimum.Location = new Point(175, 304);
            txtMinimum.Name = "txtMinimum";
            txtMinimum.PlaceholderText = "Minimum Limit";
            txtMinimum.Size = new Size(130, 29);
            txtMinimum.TabIndex = 4;
            // 
            // btnAdd
            // 
            btnAdd.Location = new Point(26, 348);
            btnAdd.Name = "btnAdd";
            btnAdd.Size = new Size(98, 30);
            btnAdd.TabIndex = 5;
            btnAdd.Text = "Add";
            btnAdd.UseVisualStyleBackColor = true;
            btnAdd.Click += btnAdd_Click;
            // 
            // btnUpdate
            // 
            btnUpdate.Location = new Point(175, 348);
            btnUpdate.Name = "btnUpdate";
            btnUpdate.Size = new Size(98, 30);
            btnUpdate.TabIndex = 6;
            btnUpdate.Text = "Update";
            btnUpdate.UseVisualStyleBackColor = true;
            btnUpdate.Click += btnUpdate_Click;
            // 
            // btnDeactivate
            // 
            btnDeactivate.Location = new Point(26, 395);
            btnDeactivate.Name = "btnDeactivate";
            btnDeactivate.Size = new Size(98, 30);
            btnDeactivate.TabIndex = 7;
            btnDeactivate.Text = "Deactivate";
            btnDeactivate.UseVisualStyleBackColor = true;
            btnDeactivate.Click += btnDeactivate_Click;
            // 
            // btnRefresh
            // 
            btnRefresh.Location = new Point(175, 395);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new Size(98, 30);
            btnRefresh.TabIndex = 8;
            btnRefresh.Text = "Refresh";
            btnRefresh.UseVisualStyleBackColor = true;
            btnRefresh.Click += btnRefresh_Click;
            // 
            // comboConcessionItem
            // 
            comboConcessionItem.FormattingEnabled = true;
            comboConcessionItem.Location = new Point(481, 409);
            comboConcessionItem.Name = "comboConcessionItem";
            comboConcessionItem.Size = new Size(158, 29);
            comboConcessionItem.TabIndex = 9;
            comboConcessionItem.SelectedIndexChanged += comboConcessionItem_SelectedIndexChanged;
            // 
            // txtStockQuantity
            // 
            txtStockQuantity.Location = new Point(661, 409);
            txtStockQuantity.Name = "txtStockQuantity";
            txtStockQuantity.PlaceholderText = "Select an item";
            txtStockQuantity.Size = new Size(130, 29);
            txtStockQuantity.TabIndex = 10;
            // 
            // btnIncreaseStock
            // 
            btnIncreaseStock.Location = new Point(709, 373);
            btnIncreaseStock.Name = "btnIncreaseStock";
            btnIncreaseStock.Size = new Size(32, 30);
            btnIncreaseStock.TabIndex = 11;
            btnIncreaseStock.Text = "^";
            btnIncreaseStock.UseVisualStyleBackColor = true;
            btnIncreaseStock.Click += btnIncreaseStock_Click;
            // 
            // btnDecreaseStock
            // 
            btnDecreaseStock.Location = new Point(709, 444);
            btnDecreaseStock.Name = "btnDecreaseStock";
            btnDecreaseStock.Size = new Size(32, 30);
            btnDecreaseStock.TabIndex = 12;
            btnDecreaseStock.Text = "v";
            btnDecreaseStock.UseVisualStyleBackColor = true;
            btnDecreaseStock.Click += btnDecreaseStock_Click;
            // 
            // btnSaveStock
            // 
            btnSaveStock.Location = new Point(580, 485);
            btnSaveStock.Name = "btnSaveStock";
            btnSaveStock.Size = new Size(98, 30);
            btnSaveStock.TabIndex = 13;
            btnSaveStock.Text = "Save";
            btnSaveStock.UseVisualStyleBackColor = true;
            btnSaveStock.Click += btnSaveStock_Click;
            // 
            // btnReactivate
            // 
            btnReactivate.Location = new Point(279, 395);
            btnReactivate.Name = "btnReactivate";
            btnReactivate.Size = new Size(98, 30);
            btnReactivate.TabIndex = 14;
            btnReactivate.Text = "Reactivate";
            btnReactivate.UseVisualStyleBackColor = true;
            btnReactivate.Click += btnReactivate_Click;
            // 
            // txtNotes
            // 
            txtNotes.Location = new Point(26, 450);
            txtNotes.Multiline = true;
            txtNotes.Name = "txtNotes";
            txtNotes.Size = new Size(335, 135);
            txtNotes.TabIndex = 15;
            // 
            // lblNotes
            // 
            lblNotes.AutoSize = true;
            lblNotes.Location = new Point(26, 426);
            lblNotes.Name = "lblNotes";
            lblNotes.Size = new Size(54, 21);
            lblNotes.TabIndex = 16;
            lblNotes.Text = "Notes:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 17.855999F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label2.Location = new Point(501, 316);
            label2.Name = "label2";
            label2.Size = new Size(256, 42);
            label2.TabIndex = 17;
            label2.Text = "Quick Stock Edit";
            // 
            // comboLocation
            // 
            comboLocation.FormattingEnabled = true;
            comboLocation.Location = new Point(359, 255);
            comboLocation.Name = "comboLocation";
            comboLocation.Size = new Size(158, 29);
            comboLocation.TabIndex = 18;
            comboLocation.SelectedIndexChanged += comboLocation_SelectedIndexChanged;
            // 
            // ConcessionsAdminForm
            // 
            AutoScaleDimensions = new SizeF(9F, 21F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(862, 606);
            Controls.Add(comboLocation);
            Controls.Add(label2);
            Controls.Add(lblNotes);
            Controls.Add(txtNotes);
            Controls.Add(btnReactivate);
            Controls.Add(btnSaveStock);
            Controls.Add(btnDecreaseStock);
            Controls.Add(btnIncreaseStock);
            Controls.Add(txtStockQuantity);
            Controls.Add(comboConcessionItem);
            Controls.Add(btnRefresh);
            Controls.Add(btnDeactivate);
            Controls.Add(btnUpdate);
            Controls.Add(btnAdd);
            Controls.Add(txtMinimum);
            Controls.Add(txtQuantity);
            Controls.Add(txtPrice);
            Controls.Add(txtItemName);
            Controls.Add(gridConcessions);
            Name = "ConcessionsAdminForm";
            Text = "ConcessionsAdminForm";
            Load += ConcessionsAdminForm_Load;
            ((System.ComponentModel.ISupportInitialize)gridConcessions).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DataGridView gridConcessions;
        private TextBox txtItemName;
        private TextBox txtPrice;
        private TextBox txtQuantity;
        private TextBox txtMinimum;
        private Button btnAdd;
        private Button btnUpdate;
        private Button btnDeactivate;
        private Button btnRefresh;
        private ComboBox comboConcessionItem;
        private TextBox txtStockQuantity;
        private Button btnIncreaseStock;
        private Button btnDecreaseStock;
        private Button btnSaveStock;
        private Button btnReactivate;
        private TextBox txtNotes;
        private Label lblNotes;
        private Label label2;
        private ComboBox comboLocation;
    }
}