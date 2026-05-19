using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using ScrumFlix.Data;
using ScrumFlix.Models;
using System.Linq;

namespace ScrumFlix.Forms
{
    public partial class PayrollManagementForm : Form
    {
        public PayrollManagementForm()
        {
            InitializeComponent();
        }

        private void PayrollManagementForm_Load(object sender, EventArgs e)
        {
            LoadPayPeriods();
            LoadTimesheets();
            LoadPayrolls();
            LoadPayStubs();
        }

        // Pay periods

        private void LoadPayPeriods()
        {
            using var db = new AppDbContext();

            var payPeriods = db.PayPeriods
                .OrderBy(p => p.StartDate)
                .Select(p => new
                {
                    p.PayPeriodId,
                    Display = p.StartDate.ToString("MM/dd/yyyy") + " - " + p.EndDate.ToString("MM/dd/yyyy")
                })
                .ToList();

            comboPayPeriod.DataSource = payPeriods;
            comboPayPeriod.DisplayMember = "Display";
            comboPayPeriod.ValueMember = "PayPeriodId";
        }

        private void btnPayPeriod_Click(object sender, EventArgs e)
        {
            using var f = new PayPeriodForm();
            f.ShowDialog();

            LoadPayPeriods();
            LoadTimesheets();
            LoadPayrolls();
            LoadPayStubs();
        }









        // Timesheets
        private void LoadTimesheets()
        {
            using var db = new AppDbContext();
            int? selectedPayPeriodId = null;

            if (comboPayPeriod.SelectedValue is int id)
            {
                selectedPayPeriodId = id;
            }

            var query = db.Timesheets
                .Include(t => t.Employee)
                .Include(t => t.PayPeriod)
                .Include(t => t.ApprovedByUser)
                .AsQueryable();

            if (selectedPayPeriodId != null)
            {
                query = query.Where(t => t.PayPeriodId == selectedPayPeriodId.Value);
            }

            var timesheets = query
                .OrderBy(t => t.PayPeriod!.StartDate)
                .ThenBy(t => t.Employee!.LastName)
                .Select(t => new
                {
                    t.TimesheetId,
                    Employee = t.Employee!.FullName,
                    PayPeriod = t.PayPeriod!.StartDate.ToString("MM/dd/yyyy") + " - " + t.PayPeriod.EndDate.ToString("MM/dd/yyyy"),
                    t.TotalHours,
                    t.Approved,
                    ApprovedBy = t.ApprovedByUser != null ? t.ApprovedByUser.UserName : ""
                })
                .ToList();

            gridTimesheets.DataSource = timesheets;

            if (gridTimesheets.Columns.Count > 0)
            {
                gridTimesheets.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            }

            if (gridTimesheets.Columns["TimesheetId"] != null)
            {
                gridTimesheets.Columns["TimesheetId"].Visible = false;
            }
        }

        private void btnGenerateTimesheets_Click(object sender, EventArgs e)
        {
            if (comboPayPeriod.SelectedValue == null)
            {
                MessageBox.Show("Select a pay period first.");
                return;
            }

            int payPeriodId = Convert.ToInt32(comboPayPeriod.SelectedValue);

            using var db = new AppDbContext();

            var payPeriod = db.PayPeriods.FirstOrDefault(p => p.PayPeriodId == payPeriodId);

            if (payPeriod == null)
            {
                MessageBox.Show("Pay period not found.");
                return;
            }

            DateTime startDate = payPeriod.StartDate.Date;
            DateTime endDateExclusive = payPeriod.EndDate.Date.AddDays(1);

            var entries = db.TimeEntries
                .Where(t =>
                    t.ClockOut != null &&
                    t.ClockIn >= startDate &&
                    t.ClockIn < endDateExclusive)
                .ToList();

            if (!entries.Any())
            {
                MessageBox.Show("No completed time entries found for this pay period.");
                return;
            }

            var groupedEntries = entries
                .GroupBy(t => t.EmployeeId)
                .ToList();

            int createdCount = 0;
            int updatedCount = 0;

            foreach (var group in groupedEntries)
            {
                int employeeId = group.Key;

                decimal totalHours = group.Sum(t =>
                    (decimal)(t.ClockOut!.Value - t.ClockIn).TotalHours);

                totalHours = Math.Round(totalHours, 2);

                var existingTimesheet = db.Timesheets.FirstOrDefault(t =>
                    t.EmployeeId == employeeId &&
                    t.PayPeriodId == payPeriodId);

                if (existingTimesheet == null)
                {
                    var timesheet = new Timesheet
                    {
                        EmployeeId = employeeId,
                        PayPeriodId = payPeriodId,
                        TotalHours = totalHours,
                        Approved = false,
                        ApprovedByUserId = null
                    };

                    db.Timesheets.Add(timesheet);
                    createdCount++;
                }
                else
                {
                    existingTimesheet.TotalHours = totalHours;
                    updatedCount++;
                }
            }

            db.SaveChanges();

            db.AuditLog.Add(new AuditLog
            {
                UserId = Session.UserId,
                ActionType = "GENERATE_TIMESHEETS",
                TableName = "Timesheets",
                ObjectId = payPeriodId,
                ActionTime = DateTime.Now,
                Description = $"Generated timesheets for pay period {payPeriod.StartDate:d} - {payPeriod.EndDate:d}",
                OldValues = null,
                NewValues = $"Created={createdCount}, Updated={updatedCount}, PayPeriodId={payPeriodId}"
            });

            db.SaveChanges();

            MessageBox.Show($"Timesheets generated.\nCreated: {createdCount}\nUpdated: {updatedCount}");

            LoadTimesheets();
            LoadPayrolls();
            LoadPayStubs();
        }
        private int? GetSelectedTimesheetId()
        {
            if (gridTimesheets.CurrentRow == null)
                return null;

            var value = gridTimesheets.CurrentRow.Cells["TimesheetId"].Value;

            if (value == null)
                return null;

            if (int.TryParse(value.ToString(), out int timesheetId))
                return timesheetId;

            return null;
        }

        private void btnApproveTimesheet_Click(object sender, EventArgs e)
        {
            var timesheetId = GetSelectedTimesheetId();

            if (timesheetId == null)
            {
                MessageBox.Show("Select a timesheet first.");
                return;
            }

            using var db = new AppDbContext();

            var timesheet = db.Timesheets
                .Include(t => t.Employee)
                .FirstOrDefault(t => t.TimesheetId == timesheetId.Value);

            if (timesheet == null)
            {
                MessageBox.Show("Timesheet not found.");
                return;
            }

            bool oldApproved = timesheet.Approved;
            int? oldApprovedBy = timesheet.ApprovedByUserId;

            timesheet.Approved = true;
            timesheet.ApprovedByUserId = Session.UserId;

            db.AuditLog.Add(new AuditLog
            {
                UserId = Session.UserId,
                ActionType = "APPROVE_TIMESHEET",
                TableName = "Timesheets",
                ObjectId = timesheet.TimesheetId,
                ActionTime = DateTime.Now,
                Description = $"Approved timesheet for '{timesheet.Employee?.FullName}'",
                OldValues = $"Approved={oldApproved}, ApprovedByUserId={oldApprovedBy}",
                NewValues = $"Approved={timesheet.Approved}, ApprovedByUserId={timesheet.ApprovedByUserId}"
            });

            db.SaveChanges();

            LoadTimesheets();
            LoadPayrolls();
            LoadPayStubs();
        }

        private void btnUnapproveTimesheet_Click(object sender, EventArgs e)
        {
            var timesheetId = GetSelectedTimesheetId();

            if (timesheetId == null)
            {
                MessageBox.Show("Select a timesheet first.");
                return;
            }

            using var db = new AppDbContext();

            var timesheet = db.Timesheets
                .Include(t => t.Employee)
                .FirstOrDefault(t => t.TimesheetId == timesheetId.Value);

            if (timesheet == null)
            {
                MessageBox.Show("Timesheet not found.");
                return;
            }

            bool oldApproved = timesheet.Approved;
            int? oldApprovedBy = timesheet.ApprovedByUserId;

            timesheet.Approved = false;
            timesheet.ApprovedByUserId = null;

            db.AuditLog.Add(new AuditLog
            {
                UserId = Session.UserId,
                ActionType = "UNAPPROVE_TIMESHEET",
                TableName = "Timesheets",
                ObjectId = timesheet.TimesheetId,
                ActionTime = DateTime.Now,
                Description = $"Unapproved timesheet for '{timesheet.Employee?.FullName}'",
                OldValues = $"Approved={oldApproved}, ApprovedByUserId={oldApprovedBy}",
                NewValues = $"Approved={timesheet.Approved}, ApprovedByUserId={timesheet.ApprovedByUserId}"
            });

            db.SaveChanges();

            LoadTimesheets();
            LoadPayrolls();
            LoadPayStubs();
        }





        // Payrolls
        private void LoadPayrolls()
        {
            using var db = new AppDbContext();

            int? selectedPayPeriodId = null;

            if (comboPayPeriod.SelectedValue is int id)
            {
                selectedPayPeriodId = id;
            }

            var query = db.Payrolls
                .Include(p => p.Employee)
                .Include(p => p.PayPeriod)
                .AsQueryable();

            if (selectedPayPeriodId != null)
            {
                query = query.Where(p => p.PayPeriodId == selectedPayPeriodId.Value);
            }

            var payrolls = query
                .OrderBy(p => p.PayPeriod!.StartDate)
                .ThenBy(p => p.Employee!.LastName)
                .Select(p => new
                {
                    p.PayrollId,
                    Employee = p.Employee!.FullName,
                    PayPeriod = p.PayPeriod!.StartDate.ToString("MM/dd/yyyy") + " - " + p.PayPeriod.EndDate.ToString("MM/dd/yyyy"),
                    p.GrossPay
                })
                .ToList();

            gridPayrolls.DataSource = payrolls;

            if (gridPayrolls.Columns.Count > 0)
            {
                gridPayrolls.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            }

            if (gridPayrolls.Columns["PayrollId"] != null)
            {
                gridPayrolls.Columns["PayrollId"].Visible = false;
            }
        }

        private void btnGeneratePayroll_Click(object sender, EventArgs e)
        {
            if (comboPayPeriod.SelectedValue == null)
            {
                MessageBox.Show("Select a pay period first.");
                return;
            }

            int payPeriodId = Convert.ToInt32(comboPayPeriod.SelectedValue);

            using var db = new AppDbContext();

            var payPeriod = db.PayPeriods.FirstOrDefault(p => p.PayPeriodId == payPeriodId);

            if (payPeriod == null)
            {
                MessageBox.Show("Pay period not found.");
                return;
            }

            var approvedTimesheets = db.Timesheets
                .Include(t => t.Employee)
                .Where(t => t.PayPeriodId == payPeriodId && t.Approved)
                .ToList();

            if (!approvedTimesheets.Any())
            {
                MessageBox.Show("No approved timesheets found for this pay period.");
                return;
            }

            int createdCount = 0;
            int updatedCount = 0;

            foreach (var timesheet in approvedTimesheets)
            {
                if (timesheet.Employee == null)
                    continue;

                decimal grossPay = timesheet.TotalHours * timesheet.Employee.PayRate;
                grossPay = Math.Round(grossPay, 2);

                var existingPayroll = db.Payrolls.FirstOrDefault(p =>
                    p.EmployeeId == timesheet.EmployeeId &&
                    p.PayPeriodId == payPeriodId);

                if (existingPayroll == null)
                {
                    var payroll = new Payroll
                    {
                        EmployeeId = timesheet.EmployeeId,
                        PayPeriodId = payPeriodId,
                        GrossPay = grossPay
                    };

                    db.Payrolls.Add(payroll);
                    createdCount++;
                }
                else
                {
                    existingPayroll.GrossPay = grossPay;
                    updatedCount++;
                }
            }

            db.SaveChanges();

            db.AuditLog.Add(new AuditLog
            {
                UserId = Session.UserId,
                ActionType = "GENERATE_PAYROLL",
                TableName = "Payrolls",
                ObjectId = payPeriodId,
                ActionTime = DateTime.Now,
                Description = $"Generated payroll for pay period {payPeriod.StartDate:d} - {payPeriod.EndDate:d}",
                OldValues = null,
                NewValues = $"Created={createdCount}, Updated={updatedCount}, PayPeriodId={payPeriodId}"
            });

            db.SaveChanges();

            MessageBox.Show($"Payroll generated.\nCreated: {createdCount}\nUpdated: {updatedCount}");

            LoadTimesheets();
            LoadPayrolls();
            LoadPayStubs();
        }





        // Pay Stubs
        private void LoadPayStubs()
        {
            using var db = new AppDbContext();

            int? selectedPayPeriodId = null;

            if (comboPayPeriod.SelectedValue is int id)
            {
                selectedPayPeriodId = id;
            }

            var query = db.PayStubs
                .Include(ps => ps.Payroll)
                    .ThenInclude(p => p.Employee)
                .Include(ps => ps.Payroll)
                    .ThenInclude(p => p.PayPeriod)
                .AsQueryable();

            if (selectedPayPeriodId != null)
            {
                query = query.Where(ps =>
                    ps.Payroll!.PayPeriodId == selectedPayPeriodId.Value);
            }

            var payStubs = query
                .OrderBy(ps => ps.IssueDate)
                .ThenBy(ps => ps.Payroll!.Employee!.LastName)
                .Select(ps => new
                {
                    ps.PayStubId,
                    Employee = ps.Payroll!.Employee!.FullName,
                    PayPeriod = ps.Payroll.PayPeriod!.StartDate.ToString("MM/dd/yyyy") + " - " + ps.Payroll.PayPeriod.EndDate.ToString("MM/dd/yyyy"),
                    ps.Payroll.GrossPay,
                    ps.IssueDate
                })
                .ToList();

            gridPayStubs.DataSource = payStubs;

            if (gridPayStubs.Columns.Count > 0)
            {
                gridPayStubs.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            }

            if (gridPayStubs.Columns["PayStubId"] != null)
            {
                gridPayStubs.Columns["PayStubId"].Visible = false;
            }
        }

        private void btnGeneratePayStubs_Click(object sender, EventArgs e)
        {
            if (comboPayPeriod.SelectedValue == null)
            {
                MessageBox.Show("Select a pay period first.");
                return;
            }

            int payPeriodId = Convert.ToInt32(comboPayPeriod.SelectedValue);

            using var db = new AppDbContext();

            var payPeriod = db.PayPeriods.FirstOrDefault(p => p.PayPeriodId == payPeriodId);

            if (payPeriod == null)
            {
                MessageBox.Show("Pay period not found.");
                return;
            }

            var payrolls = db.Payrolls
                .Where(p => p.PayPeriodId == payPeriodId)
                .ToList();

            if (!payrolls.Any())
            {
                MessageBox.Show("No payroll records found for this pay period. Generate payroll first.");
                return;
            }

            int createdCount = 0;
            int skippedCount = 0;

            foreach (var payroll in payrolls)
            {
                bool alreadyExists = db.PayStubs.Any(ps => ps.PayrollId == payroll.PayrollId);

                if (alreadyExists)
                {
                    skippedCount++;
                    continue;
                }

                var payStub = new PayStub
                {
                    PayrollId = payroll.PayrollId,
                    IssueDate = DateTime.Now
                };

                db.PayStubs.Add(payStub);
                createdCount++;
            }

            db.SaveChanges();

            db.AuditLog.Add(new AuditLog
            {
                UserId = Session.UserId,
                ActionType = "GENERATE_PAY_STUBS",
                TableName = "PayStubs",
                ObjectId = payPeriodId,
                ActionTime = DateTime.Now,
                Description = $"Generated pay stubs for pay period {payPeriod.StartDate:d} - {payPeriod.EndDate:d}",
                OldValues = null,
                NewValues = $"Created={createdCount}, SkippedExisting={skippedCount}, PayPeriodId={payPeriodId}"
            });

            db.SaveChanges();

            MessageBox.Show($"Pay stubs generated.\nCreated: {createdCount}\nSkipped existing: {skippedCount}");

            LoadPayStubs();
        }




        // view pay stub
        private int? GetSelectedPayStubId()
        {
            if (gridPayStubs.CurrentRow == null)
                return null;

            var value = gridPayStubs.CurrentRow.Cells["PayStubId"].Value;

            if (value == null)
                return null;

            if (int.TryParse(value.ToString(), out int payStubId))
                return payStubId;

            return null;
        }

        private void btnViewPayStub_Click(object sender, EventArgs e)
        {
            var payStubId = GetSelectedPayStubId();

            if (payStubId == null)
            {
                MessageBox.Show("Select a pay stub first.");
                return;
            }

            using var db = new AppDbContext();

            var payStub = db.PayStubs
                .Include(ps => ps.Payroll)
                    .ThenInclude(p => p.Employee)
                .Include(ps => ps.Payroll)
                    .ThenInclude(p => p.PayPeriod)
                .FirstOrDefault(ps => ps.PayStubId == payStubId.Value);

            if (payStub == null || payStub.Payroll == null)
            {
                MessageBox.Show("Pay stub not found.");
                return;
            }

            var employee = payStub.Payroll.Employee;
            var payPeriod = payStub.Payroll.PayPeriod;

            var timesheet = db.Timesheets.FirstOrDefault(t =>
                t.EmployeeId == payStub.Payroll.EmployeeId &&
                t.PayPeriodId == payStub.Payroll.PayPeriodId);

            decimal totalHours = timesheet?.TotalHours ?? 0;
            decimal payRate = employee?.PayRate ?? 0;
            decimal grossPay = payStub.Payroll.GrossPay;

            MessageBox.Show(
                $"Employee: {employee?.FullName}\n" +
                $"Pay Period: {payPeriod?.StartDate:MM/dd/yyyy} - {payPeriod?.EndDate:MM/dd/yyyy}\n" +
                $"Total Hours: {totalHours}\n" +
                $"Pay Rate: {payRate:C}\n" +
                $"Gross Pay: {grossPay:C}\n" +
                $"Issue Date: {payStub.IssueDate:MM/dd/yyyy hh:mm tt}",
                "Pay Stub Details"
            );
        }

        private void comboPayPeriod_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadTimesheets();
            LoadPayrolls();
            LoadPayStubs();
        }
    }
}