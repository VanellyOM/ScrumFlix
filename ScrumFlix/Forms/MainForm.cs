/* FILE OVERVIEW:
MainForm.cs is the main form that opens first when running the scrumflix program.
It opens a screen with 4 buttons (so far) that open other forms when clicked 
on to manage movies, showtimes, screens, and locations.

In the solution explorer you will see folders, the data folder holds the database connection in AppDbContext.cs
the forms folder includes all the forms the project uses,
the models folder holds all the object classes for the movies, showtimes, screens, and locations.
Program.cs is what runs when starting the program, opening the main form */

using Microsoft.EntityFrameworkCore;
using ScrumFlix.Data;
using ScrumFlix.Forms;
using ScrumFlix.Models;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ScrumFlix
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }
        private void MainForm_Load(object sender, EventArgs e)
        {
            /*using var db = new AppDbContext();

            if (!db.Movies.Any())
            {
                db.Movies.Add(new Movie
                {
                    Title = "Test Movie",
                    Rating = "PG",
                    RuntimeMinutes = 120
                });

                db.SaveChanges();
            }*/

            // int count = db.Movies.Count();
            // MessageBox.Show($"Movies in database: {count}");

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

        // Opens MovieForm.cs when clicked
        private void btnMovies_Click(object sender, EventArgs e)
        {
            using var f = new MoviesForm();
            f.ShowDialog();
        }
        // Opens ShowtimesForm.cs when clicked
        private void btnShowtimes_Click(object sender, EventArgs e)
        {
            using var f = new ShowtimesForm();
            f.ShowDialog();
        }
        // Opens TheaterScreenForm.cs when clicked
        private void btnTheaterScreen_Click(object sender, EventArgs e)
        {
            using var f = new TheaterScreenForm();
            f.ShowDialog();
        }
        // Opens LocationForm.cs when clicked
        private void button1_Click(object sender, EventArgs e)
        {
            using var f = new LocationForm();
            f.ShowDialog();
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void btnUsers_Click(object sender, EventArgs e)
        {
            using var f = new UserManagementForm();
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

        private void btnDashboard_Click(object sender, EventArgs e)
        {
            var f = new DashboardForm();
            f.Show();
        }

        private void btnBackupRestore_Click(object sender, EventArgs e)
        {
            using var f = new BackupRestoreForm();
            f.ShowDialog();
        }

        private void btnPOS_Click(object sender, EventArgs e)
        {

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
