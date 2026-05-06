using Microsoft.EntityFrameworkCore;
using ScrumFlix.Data;
using ScrumFlix.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace ScrumFlix.Forms
{
    public partial class ManagerForm : Form
    {
        public ManagerForm()
        {
            InitializeComponent();
        }
        private void ManagerForm_Load(object sender, EventArgs e)
        {
            using var context = new AppDbContext();

            var user = context.Users
                .Include(u => u.Employee)
                .FirstOrDefault(u => u.UserId == Session.UserId);

            if (user != null && user.Employee != null)
            {
                string fullName = $"{user.Employee.FirstName} {user.Employee.LastName}";
                lblTitle.Text = $"Welcome {fullName}!";
            }
            lblTitle.AutoSize = true;
            lblTitle.Left = (this.ClientSize.Width - lblTitle.Width) / 2;

            LoadStockAlert();
        }

        private void LoadStockAlert()
        {
            using var context = new AppDbContext();

            var lowStockItems = context.ConcessionItem
                .Where(c => c.is_active && c.QuantityInStock <= c.Minimum)
                .OrderBy(c => c.ItemName)
                .Select(c => new
                {
                    c.ItemName,
                    c.QuantityInStock
                })
                .ToList();

            if (lowStockItems.Count == 0)
            {
                lblStockAlert.Text = "";
                return;
            }

            lblStockAlert.ForeColor = Color.White;
            lblStockAlert.AutoSize = true;
            lblStockAlert.Text = "Low stock:\n" +
                string.Join("\n", lowStockItems.Select(i => $"{i.ItemName} ({i.QuantityInStock})"));
        }

        private void btnShowtimes_Click(object sender, EventArgs e)
        {
            using var f = new ShowtimesForm();
            f.ShowDialog();
        }

        private void btnConcessions_Click(object sender, EventArgs e)
        {
            using var f = new ConcessionsAdminForm();
            f.ShowDialog();
        }

        private void btnEmployees_Click(object sender, EventArgs e)
        {
            using var f = new EmployeeManagementForm();
            f.ShowDialog();
        }

        private void btnSchedules_Click(object sender, EventArgs e)
        {
            using var f = new ScheduleManagementForm();
            f.ShowDialog();
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            using (var db = new AppDbContext())
            {
                db.AuditLog.Add(new AuditLog
                {
                    UserId = Session.UserId,
                    ActionType = "LOGOUT",
                    TableName = "Users",
                    ObjectId = Session.UserId,
                    ActionTime = DateTime.Now,
                    Description = "User logged out",
                    OldValues = null,
                    NewValues = null
                });

                db.SaveChanges();
            }

            Session.UserId = 0;
            Session.UserName = null;
            Session.RoleId = 0;

            new LoginForm().Show();
            this.Hide();
        }

        private void btnReports_Click(object sender, EventArgs e)
        {
            var f = new ReportsForm();
            f.Show();
        }

        private void btnPayroll_Click(object sender, EventArgs e)
        {
            using var f = new PayrollManagementForm();
            f.ShowDialog();
        }

        private void btnDashboard_Click(object sender, EventArgs e)
        {
            var f = new DashboardForm();
            f.Show();
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Session.UserId != 0)
            {
                using var db = new AppDbContext();

                db.AuditLog.Add(new AuditLog
                {
                    UserId = Session.UserId,
                    ActionType = "APP_CLOSE",
                    TableName = "Users",
                    ObjectId = Session.UserId,
                    ActionTime = DateTime.Now,
                    Description = "User closed the application",
                    OldValues = null,
                    NewValues = null
                });

                db.SaveChanges();
            }
        }
    }
}
