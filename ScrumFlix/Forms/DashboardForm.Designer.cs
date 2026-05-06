namespace ScrumFlix.Forms
{
    partial class DashboardForm
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
            webViewDashboard = new Microsoft.Web.WebView2.WinForms.WebView2();
            ((System.ComponentModel.ISupportInitialize)webViewDashboard).BeginInit();
            SuspendLayout();
            // 
            // webViewDashboard
            // 
            webViewDashboard.AllowExternalDrop = true;
            webViewDashboard.CreationProperties = null;
            webViewDashboard.DefaultBackgroundColor = Color.White;
            webViewDashboard.Dock = DockStyle.Fill;
            webViewDashboard.Location = new Point(0, 0);
            webViewDashboard.Name = "webViewDashboard";
            webViewDashboard.Size = new Size(2113, 1002);
            webViewDashboard.TabIndex = 0;
            webViewDashboard.ZoomFactor = 1D;
            // 
            // DashboardForm
            // 
            AutoScaleDimensions = new SizeF(9F, 21F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(2113, 1002);
            Controls.Add(webViewDashboard);
            Name = "DashboardForm";
            Text = "DashboardForm";
            Load += DashboardForm_Load;
            ((System.ComponentModel.ISupportInitialize)webViewDashboard).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Microsoft.Web.WebView2.WinForms.WebView2 webViewDashboard;
    }
}