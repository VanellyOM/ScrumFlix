using Microsoft.EntityFrameworkCore;
using ScrumFlix.Data;
using ScrumFlix.Models;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ScrumFlix.Forms
{
    public partial class ScheduleManagementForm : Form
    {
        private class ShowtimeComboItem
{
    public int? ShowtimeId { get; set; }
    public string Display { get; set; } = "";
}
        public ScheduleManagementForm()
        {
            InitializeComponent();
        }
        private void comboLocation_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadShiftsGrid();
        }

        private void ScheduleManagementForm_Load(object sender, EventArgs e)
        {
            panelSchedule.AutoScroll = true;

            comboMonth.Items.Clear();
            comboMonth.Items.Add("May 2026");
            comboMonth.SelectedIndex = 0;

            dateTimeStartShift.Format = DateTimePickerFormat.Custom;
            dateTimeStartShift.CustomFormat = "MM/dd/yyyy hh:mm tt";

            dateTimeEndShift.Format = DateTimePickerFormat.Custom;
            dateTimeEndShift.CustomFormat = "MM/dd/yyyy hh:mm tt";

            LoadCombos();
            LoadShiftsGrid();
            LoadSelectedMonthSchedule();
            LoadAssignmentCombos();
            LoadScheduleAssignmentsGrid();
        }

        private void LoadCombos()
        {
            using var db = new AppDbContext();

            comboRole.DataSource = db.Roles
                .OrderBy(r => r.RoleName)
                .ToList();

            comboRole.DisplayMember = "RoleName";
            comboRole.ValueMember = "RoleId";

            comboLocation.DataSource = db.Location
                .Where(l => l.IsActive)
                .OrderBy(l => l.LocationName)
                .ToList();

            comboLocation.DisplayMember = "LocationName";
            comboLocation.ValueMember = "LocationId";

            comboShiftLocation.DataSource = null;
            comboShiftLocation.DisplayMember = "LocationName";
            comboShiftLocation.ValueMember = "LocationId";
            comboShiftLocation.DataSource = db.Location
                .Where(l => l.IsActive)
                .OrderBy(l => l.LocationName)
                .ToList();
        }

        private void LoadShiftsGrid()
        {
            using var db = new AppDbContext();

            int? selectedLocationId = null;

            if (comboLocation.SelectedValue is int locationId)
            {
                selectedLocationId = locationId;
            }
            else if (comboLocation.SelectedItem is Location location)
            {
                selectedLocationId = location.LocationId;
            }

            var query = db.Shifts
                .Include(s => s.Role)
                .Include(s => s.Location)
                .AsQueryable();

            if (selectedLocationId != null)
            {
                query = query.Where(s => s.LocationId == selectedLocationId.Value);
            }

            var shifts = query
                .OrderBy(s => s.Location!.LocationName)
                .ThenBy(s => s.RoleId)
                .ThenBy(s => s.StartTime)
                .Select(s => new
                {
                    s.ShiftId,
                    Location = s.Location!.LocationName,
                    Role = s.Role!.RoleName,
                    s.StartTime,
                    s.EndTime
                })
                .ToList();

            gridShifts.DataSource = shifts;

            if (gridShifts.Columns.Count > 0)
            {
                gridShifts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            }

            if (gridShifts.Columns["ShiftId"] != null)
            {
                gridShifts.Columns["ShiftId"].Visible = false;
            }
        }

        private int? GetSelectedShiftId()
        {
            if (gridShifts.CurrentRow == null)
                return null;

            var value = gridShifts.CurrentRow.Cells["ShiftId"].Value;

            if (value == null)
                return null;

            if (int.TryParse(value.ToString(), out int shiftId))
                return shiftId;

            return null;
        }

        private void gridShifts_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            var shiftId = GetSelectedShiftId();

            if (shiftId == null)
                return;

            using var db = new AppDbContext();

            var shift = db.Shifts.FirstOrDefault(s => s.ShiftId == shiftId.Value);

            if (shift == null)
                return;

            dateTimeStartShift.Value = shift.StartTime;
            dateTimeEndShift.Value = shift.EndTime;
            comboRole.SelectedValue = shift.RoleId;
            comboLocation.SelectedValue = shift.LocationId;
        }

        private void btnLoadSchedule_Click(object sender, EventArgs e)
        {
            LoadSelectedMonthSchedule();
        }

        private void LoadScheduleVisual(int year, int month)
        {
            panelSchedule.Controls.Clear();

            if (comboShiftLocation.SelectedValue == null)
                return;

            int selectedLocationId = Convert.ToInt32(comboShiftLocation.SelectedValue);

            using var db = new AppDbContext();

            var startMonth = new DateTime(year, month, 1);
            var endMonth = startMonth.AddMonths(1);

            var shifts = db.Shifts
                .Include(s => s.Role)
                .Include(s => s.Location)
                .Where(s =>
                    s.LocationId == selectedLocationId &&
                    s.StartTime >= startMonth &&
                    s.StartTime < endMonth)
                .OrderBy(s => s.StartTime)
                .ToList();

            int y = 10;

            int timelineLeft = 170;
            int hourWidth = 70;
            int rowHeight = 36;
            int headerHeight = 25;

            int startHour = 8;
            int endHour = 24;
            int totalHours = endHour - startHour;
            int timelineWidth = totalHours * hourWidth;

            foreach (var dayGroup in shifts.GroupBy(s => s.StartTime.Date))
            {
                Label dayLabel = new Label
                {
                    Text = dayGroup.Key.ToString("dddd, MMMM d, yyyy"),
                    Left = 10,
                    Top = y,
                    Width = 700,
                    Height = 30,
                    Font = new Font("Segoe UI", 12, FontStyle.Bold)
                };

                panelSchedule.Controls.Add(dayLabel);
                y += 35;

                // Hour labels
                for (int hour = startHour; hour <= endHour; hour += 2)
                {
                    int left = timelineLeft + ((hour - startHour) * hourWidth);

                    Label hourLabel = new Label
                    {
                        Text = FormatHour(hour),
                        Left = left - 10,
                        Top = y,
                        Width = 60,
                        Height = 20,
                        TextAlign = ContentAlignment.MiddleLeft
                    };

                    panelSchedule.Controls.Add(hourLabel);
                }

                y += headerHeight;

                var dayShifts = dayGroup
                    .OrderBy(s => s.Role!.RoleName)
                    .ThenBy(s => s.StartTime)
                    .ToList();

                int gridTop = y;
                int gridHeight = dayShifts.Count * rowHeight;

                // Vertical grid lines every hour
                for (int hour = startHour; hour <= endHour; hour++)
                {
                    int left = timelineLeft + ((hour - startHour) * hourWidth);

                    Panel line = new Panel
                    {
                        Left = left,
                        Top = gridTop,
                        Width = 1,
                        Height = gridHeight,
                        BackColor = Color.LightGray
                    };

                    panelSchedule.Controls.Add(line);
                    line.SendToBack();
                }

                // Horizontal row lines
                for (int i = 0; i <= dayShifts.Count; i++)
                {
                    Panel line = new Panel
                    {
                        Left = timelineLeft,
                        Top = gridTop + (i * rowHeight),
                        Width = timelineWidth,
                        Height = 1,
                        BackColor = Color.Gainsboro
                    };

                    panelSchedule.Controls.Add(line);
                    line.SendToBack();
                }

                foreach (var shift in dayShifts)
                {
                    Label roleLabel = new Label
                    {
                        Text = shift.Role?.RoleName ?? "",
                        Left = 10,
                        Top = y,
                        Width = 150,
                        Height = 30,
                        TextAlign = ContentAlignment.MiddleLeft
                    };

                    DateTime dayStart = shift.StartTime.Date.AddHours(startHour);

                    int left = timelineLeft + (int)((shift.StartTime - dayStart).TotalHours * hourWidth);
                    int width = Math.Max(60, (int)((shift.EndTime - shift.StartTime).TotalHours * hourWidth));

                    Label shiftBar = new Label
                    {
                        Text = $"{shift.StartTime:t} - {shift.EndTime:t}",
                        Left = left,
                        Top = y + 3,
                        Width = width,
                        Height = 28,
                        BackColor = GetRoleColor(shift.Role?.RoleName),
                        BorderStyle = BorderStyle.FixedSingle,
                        TextAlign = ContentAlignment.MiddleCenter
                    };

                    panelSchedule.Controls.Add(roleLabel);
                    panelSchedule.Controls.Add(shiftBar);
                    shiftBar.BringToFront();

                    y += rowHeight;
                }

                y += 35;
            }

            if (!shifts.Any())
            {
                Label emptyLabel = new Label
                {
                    Text = "No shifts found for this location/month.",
                    Left = 10,
                    Top = 10,
                    Width = 400,
                    Height = 30
                };

                panelSchedule.Controls.Add(emptyLabel);
            }
        }
        private string FormatHour(int hour)
        {
            if (hour == 0 || hour == 24)
                return "12AM";

            if (hour == 12)
                return "12PM";

            if (hour < 12)
                return $"{hour}AM";

            return $"{hour - 12}PM";
        }

        private Color GetRoleColor(string? roleName)
        {
            return roleName switch
            {
                "Admin" => Color.LightCoral,
                "Manager" => Color.Khaki,
                "Employee" => Color.LightSkyBlue,
                _ => Color.LightGray
            };
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (comboRole.SelectedValue == null || comboLocation.SelectedValue == null)
            {
                MessageBox.Show("Select a role and location.");
                return;
            }

            DateTime start = dateTimeStartShift.Value;
            DateTime end = dateTimeEndShift.Value;

            if (end <= start)
            {
                MessageBox.Show("End time must be after start time.");
                return;
            }

            int roleId = Convert.ToInt32(comboRole.SelectedValue);
            int locationId = Convert.ToInt32(comboLocation.SelectedValue);

            using var db = new AppDbContext();


            var shift = new Shift
            {
                StartTime = start,
                EndTime = end,
                RoleId = roleId,
                LocationId = locationId
            };

            db.Shifts.Add(shift);
            db.SaveChanges();

            db.AuditLog.Add(new AuditLog
            {
                UserId = Session.UserId,
                ActionType = "ADD_SHIFT",
                TableName = "Shifts",
                ObjectId = shift.ShiftId,
                ActionTime = DateTime.Now,
                Description = "Added shift",
                OldValues = null,
                NewValues = $"StartTime={shift.StartTime}, EndTime={shift.EndTime}, RoleId={shift.RoleId}, LocationId={shift.LocationId}"
            });

            db.SaveChanges();

            LoadShiftsGrid();
            LoadSelectedMonthSchedule();
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            var shiftId = GetSelectedShiftId();

            if (shiftId == null)
            {
                MessageBox.Show("Select a shift to update.");
                return;
            }

            DateTime start = dateTimeStartShift.Value;
            DateTime end = dateTimeEndShift.Value;

            if (end <= start)
            {
                MessageBox.Show("End time must be after start time.");
                return;
            }

            int roleId = Convert.ToInt32(comboRole.SelectedValue);
            int locationId = Convert.ToInt32(comboLocation.SelectedValue);

            using var db = new AppDbContext();

            var shift = db.Shifts.FirstOrDefault(s => s.ShiftId == shiftId.Value);

            if (shift == null)
            {
                MessageBox.Show("Shift not found.");
                return;
            }


            var oldStart = shift.StartTime;
            var oldEnd = shift.EndTime;
            var oldRoleId = shift.RoleId;
            var oldLocationId = shift.LocationId;

            shift.StartTime = start;
            shift.EndTime = end;
            shift.RoleId = roleId;
            shift.LocationId = locationId;

            db.AuditLog.Add(new AuditLog
            {
                UserId = Session.UserId,
                ActionType = "UPDATE_SHIFT",
                TableName = "Shifts",
                ObjectId = shift.ShiftId,
                ActionTime = DateTime.Now,
                Description = "Updated shift",
                OldValues = $"StartTime={oldStart}, EndTime={oldEnd}, RoleId={oldRoleId}, LocationId={oldLocationId}",
                NewValues = $"StartTime={shift.StartTime}, EndTime={shift.EndTime}, RoleId={shift.RoleId}, LocationId={shift.LocationId}"
            });

            db.SaveChanges();

            LoadShiftsGrid();
            LoadSelectedMonthSchedule();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            var shiftId = GetSelectedShiftId();

            if (shiftId == null)
            {
                MessageBox.Show("Select a shift to delete.");
                return;
            }

            var confirm = MessageBox.Show(
                "Delete this shift?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes)
                return;

            using var db = new AppDbContext();

            var shift = db.Shifts.FirstOrDefault(s => s.ShiftId == shiftId.Value);

            if (shift == null)
            {
                MessageBox.Show("Shift not found.");
                return;
            }

            bool hasAssignments = db.ScheduleAssignments.Any(a => a.ShiftId == shift.ShiftId);

            if (hasAssignments)
            {
                MessageBox.Show("This shift has schedule assignments. Delete those assignments before deleting the shift.");
                return;
            }

            db.AuditLog.Add(new AuditLog
            {
                UserId = Session.UserId,
                ActionType = "DELETE_SHIFT",
                TableName = "Shifts",
                ObjectId = shift.ShiftId,
                ActionTime = DateTime.Now,
                Description = "Deleted shift",
                OldValues = $"StartTime={shift.StartTime}, EndTime={shift.EndTime}, RoleId={shift.RoleId}, LocationId={shift.LocationId}",
                NewValues = null
            });

            db.Shifts.Remove(shift);
            db.SaveChanges();

            LoadShiftsGrid();
            LoadSelectedMonthSchedule();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadCombos();
            LoadAssignmentCombos();
            LoadShiftsGrid();
            LoadScheduleAssignmentsGrid();
            LoadSelectedMonthSchedule();
        }

        private void LoadAssignmentCombos()
        {
            using var db = new AppDbContext();

            comboEmployee.DataSource = db.Users
                .Include(u => u.Employee)
                .Include(u => u.Role)
                .OrderBy(u => u.Employee!.LastName)
                .ThenBy(u => u.Employee!.FirstName)
                .Select(u => new
                {
                    u.UserId,
                    Display = u.Employee!.FirstName + " " + u.Employee.LastName +
                              " (" + u.Role!.RoleName + ")"
                })
                .ToList();

            comboEmployee.DisplayMember = "Display";
            comboEmployee.ValueMember = "UserId";

            var showtimes = db.Showtime
                .Include(s => s.Movie)
                .OrderBy(s => s.StartTime)
                .Select(s => new ShowtimeComboItem
                {
                    ShowtimeId = s.ShowtimeId,
                    Display = s.Movie!.Title + " - " + s.StartTime.ToString("MM/dd/yyyy hh:mm tt")
                })
                .ToList();

            showtimes.Insert(0, new ShowtimeComboItem
            {
                ShowtimeId = null,
                Display = "None"
            });

            comboShowtime.DataSource = showtimes;
            comboShowtime.DisplayMember = "Display";
            comboShowtime.ValueMember = "ShowtimeId";
        }

        private void LoadScheduleAssignmentsGrid()
        {
            using var db = new AppDbContext();

            var assignments = db.ScheduleAssignments
                .Include(a => a.User)
                    .ThenInclude(u => u.Employee)
                .Include(a => a.Shift)
                    .ThenInclude(s => s.Role)
                .Include(a => a.Shift)
                    .ThenInclude(s => s.Location)
                .Include(a => a.Showtime)
                    .ThenInclude(s => s.Movie)
                .OrderBy(a => a.Shift!.StartTime)
                .Select(a => new
                {
                    a.AssignmentId,
                    a.AssignmentName,
                    User = a.User!.Employee!.FullName,
                    UserName = a.User.UserName,
                    Role = a.Shift!.Role!.RoleName,
                    Location = a.Shift.Location!.LocationName,
                    StartTime = a.Shift.StartTime,
                    EndTime = a.Shift.EndTime,
                    Showtime = a.Showtime != null ? a.Showtime.Movie!.Title : "None"
                })
                .ToList();

            gridScheduleAssignments.DataSource = assignments;

            if (gridScheduleAssignments.Columns.Count > 0)
                gridScheduleAssignments.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            if (gridScheduleAssignments.Columns["AssignmentId"] != null)
                gridScheduleAssignments.Columns["AssignmentId"].Visible = false;
        }

        private int? GetSelectedAssignmentId()
        {
            if (gridScheduleAssignments.CurrentRow == null)
                return null;

            var value = gridScheduleAssignments.CurrentRow.Cells["AssignmentId"].Value;

            if (value == null)
                return null;

            if (int.TryParse(value.ToString(), out int assignmentId))
                return assignmentId;

            return null;
        }

        private void btnAdd2_Click(object sender, EventArgs e)
        {
            var shiftId = GetSelectedShiftId();

            if (shiftId == null)
            {
                MessageBox.Show("Select a shift first.");
                return;
            }

            if (comboEmployee.SelectedValue == null)
            {
                MessageBox.Show("Select a user/employee.");
                return;
            }

            string assignmentName = txtAssignmentName.Text.Trim();

            if (string.IsNullOrWhiteSpace(assignmentName))
            {
                MessageBox.Show("Assignment name is required.");
                return;
            }

            int userId = Convert.ToInt32(comboEmployee.SelectedValue);

            using var db = new AppDbContext();

            var user = db.Users
                .Include(u => u.Employee)
                .FirstOrDefault(u => u.UserId == userId);

            var shift = db.Shifts.FirstOrDefault(s => s.ShiftId == shiftId.Value);

            if (user == null || user.Employee == null || shift == null)
            {
                MessageBox.Show("User, employee, or shift was not found.");
                return;
            }

            if (user.RoleId != shift.RoleId)
            {
                MessageBox.Show("This user does not have the same role as the selected shift.");
                return;
            }

            bool hasOverlap = db.ScheduleAssignments
                .Include(a => a.Shift)
                .Any(a =>
                    a.UserId == user.UserId &&
                    shift.StartTime < a.Shift!.EndTime &&
                    a.Shift.StartTime < shift.EndTime
                );

            if (hasOverlap)
            {
                MessageBox.Show("This user already has an overlapping schedule assignment.");
                return;
            }

            int? showtimeId = null;

            if (comboShowtime.SelectedItem is ShowtimeComboItem selectedShowtime)
            {
                showtimeId = selectedShowtime.ShowtimeId;
            }

            var assignment = new ScheduleAssignment
            {
                AssignmentName = assignmentName,
                UserId = user.UserId,
                ShiftId = shift.ShiftId,
                ShowtimeId = showtimeId
            };

            db.ScheduleAssignments.Add(assignment);
            db.SaveChanges();

            db.AuditLog.Add(new AuditLog
            {
                UserId = Session.UserId,
                ActionType = "ADD_SCHEDULE_ASSIGNMENT",
                TableName = "ScheduleAssignments",
                ObjectId = assignment.AssignmentId,
                ActionTime = DateTime.Now,
                Description = $"Added schedule assignment '{assignment.AssignmentName}'",
                OldValues = null,
                NewValues = $"UserId={assignment.UserId}, ShiftId={assignment.ShiftId}, ShowtimeId={assignment.ShowtimeId}"
            });

            db.SaveChanges();

            txtAssignmentName.Text = "";
            LoadScheduleAssignmentsGrid();
        }

        private void btnUpdate2_Click(object sender, EventArgs e)
        {
            var assignmentId = GetSelectedAssignmentId();

            if (assignmentId == null)
            {
                MessageBox.Show("Select an assignment to update.");
                return;
            }

            var shiftId = GetSelectedShiftId();

            if (shiftId == null)
            {
                MessageBox.Show("Select a shift for this assignment.");
                return;
            }

            if (comboEmployee.SelectedValue == null)
            {
                MessageBox.Show("Select a user/employee.");
                return;
            }

            string assignmentName = txtAssignmentName.Text.Trim();

            if (string.IsNullOrWhiteSpace(assignmentName))
            {
                MessageBox.Show("Assignment name is required.");
                return;
            }

            int userId = Convert.ToInt32(comboEmployee.SelectedValue);

            using var db = new AppDbContext();

            var assignment = db.ScheduleAssignments.FirstOrDefault(a => a.AssignmentId == assignmentId.Value);
            var user = db.Users.Include(u => u.Employee).FirstOrDefault(u => u.UserId == userId);
            var shift = db.Shifts.FirstOrDefault(s => s.ShiftId == shiftId.Value);

            if (assignment == null || user == null || user.Employee == null || shift == null)
            {
                MessageBox.Show("Assignment, user, employee, or shift was not found.");
                return;
            }

            if (user.RoleId != shift.RoleId)
            {
                MessageBox.Show("This user does not have the same role as the selected shift.");
                return;
            }

            bool hasOverlap = db.ScheduleAssignments
                .Include(a => a.Shift)
                .Any(a =>
                    a.UserId == user.UserId &&
                    a.AssignmentId != assignment.AssignmentId &&
                    shift.StartTime < a.Shift!.EndTime &&
                    a.Shift.StartTime < shift.EndTime
                );

            if (hasOverlap)
            {
                MessageBox.Show("This user already has an overlapping schedule assignment.");
                return;
            }

            int? showtimeId = null;

            if (comboShowtime.SelectedItem is ShowtimeComboItem selectedShowtime)
            {
                showtimeId = selectedShowtime.ShowtimeId;
            }

            var oldName = assignment.AssignmentName;
            var oldUserId = assignment.UserId;
            var oldShiftId = assignment.ShiftId;
            var oldShowtimeId = assignment.ShowtimeId;

            assignment.AssignmentName = assignmentName;
            assignment.UserId = user.UserId;
            assignment.ShiftId = shift.ShiftId;
            assignment.ShowtimeId = showtimeId;

            db.AuditLog.Add(new AuditLog
            {
                UserId = Session.UserId,
                ActionType = "UPDATE_SCHEDULE_ASSIGNMENT",
                TableName = "ScheduleAssignments",
                ObjectId = assignment.AssignmentId,
                ActionTime = DateTime.Now,
                Description = $"Updated schedule assignment '{oldName}'",
                OldValues = $"AssignmentName={oldName}, EmployeeId={oldUserId}, ShiftId={oldShiftId}, ShowtimeId={oldShowtimeId}",
                NewValues = $"AssignmentName={assignment.AssignmentName}, EmployeeId={assignment.UserId}, ShiftId={assignment.ShiftId}, ShowtimeId={assignment.ShowtimeId}"
            });

            db.SaveChanges();

            LoadScheduleAssignmentsGrid();
        }

        private void btnDelete2_Click(object sender, EventArgs e)
        {
            var assignmentId = GetSelectedAssignmentId();

            if (assignmentId == null)
            {
                MessageBox.Show("Select an assignment to delete.");
                return;
            }

            var confirm = MessageBox.Show(
                "Delete this schedule assignment?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes)
                return;

            using var db = new AppDbContext();

            var assignment = db.ScheduleAssignments.FirstOrDefault(a => a.AssignmentId == assignmentId.Value);

            if (assignment == null)
            {
                MessageBox.Show("Assignment not found.");
                return;
            }

            db.AuditLog.Add(new AuditLog
            {
                UserId = Session.UserId,
                ActionType = "DELETE_SCHEDULE_ASSIGNMENT",
                TableName = "ScheduleAssignments",
                ObjectId = assignment.AssignmentId,
                ActionTime = DateTime.Now,
                Description = $"Deleted schedule assignment '{assignment.AssignmentName}'",
                OldValues = $"AssignmentName={assignment.AssignmentName}, EmployeeId={assignment.UserId}, ShiftId={assignment.ShiftId}, ShowtimeId={assignment.ShowtimeId}",
                NewValues = null
            });

            db.ScheduleAssignments.Remove(assignment);
            db.SaveChanges();

            LoadScheduleAssignmentsGrid();
        }

        private void btnRefresh2_Click(object sender, EventArgs e)
        {
            LoadAssignmentCombos();
            LoadScheduleAssignmentsGrid();
            txtAssignmentName.Text = "";
        }

        private void gridScheduleAssignments_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            var assignmentId = GetSelectedAssignmentId();

            if (assignmentId == null)
                return;

            using var db = new AppDbContext();

            var assignment = db.ScheduleAssignments
                .FirstOrDefault(a => a.AssignmentId == assignmentId.Value);

            if (assignment == null)
                return;

            txtAssignmentName.Text = assignment.AssignmentName;
            comboEmployee.SelectedValue = assignment.UserId;
            comboShowtime.SelectedValue = assignment.ShowtimeId;
        }
        private void comboShiftLocation_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadSelectedMonthSchedule();
        }
        private void LoadSelectedMonthSchedule()
        {
            if (comboMonth.SelectedItem?.ToString() == "May 2026")
            {
                LoadScheduleVisual(2026, 5);
            }
        }
    }
}