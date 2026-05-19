// This form is the edit form that opens when someone adds or edits a theater screen in the TheaterScreenForm.cs form

using ScrumFlix.Data;
using ScrumFlix.Models;

namespace ScrumFlix.Forms
{
    public partial class TheaterScreenEditForm : Form
    {
        private void LoadData()
        {
            using var db = new AppDbContext();

            var Location = db.Location
                .OrderBy(s => s.LocationId)
                .ToList();

            screenLocation.DisplayMember = "LocationName";
            screenLocation.ValueMember = "LocationId";
            screenLocation.DataSource = Location;
        }
        public TheaterScreen TheaterScreen { get; private set; } = new TheaterScreen();
        public TheaterScreenEditForm()
        {
            InitializeComponent();
            LoadData();

            chkIsActive.Checked = true;
        }
        public TheaterScreenEditForm(TheaterScreen existing) : this() // Constructor used when editing an existing screen, copies existing screen data into the forms fields
        {
            TheaterScreen = new TheaterScreen
            {
                TheaterScreenId = existing.TheaterScreenId,
                ScreenName = existing.ScreenName,
                LocationId = existing.LocationId,
                Capacity = existing.Capacity,
                IsActive = existing.IsActive
            };

            screenName.Text = TheaterScreen.ScreenName;
            screenLocation.SelectedValue = TheaterScreen.LocationId;
            numCapacity.Value = TheaterScreen.Capacity;
            chkIsActive.Checked = TheaterScreen.IsActive;
        }

        private void btnOk_Click(object sender, EventArgs e) // Validates the form input and updates the TheaterScreen object before returning to the parent form
        {
            string name = screenName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Theater Screen name is required.");
                DialogResult = DialogResult.None;
                return;
            }
            TheaterScreen.ScreenName = name;

            TheaterScreen.Capacity = (int)numCapacity.Value;

            TheaterScreen.LocationId = Convert.ToInt32(screenLocation.SelectedValue);

            TheaterScreen.IsActive = chkIsActive.Checked;
        }
    }
}