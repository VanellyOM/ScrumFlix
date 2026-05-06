using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;

namespace ScrumFlix.Forms
{
    public partial class DashboardForm : Form
    {
        public DashboardForm()
        {
            InitializeComponent();
        }

        private async void DashboardForm_Load(object sender, EventArgs e)
        {
            await webViewDashboard.EnsureCoreWebView2Async(null);
            webViewDashboard.Source = new Uri("http://127.0.0.1:8050/");
        }
}
}
