// This form opens a window that allows the user to CRUD showtimes from the database

using ScrumFlix.Data;
using ScrumFlix.Models;
using System;
using System.Linq;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;

namespace ScrumFlix.Forms
{
    public partial class ShowtimesForm : Form
    {
        public ShowtimesForm()
        {
            InitializeComponent();

            showtimesGrid.CellClick += showtimesGrid_CellClick;
            chkIsActive.CheckedChanged += chkIsActive_CheckedChanged;

            LoadData();
            LoadShowtimes();
        }
        private void LoadData() // Loads the data for the screenCombo box and the movieCombo box
        {
            using var db = new AppDbContext();

            var screens = db.TheaterScreen
                .Include(s => s.Location)
                .OrderBy(s => s.TheaterScreenId)
                .ToList();

            screenCombo.DisplayMember = "ScreenDisplay";
            screenCombo.ValueMember = "TheaterScreenId";
            screenCombo.DataSource = screens;

            var movies = db.Movies
                .OrderBy(m => m.Title)
                .ToList();

            movieCombo.DisplayMember = "Title";
            movieCombo.ValueMember = "MovieId";
            movieCombo.DataSource = movies;
        }

        private void screenCombo_SelectedIndexChanged(object sender, EventArgs e) // Triggered whenever the selected screen changes
        {
            LoadShowtimes();
        }
        private void LoadShowtimes()  // Loads showtimes for the currently selected screen and displays them in the grid
        {
            if (screenCombo.SelectedValue is null)
                return;

            int screenId = (int)screenCombo.SelectedValue;

            using var db = new AppDbContext();

            var rows = db.Showtime
                .Include(s => s.Movie)
                .Where(s => s.TheaterScreenId == screenId)
                .Select(s => new
                {
                    s.ShowtimeId,
                    MovieTitle = s.Movie!.Title,
                    StartTime = s.StartTime,
                    EndTime = s.StartTime.AddMinutes(s.Movie!.RuntimeMinutes),
                    s.Capacity,
                    s.PricePerTicket,
                    s.IsActive
                })
                .OrderBy(r => r.StartTime)
                .ToList();

            showtimesGrid.DataSource = rows;

            if (showtimesGrid.Columns["ShowtimeId"] != null)
                showtimesGrid.Columns["ShowtimeId"].Visible = false;
        }

        private void showtimesGrid_CellClick(object sender, DataGridViewCellEventArgs e) // When a showtime row is clicked it fills the price textbox and active checkbox
        {
            if (showtimesGrid.CurrentRow == null)
                return;

            if (showtimesGrid.CurrentRow.Cells["PricePerTicket"].Value != null)
                txtPricePerTicket.Text = showtimesGrid.CurrentRow.Cells["PricePerTicket"].Value.ToString();

            if (showtimesGrid.CurrentRow.Cells["IsActive"].Value != null)
                chkIsActive.Checked = Convert.ToBoolean(showtimesGrid.CurrentRow.Cells["IsActive"].Value);
        }

        private void AddButton_Click(object sender, EventArgs e) // Adds a new showtime for the selected movie and screen, also makes sure the new showtime doesn't overlap with previous showtimes
        {
            if (screenCombo.SelectedValue is null || movieCombo.SelectedValue is null)
            {
                MessageBox.Show("Please select a screen and a movie");
                return;
            }

            if (!decimal.TryParse(txtPricePerTicket.Text.Trim(), out decimal pricePerTicket) || pricePerTicket < 0)
            {
                MessageBox.Show("Please enter a valid ticket price.");
                return;
            }

            int screenId = (int)screenCombo.SelectedValue;
            int movieId = (int)movieCombo.SelectedValue;
            DateTime startTime = startTimePicker.Value;

            using var db = new AppDbContext();

            var screen = db.TheaterScreen
                .FirstOrDefault(s => s.TheaterScreenId == screenId);

            if (screen == null)
            {
                MessageBox.Show("Selected screen was not found.");
                return;
            }

            var movie = db.Movies.First(m => m.MovieId == movieId);
            int runtime = movie.RuntimeMinutes;

            DateTime newStart = startTime;
            DateTime newEnd = startTime.AddMinutes(runtime);

            var existing = db.Showtime
                .Include(s => s.Movie)
                .Where(s => s.TheaterScreenId == screenId)
                .ToList();
            bool overlaps = existing.Any(s =>
            {
                DateTime existingStart = s.StartTime;
                DateTime existingEnd = s.StartTime.AddMinutes(s.Movie!.RuntimeMinutes);

                return newStart < existingEnd && existingStart < newEnd;
            });

            if (overlaps)
            {
                MessageBox.Show("That showtime overlaps an existing showtime in this screen, please choose another showtime.");
                return;
            }

            var showtime = new Showtime
            {
                TheaterScreenId = screenId,
                MovieId = movieId,
                StartTime = startTime,
                Capacity = screen.Capacity,
                PricePerTicket = pricePerTicket,
                IsActive = chkIsActive.Checked
            };

            db.Showtime.Add(showtime);
            db.SaveChanges();

            // Audit Log
            db.AuditLog.Add(new AuditLog
            {
                UserId = Session.UserId,
                ActionType = "ADD_SHOWTIME",
                TableName = "Showtime",
                ObjectId = showtime.ShowtimeId,
                ActionTime = DateTime.Now,
                Description = $"Added showtime for '{movie.Title}'",
                OldValues = null,
                NewValues = $"MovieId={showtime.MovieId}, TheaterScreenId={showtime.TheaterScreenId}, StartTime={showtime.StartTime}, Capacity={showtime.Capacity}, PricePerTicket={showtime.PricePerTicket}, IsActive={showtime.IsActive}"
            });

            db.SaveChanges();

            LoadShowtimes();
        }

        private void chkIsActive_CheckedChanged(object sender, EventArgs e) // When the active checkbox is changed it updates the selected showtimes active status
        {
            if (showtimesGrid.CurrentRow == null)
                return;

            var cellValue = showtimesGrid.CurrentRow.Cells["ShowtimeId"].Value;
            if (cellValue == null)
                return;

            int showtimeId = (int)cellValue;

            using var db = new AppDbContext();

            var showtime = db.Showtime
                .Include(s => s.Movie)
                .FirstOrDefault(s => s.ShowtimeId == showtimeId);

            if (showtime == null)
                return;

            bool oldActive = showtime.IsActive;

            if (oldActive == chkIsActive.Checked)
                return;

            showtime.IsActive = chkIsActive.Checked;

            // Audit Log
            db.AuditLog.Add(new AuditLog
            {
                UserId = Session.UserId,
                ActionType = "UPDATE_SHOWTIME_ACTIVE_STATUS",
                TableName = "Showtime",
                ObjectId = showtime.ShowtimeId,
                ActionTime = DateTime.Now,
                Description = $"Changed active status for showtime '{showtime.Movie?.Title}'",
                OldValues = $"IsActive={oldActive}",
                NewValues = $"IsActive={showtime.IsActive}"
            });

            db.SaveChanges();

            LoadShowtimes();
        }

        private void btnUpdatePrice_Click(object sender, EventArgs e) // Updates the selected showtimes ticket price
        {
            if (showtimesGrid.CurrentRow == null)
            {
                MessageBox.Show("Choose a showtime to update.");
                return;
            }

            if (!decimal.TryParse(txtPricePerTicket.Text.Trim(), out decimal newPrice) || newPrice < 0)
            {
                MessageBox.Show("Please enter a valid ticket price.");
                return;
            }

            var cellValue = showtimesGrid.CurrentRow.Cells["ShowtimeId"].Value;
            if (cellValue == null)
            {
                MessageBox.Show("Could not determine the selected showtime.");
                return;
            }

            int showtimeId = (int)cellValue;

            using var db = new AppDbContext();

            var showtime = db.Showtime
                .Include(s => s.Movie)
                .FirstOrDefault(s => s.ShowtimeId == showtimeId);

            if (showtime == null)
            {
                MessageBox.Show("That showtime no longer exists.");
                LoadShowtimes();
                return;
            }

            var oldPrice = showtime.PricePerTicket;
            var oldActive = showtime.IsActive;

            showtime.PricePerTicket = newPrice;
            showtime.IsActive = chkIsActive.Checked;

            // Audit Log
            db.AuditLog.Add(new AuditLog
            {
                UserId = Session.UserId,
                ActionType = "UPDATE_SHOWTIME",
                TableName = "Showtime",
                ObjectId = showtime.ShowtimeId,
                ActionTime = DateTime.Now,
                Description = $"Updated showtime for '{showtime.Movie?.Title}'",
                OldValues = $"PricePerTicket={oldPrice}, IsActive={oldActive}",
                NewValues = $"PricePerTicket={showtime.PricePerTicket}, IsActive={showtime.IsActive}"
            });

            db.SaveChanges();

            LoadShowtimes();
        }

        private void deleteButton_Click(object sender, EventArgs e) // Deletes the selected showtime from the db
        {
            if (showtimesGrid.CurrentRow == null)
            {
                MessageBox.Show("Choose a showtime to delete.");
                return;
            }

            var cellValue = showtimesGrid.CurrentRow.Cells["ShowtimeId"].Value;
            if (cellValue == null)
            {
                MessageBox.Show("Could not determine the selected showtime.");
                return;
            }

            int showtimeId = (int)cellValue;

            var confirm = MessageBox.Show(
                "Delete the selected showtime?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes)
                return;

            using var db = new AppDbContext();

            var showtime = db.Showtime
                .Include(s => s.Movie)
                .FirstOrDefault(s => s.ShowtimeId == showtimeId);

            if (showtime == null)
            {
                MessageBox.Show("That showtime no longer exists.");
                LoadShowtimes();
                return;
            }

            // Audit Log
            db.AuditLog.Add(new AuditLog
            {
                UserId = Session.UserId,
                ActionType = "DELETE_SHOWTIME",
                TableName = "Showtime",
                ObjectId = showtime.ShowtimeId,
                ActionTime = DateTime.Now,
                Description = $"Deleted showtime for '{showtime.Movie?.Title}'",
                OldValues = $"MovieId={showtime.MovieId}, TheaterScreenId={showtime.TheaterScreenId}, StartTime={showtime.StartTime}, Capacity={showtime.Capacity}, PricePerTicket={showtime.PricePerTicket}, IsActive={showtime.IsActive}",
                NewValues = null
            });

            db.Showtime.Remove(showtime);
            db.SaveChanges();

            LoadShowtimes();
        }
    }
}