// This form opens a window that allows the user to CRUD screens from the database

using ScrumFlix.Data;
using ScrumFlix.Models;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;

namespace ScrumFlix.Forms
{
    public partial class TheaterScreenForm : Form
    {
        public TheaterScreenForm()
        {
            InitializeComponent();
        }

        private async void TheaterScreenForm_Load(object sender, EventArgs e) // When form loads it loads the data from the db
        {
            await LoadTheaterScreenAsync();
        }

        private async Task LoadTheaterScreenAsync() // Loads data from database and shows only the screen ID and the screenName/locationName
        {
            using var db = new AppDbContext();
            var screens = await db.TheaterScreen
                .Include(s => s.Location)
                .OrderBy(s => s.LocationId)
                .ToListAsync();

            gridScreens.AutoGenerateColumns = true;
            gridScreens.DataSource = screens;

            gridScreens.Columns["LocationId"].Visible = false;
            gridScreens.Columns["ScreenName"].Visible = false;
            gridScreens.Columns["Location"].Visible = false;

            gridScreens.Columns["ScreenDisplay"].HeaderText = "Screen Name";
            gridScreens.Columns["ScreenDisplay"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        private async void btnAdd_Click(object sender, EventArgs e) // When the add button is clicked it opens the TheaterScreenEditForm.cs form to allow the user to add a screen to the db
        {
            using var form = new TheaterScreenEditForm();

            if (form.ShowDialog() != DialogResult.OK)
                return;

            using var db = new AppDbContext();

            db.TheaterScreen.Add(form.TheaterScreen);
            await db.SaveChangesAsync();

            // Audit Log
            db.AuditLog.Add(new AuditLog
            {
                UserId = Session.UserId,
                ActionType = "ADD_THEATER_SCREEN",
                TableName = "TheaterScreen",
                ObjectId = form.TheaterScreen.TheaterScreenId,
                ActionTime = DateTime.Now,
                Description = $"Added theater screen '{form.TheaterScreen.ScreenName}'",
                OldValues = null,
                NewValues = $"ScreenName={form.TheaterScreen.ScreenName}, LocationId={form.TheaterScreen.LocationId}, Capacity={form.TheaterScreen.Capacity}, IsActive={form.TheaterScreen.IsActive}"
            });

            await db.SaveChangesAsync();

            await LoadTheaterScreenAsync();
        }
        private TheaterScreen? SelectedScreen() // When selecting an item in the dataviewgrid this function returns that object
        {
            return gridScreens.CurrentRow?.DataBoundItem as TheaterScreen;
        }

        private async void btnEdit_Click(object sender, EventArgs e) // When the edit button is clicked it pulls the info from the selected item and opens a TheaterScreenEditForm.cs form to allow the user to edit the screens info
        {
            var selected = SelectedScreen();
            if (selected is null)
            {
                MessageBox.Show("Select a screen first please.");
                return;
            }

            using var form = new TheaterScreenEditForm(selected);
            if (form.ShowDialog() != DialogResult.OK)
                return;

            using var db = new AppDbContext();
            var screen = await db.TheaterScreen.FindAsync(selected.TheaterScreenId);
            if (screen is null) return;

            var oldScreenName = screen.ScreenName;
            var oldLocationId = screen.LocationId;
            var oldCapacity = screen.Capacity;
            var oldActive = screen.IsActive;

            screen.ScreenName = form.TheaterScreen.ScreenName;
            screen.LocationId = form.TheaterScreen.LocationId;
            screen.Capacity = form.TheaterScreen.Capacity;
            screen.IsActive = form.TheaterScreen.IsActive;

            // Audit Log
            db.AuditLog.Add(new AuditLog
            {
                UserId = Session.UserId,
                ActionType = "UPDATE_THEATER_SCREEN",
                TableName = "TheaterScreen",
                ObjectId = screen.TheaterScreenId,
                ActionTime = DateTime.Now,
                Description = $"Updated theater screen '{oldScreenName}'",
                OldValues = $"ScreenName={oldScreenName}, LocationId={oldLocationId}, Capacity={oldCapacity}, IsActive={oldActive}",
                NewValues = $"ScreenName={screen.ScreenName}, LocationId={screen.LocationId}, Capacity={screen.Capacity}, IsActive={screen.IsActive}"
            });

            await db.SaveChangesAsync();
            await LoadTheaterScreenAsync();
        }

        private async void btnDelete_Click(object sender, EventArgs e) // When the delete button is clicked it checks to make sure there are no showtimes attached to the screen before removing it from the db
        {
            var selected = SelectedScreen();
            if (selected is null)
            {
                MessageBox.Show("Select a screen first please.");
                return;
            }

            using var db = new AppDbContext();
            // If showtime exists won't delete screen
            bool hasShowtimes = await db.Showtime.AnyAsync(s => s.TheaterScreenId == selected.TheaterScreenId);
            if (hasShowtimes)
            {
                MessageBox.Show("Delete screen's showtimes before attempting to delete the screen.");
                return;
            }

            var confirm = MessageBox.Show(
                $"Delete '{selected.ScreenName}'?",
                "Confirm delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes)
                return;

            var screen = await db.TheaterScreen.FindAsync(selected.TheaterScreenId);
            if (screen is null) return;

            // Audit Log
            db.AuditLog.Add(new AuditLog
            {
                UserId = Session.UserId,
                ActionType = "DELETE_THEATER_SCREEN",
                TableName = "TheaterScreen",
                ObjectId = screen.TheaterScreenId,
                ActionTime = DateTime.Now,
                Description = $"Deleted theater screen '{screen.ScreenName}'",
                OldValues = $"ScreenName={screen.ScreenName}, LocationId={screen.LocationId}, Capacity={screen.Capacity}, IsActive={screen.IsActive}",
                NewValues = null
            });

            db.TheaterScreen.Remove(screen);
            await db.SaveChangesAsync();

            await LoadTheaterScreenAsync();
        }

        private async void btnRefresh_Click(object sender, EventArgs e) // When the refresh button is clicked it refreshes the data in the gridview (in case changes are made to the database while this form is still open on the admin computer)
        {
            await LoadTheaterScreenAsync();
        }
    }
}