// This form opens a window that allows the user to CRUD locations from the database

using Microsoft.EntityFrameworkCore;
using ScrumFlix.Data;
using ScrumFlix.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScrumFlix.Forms
{
    public partial class LocationForm : Form
    {
        public LocationForm()
        {
            InitializeComponent();
            gridLocations.CellClick += gridLocations_CellClick;
        }

        private async Task LoadLocationAsync() // Loads data from database
        {
            using var db = new AppDbContext();
            var locations = await db.Location
                .OrderBy(s => s.LocationId)
                .ToListAsync();

            gridLocations.AutoGenerateColumns = true;
            gridLocations.DataSource = locations;

            gridLocations.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        private async void LocationForm_Load(object sender, EventArgs e) // When form loads it loads the data from the db
        {
            await LoadLocationAsync();
        }

        private Location? SelectedLocation() // When selecting an item in the dataviewgrid this function returns that object
        {
            return gridLocations.CurrentRow?.DataBoundItem as Location;
        }

        private void ClearFields() // Clears textbox inputs after actions
        {
            txtName.Text = "";
            txtAddress.Text = "";
            chkIsActive.Checked = true;
        }

        private void gridLocations_CellClick(object? sender, DataGridViewCellEventArgs e) // When a row is clicked it fills the textboxes with that locations data
        {
            var selected = SelectedLocation();
            if (selected == null) return;

            txtName.Text = selected.LocationName;
            txtAddress.Text = selected.LocationAddress ?? "";
            chkIsActive.Checked = selected.IsActive;
        }

        private async void btnAdd_Click(object sender, EventArgs e) // When the add button is clicked it adds a new location using textbox values
        {
            string name = txtName.Text.Trim();
            string address = txtAddress.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Location name is required.");
                return;
            }

            using var db = new AppDbContext();

            var location = new Location
            {
                LocationName = name,
                LocationAddress = string.IsNullOrWhiteSpace(address) ? null : address,
                IsActive = chkIsActive.Checked
            };

            db.Location.Add(location);
            await db.SaveChangesAsync();

            // Audit Log
            db.AuditLog.Add(new AuditLog
            {
                UserId = Session.UserId,
                ActionType = "ADD_LOCATION",
                TableName = "Location",
                ObjectId = location.LocationId,
                ActionTime = DateTime.Now,
                Description = $"Added location '{location.LocationName}'",
                OldValues = null,
                NewValues = $"LocationName={location.LocationName}, LocationAddress={location.LocationAddress}, IsActive={location.IsActive}"
            });

            await db.SaveChangesAsync();

            ClearFields();
            await LoadLocationAsync();
        }

        private async void btnUpdate_Click(object sender, EventArgs e) // When the update button is clicked it updates the selected locations name/address/active status
        {
            var selected = SelectedLocation();
            if (selected is null)
            {
                MessageBox.Show("Select a location first please.");
                return;
            }

            string name = txtName.Text.Trim();
            string address = txtAddress.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Location name is required.");
                return;
            }

            using var db = new AppDbContext();

            var location = await db.Location.FindAsync(selected.LocationId);
            if (location == null) return;

            var oldName = location.LocationName;
            var oldAddress = location.LocationAddress;
            var oldActive = location.IsActive;

            location.LocationName = name;
            location.LocationAddress = string.IsNullOrWhiteSpace(address) ? null : address;
            location.IsActive = chkIsActive.Checked;

            // Audit Log
            db.AuditLog.Add(new AuditLog
            {
                UserId = Session.UserId,
                ActionType = "UPDATE_LOCATION",
                TableName = "Location",
                ObjectId = location.LocationId,
                ActionTime = DateTime.Now,
                Description = $"Updated location '{oldName}'",
                OldValues = $"LocationName={oldName}, LocationAddress={oldAddress}, IsActive={oldActive}",
                NewValues = $"LocationName={location.LocationName}, LocationAddress={location.LocationAddress}, IsActive={location.IsActive}"
            });

            await db.SaveChangesAsync();

            ClearFields();
            await LoadLocationAsync();
        }

        private async void btnDelete_Click(object sender, EventArgs e) // When the delete button is clicked it checks to make sure there are no screens attached before removing the location
        {
            var selected = SelectedLocation();
            if (selected is null)
            {
                MessageBox.Show("Select a location first please");
                return;
            }

            using var db = new AppDbContext();

            // If location has screens won't delete the location
            bool hasScreens = await db.TheaterScreen.AnyAsync(s => s.LocationId == selected.LocationId);
            if (hasScreens)
            {
                MessageBox.Show("Delete the locations screens before attempting to delete the location.");
                return;
            }

            var confirm = MessageBox.Show(
                $"Delete '{selected.LocationName}'?",
                "Confirm delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes)
                return;

            var location = await db.Location.FindAsync(selected.LocationId);
            if (location == null) return;

            // Audit Log
            db.AuditLog.Add(new AuditLog
            {
                UserId = Session.UserId,
                ActionType = "DELETE_LOCATION",
                TableName = "Location",
                ObjectId = location.LocationId,
                ActionTime = DateTime.Now,
                Description = $"Deleted location '{location.LocationName}'",
                OldValues = $"LocationName={location.LocationName}, LocationAddress={location.LocationAddress}, IsActive={location.IsActive}",
                NewValues = null
            });

            db.Location.Remove(location);
            await db.SaveChangesAsync();

            ClearFields();
            await LoadLocationAsync();
        }

        private async void btnRefresh_Click(object sender, EventArgs e) // When the refresh button is clicked it refreshes the data in the gridview
        {
            ClearFields();
            await LoadLocationAsync();
        }
    }
}