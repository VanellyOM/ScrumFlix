using Microsoft.EntityFrameworkCore;
using ScrumFlix.Data;
using ScrumFlix.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using static Google.Protobuf.Reflection.SourceCodeInfo.Types;

namespace ScrumFlix.Forms
{
    public partial class EmployeeForm : Form
    {
        public EmployeeForm()
        {
            InitializeComponent();
        }

        private bool IsValidEmail(string email)
        {
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            string search = txtSearch.Text.ToLower();

            using (var context = new AppDbContext())
            {
                int? locationId = GetCurrentEmployeeLocationId();
                var filtered = context.Showtime
                    .Where(s =>
                        s.TheaterScreen!.LocationId == locationId &&
                        s.Movie!.Title.ToLower().Contains(search))
                    .Select(s => new
                    {
                        Movie = s.Movie!.Title,
                        ShowTime = s.StartTime,
                        Location = s.TheaterScreen!.Location,
                        Screen = s.TheaterScreen!.ScreenName,
                        Capacity = s.Capacity
                    })
                    .ToList();

                gridMovieShowtimes.DataSource = filtered;
            }
        }

        private void EmployeeForm_Load(object sender, EventArgs e)
        {
            using (var context = new AppDbContext())
            {
                int? locationId = GetCurrentEmployeeLocationId();

                if (locationId == null)
                {
                    MessageBox.Show("Could not find your assigned location.");
                    return;
                }

                var showtimes = context.Showtime
                    .Include(s => s.Movie)
                    .Include(s => s.TheaterScreen)
                    .ThenInclude(ts => ts.Location)
                    .Where(s => s.TheaterScreen!.LocationId == locationId.Value)
                    .Select(s => new
                    {
                        Movie = s.Movie!.Title,
                        ShowTime = s.StartTime,
                        Location = s.TheaterScreen!.GetLocation,
                        Screen = s.TheaterScreen!.ScreenName,
                        Capacity = s.Capacity
                    })
                    .ToList();

                gridMovieShowtimes.DataSource = showtimes;
                gridMovieShowtimes.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                gridMovieShowtimes.Columns["Movie"].FillWeight = 30;
                gridMovieShowtimes.Columns["ShowTime"].FillWeight = 20;
                gridMovieShowtimes.Columns["Location"].FillWeight = 20;
                gridMovieShowtimes.Columns["Screen"].FillWeight = 15;
                gridMovieShowtimes.Columns["Capacity"].FillWeight = 15;
            }
        }

        private void btnShowAll_Click(object sender, EventArgs e)
        {
            ShowAll();
        }

        private void btnSell_Click(object sender, EventArgs e)
        {
            string email = txtEmail.Text;

            if (!IsValidEmail(email))
            {
                MessageBox.Show("Enter a valid email.");
                return;
            }

            if ((DateTime)gridMovieShowtimes.CurrentRow.Cells["ShowTime"].Value < DateTime.Now.AddMinutes(-30))
            {
                MessageBox.Show("Please select a showtime that did not start more than 30 minutes ago");
                return;
            }

            if (gridMovieShowtimes.CurrentRow == null)
                return;

            int ticketsToSell = (int)numQuantity.Value;

            if (ticketsToSell <= 0)
            {
                MessageBox.Show("Select at least 1 ticket.");
                return;
            }

            var movieTitle = gridMovieShowtimes.CurrentRow.Cells["Movie"].Value.ToString();
            var startTime = (DateTime)gridMovieShowtimes.CurrentRow.Cells["ShowTime"].Value;
            var location = gridMovieShowtimes.CurrentRow.Cells["Location"].Value.ToString();
            var screen = gridMovieShowtimes.CurrentRow.Cells["Screen"].Value.ToString();

            using (var context = new AppDbContext())
            {
                var showtime = context.Showtime
                    .Include(s => s.Movie)
                    .Include(s => s.TheaterScreen)
                    .FirstOrDefault(s =>
                        s.Movie!.Title == movieTitle &&
                        s.StartTime == startTime);

                if (showtime == null)
                    return;

                if (showtime.SellTicket(ticketsToSell))
                {
                    context.SaveChanges();

                    var random = new Random();
                    List<string> ticketCodes = new List<string>();

                    decimal price = 0.00m;

                    for (int i = 0; i < ticketsToSell; i++)
                    {
                        string code = random.Next(100000, 999999).ToString();
                        ticketCodes.Add(code);

                        var ticket = new Ticket
                        {
                            TicketCode = int.Parse(code),
                            ShowtimeId = showtime.ShowtimeId,
                            UserAtSale = Session.UserId,
                            TimeOfSale = DateTime.Now
                        };

                        context.Ticket.Add(ticket);
                        price = price + showtime.PricePerTicket;
                    }

                    context.SaveChanges();

                    try
                    {
                        SendEmail(email, ticketCodes, ticketsToSell, movieTitle, startTime, location, screen, price);
                        MessageBox.Show($"{ticketsToSell} ticket(s) sold and email sent!");
                        txtEmail.Text = "";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ticket sold but email failed: " + ex.Message);
                    }

                    EmployeeForm_Load(null, null);
                }
                else
                {
                    MessageBox.Show("No tickets available :(");
                }
            }
        }

        private void ShowAll()
        {
            using (var context = new AppDbContext())
            {
                int? locationId = GetCurrentEmployeeLocationId();
                var showtimes = context.Showtime
                    .Where(s => s.TheaterScreen!.LocationId == locationId)
                    .Select(s => new
                    {
                        Movie = s.Movie!.Title,
                        ShowTime = s.StartTime,
                        Location = s.TheaterScreen!.Location,
                        Screen = s.TheaterScreen!.ScreenName,
                        Capacity = s.Capacity
                    })
                    .ToList();

                gridMovieShowtimes.DataSource = showtimes;
            }
        }

        private void SendEmail(string toEmail, List<string> codes, int ticketCount, string movie, DateTime showTime, string location, string screen, decimal price)
        {
            var fromAddress = new MailAddress("scrumflix@gmail.com");
            var toAddress = new MailAddress(toEmail);

            const string fromPassword = "ftdcmrxicxrjincg";

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
            };

            string codeList = string.Join("\n", codes.Select(c => $"- {c}"));

            var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = "ScrumFlix Theaters Ticket Codes",
                Body =
                    $"Tickets sold: {ticketCount}" +
                    $"\n\nTicket codes:\n{codeList}" +
                    $"\n\nTotal price: {price}" +
                    $"\n\nMovie: {movie}" +
                    $"\nShowtime: {showTime}" +
                    $"\nLocation: {location}" +
                    $"\nScreen: {screen}"
            };

            smtp.Send(message);
        }

        private void EmployeeForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            string search = txtSearch.Text.ToLower();

            using (var context = new AppDbContext())
            {
                int? locationId = GetCurrentEmployeeLocationId();
                var filtered = context.Showtime
                    .Where(s =>
                        s.TheaterScreen!.LocationId == locationId &&
                        s.Movie!.Title.ToLower().Contains(search))
                    .Select(s => new
                    {
                        Movie = s.Movie!.Title,
                        ShowTime = s.StartTime,
                        Location = s.TheaterScreen!.Location,
                        Screen = s.TheaterScreen!.ScreenName,
                        Capacity = s.Capacity
                    })
                    .ToList();

                gridMovieShowtimes.DataSource = filtered;
            }
        }

        private void EmployeeForm_FormClosing(object sender, FormClosingEventArgs e)
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
        private int? GetCurrentEmployeeLocationId()
        {
            using var context = new AppDbContext();

            return context.Users
                .Include(u => u.Employee)
                .Where(u => u.UserId == Session.UserId)
                .Select(u => (int?)u.Employee!.LocationId)
                .FirstOrDefault();
        }
    }
}
