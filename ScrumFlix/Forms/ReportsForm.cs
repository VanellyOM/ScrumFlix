using Microsoft.EntityFrameworkCore;
using ScrumFlix.Data;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ScrumFlix.Forms
{
    public partial class ReportsForm : Form
    {
        public ReportsForm()
        {
            InitializeComponent();
        }

        private string DownloadsFolder =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

        private void ReportsForm_Load(object sender, EventArgs e)
        {
            dateStart.Value = new DateTime(2026, 5, 1);
            dateEnd.Value = new DateTime(2026, 5, 31);

            LoadLocations();
        }

        private void btnTicketSalesReport_Click(object sender, EventArgs e)
        {
            DateTime start = dateStart.Value.Date;
            DateTime endExclusive = dateEnd.Value.Date.AddDays(1);

            using var db = new AppDbContext();

            int locationId = Convert.ToInt32(comboLocation.SelectedValue);

            var rows = db.Ticket
                .Include(t => t.Showtime)
                    .ThenInclude(s => s.Movie)
                .Include(t => t.Showtime)
                    .ThenInclude(s => s.TheaterScreen)
                        .ThenInclude(ts => ts.Location)
                .Where(t =>
                    t.TimeOfSale >= start &&
                    t.TimeOfSale < endExclusive &&
                    t.Showtime!.TheaterScreen!.LocationId == locationId)
                .GroupBy(t => new
                {
                    Theater = t.Showtime!.TheaterScreen!.Location!.LocationName,
                    SaleDate = t.TimeOfSale.Date
                })
                .Select(g => new
                {
                    g.Key.Theater,
                    g.Key.SaleDate,
                    TicketsSold = g.Count(),
                    TotalRevenue = g.Sum(t => t.Showtime!.PricePerTicket)
                })
                .OrderBy(r => r.SaleDate)
                .ThenBy(r => r.Theater)
                .ToList();

            if (!rows.Any())
            {
                MessageBox.Show("No ticket sales found for this date range.");
                return;
            }

            var csv = new StringBuilder();
            csv.AppendLine("Theater,SaleDate,TicketsSold,TotalRevenue");

            var popup = new StringBuilder();
            popup.AppendLine("Ticket Sales Report");
            string locationName = comboLocation.Text;
            popup.AppendLine($"Location: {locationName}");
            popup.AppendLine($"{start:d} - {dateEnd.Value.Date:d}");
            popup.AppendLine();

            foreach (var r in rows)
            {
                csv.AppendLine($"{r.Theater},{r.SaleDate:MM/dd/yyyy},{r.TicketsSold},{r.TotalRevenue:0.00}");
                popup.AppendLine($"{r.Theater} | {r.SaleDate:MM/dd/yyyy} | Tickets: {r.TicketsSold} | Revenue: {r.TotalRevenue:C}");
            }

            string filePath = Path.Combine(DownloadsFolder, $"TicketSalesReport_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            File.WriteAllText(filePath, csv.ToString());

            MessageBox.Show(popup + $"\n\nSaved to:\n{filePath}", "Ticket Sales Report");
        }

        private void btnConcessionSalesReport_Click(object sender, EventArgs e)
        {
            DateTime start = dateStart.Value.Date;
            DateTime endExclusive = dateEnd.Value.Date.AddDays(1);

            using var db = new AppDbContext();

            int locationId = Convert.ToInt32(comboLocation.SelectedValue);

            var rows = db.ConcessionSale
                .Include(s => s.User)
                    .ThenInclude(u => u.Employee)
                .Where(s =>
                    s.TimeOfSale >= start &&
                    s.TimeOfSale < endExclusive &&
                    s.User != null &&
                    s.User.Employee != null &&
                    s.User.Employee.LocationId == locationId)
                .Select(s => new
                {
                    s.ConcessionSaleId,
                    s.TimeOfSale,
                    s.CustomerEmail,
                    s.Total,
                    SoldBy = s.User != null ? s.User.UserName : "",
                    Location = s.User!.Employee!.Location!.LocationName
                })
                .OrderBy(s => s.TimeOfSale)
                .ToList();

            if (!rows.Any())
            {
                MessageBox.Show("No concession sales found for this date range.");
                return;
            }

            var csv = new StringBuilder();
            csv.AppendLine("SaleId,TimeOfSale,CustomerEmail,Total,SoldBy");

            var popup = new StringBuilder();
            popup.AppendLine("Concession Sales Report");
            string locationName = comboLocation.Text;
            popup.AppendLine($"Location: {locationName}");
            popup.AppendLine($"{start:d} - {dateEnd.Value.Date:d}");
            popup.AppendLine();

            foreach (var r in rows)
            {
                csv.AppendLine($"{r.ConcessionSaleId},{r.TimeOfSale},{r.CustomerEmail},{r.Total:0.00},{r.SoldBy}");
                popup.AppendLine($"Sale #{r.ConcessionSaleId} | {r.TimeOfSale:g} | {r.Total:C} | {r.CustomerEmail}");
            }

            string filePath = Path.Combine(DownloadsFolder, $"ConcessionSalesReport_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            File.WriteAllText(filePath, csv.ToString());

            MessageBox.Show(popup + $"\n\nSaved to:\n{filePath}", "Concession Sales Report");
        }

        private void btnPayrollReport_Click(object sender, EventArgs e)
        {
            using var db = new AppDbContext();

            var rows = db.Payrolls
                .Include(p => p.Employee)
                .Include(p => p.PayPeriod)
                .OrderBy(p => p.PayPeriod!.StartDate)
                .ThenBy(p => p.Employee!.LastName)
                .Select(p => new
                {
                    p.PayrollId,
                    Employee = p.Employee!.FullName,
                    PayPeriod = p.PayPeriod!.StartDate.ToString("MM/dd/yyyy") + " - " + p.PayPeriod.EndDate.ToString("MM/dd/yyyy"),
                    p.Employee.PayRate,
                    p.GrossPay
                })
                .ToList();

            if (!rows.Any())
            {
                MessageBox.Show("No payroll records found.");
                return;
            }

            var csv = new StringBuilder();
            csv.AppendLine("PayrollId,Employee,PayPeriod,PayRate,GrossPay");

            var popup = new StringBuilder();
            popup.AppendLine("Payroll Export");
            popup.AppendLine();

            foreach (var r in rows)
            {
                csv.AppendLine($"{r.PayrollId},{r.Employee},{r.PayPeriod},{r.PayRate:0.00},{r.GrossPay:0.00}");
                popup.AppendLine($"{r.Employee} | {r.PayPeriod} | Gross Pay: {r.GrossPay:C}");
            }

            string filePath = Path.Combine(DownloadsFolder, $"PayrollReport_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            File.WriteAllText(filePath, csv.ToString());

            MessageBox.Show(popup + $"\n\nSaved to:\n{filePath}", "Payroll Report");
        }
        private void LoadLocations()
        {
            using var db = new AppDbContext();

            var locations = db.Location
                .Where(l => l.IsActive)
                .OrderBy(l => l.LocationName)
                .Select(l => new
                {
                    l.LocationId,
                    l.LocationName
                })
                .ToList();

            comboLocation.DataSource = locations;
            comboLocation.DisplayMember = "LocationName";
            comboLocation.ValueMember = "LocationId";
        }
    }
}