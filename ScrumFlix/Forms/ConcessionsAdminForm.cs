using ScrumFlix.Data;
using ScrumFlix.Models;
using System;
using System.Linq;
using System.Windows.Forms;

namespace ScrumFlix.Forms
{
    public partial class ConcessionsAdminForm : Form
    {
        public ConcessionsAdminForm()
        {
            InitializeComponent();
        }

        private void ConcessionsAdminForm_Load(object sender, EventArgs e)
        {
            LoadLocations();
            LoadItems();
            LoadStockItems();
        }

        private void LoadItems()
        {
            using var context = new AppDbContext();

            int? locationId = GetSelectedLocationId();

            var query = context.ConcessionItem.AsQueryable();

            if (locationId != null)
            {
                query = query.Where(c => c.LocationId == locationId.Value);
            }

            var items = query
                .OrderBy(c => c.ItemName)
                .Select(c => new
                {
                    c.ConcessionItemId,
                    c.ItemName,
                    c.Price,
                    c.QuantityInStock,
                    c.Minimum,
                    c.LocationId,
                    c.IsActive
                })
                .ToList();

            gridConcessions.DataSource = items;

            if (gridConcessions.Columns.Count > 0)
            {
                gridConcessions.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            }

            if (gridConcessions.Columns["ConcessionItemId"] != null)
            {
                gridConcessions.Columns["ConcessionItemId"].Visible = false;
            }
        }

        private void LoadStockItems()
        {
            using var context = new AppDbContext();

            int? locationId = GetSelectedLocationId();

            var query = context.ConcessionItem
                .Where(c => c.IsActive);

            if (locationId != null)
            {
                query = query.Where(c => c.LocationId == locationId.Value);
            }

            var items = query
                .OrderBy(c => c.ItemName)
                .ToList();

            comboConcessionItem.DataSource = null;
            comboConcessionItem.DisplayMember = "ItemName";
            comboConcessionItem.ValueMember = "ConcessionItemId";
            comboConcessionItem.DataSource = items;

            if (items.Count > 0)
            {
                txtStockQuantity.Text = items[0].QuantityInStock.ToString();
            }
            else
            {
                txtStockQuantity.Text = "";
            }
        }

        private void ClearCrudFields()
        {
            txtItemName.Text = "";
            txtPrice.Text = "";
            txtQuantity.Text = "";
            txtMinimum.Text = "";
        }

        private bool TryGetCrudValues(out string itemName, out decimal price, out int quantity, out int minimum)
        {
            itemName = txtItemName.Text.Trim();
            price = 0;
            quantity = 0;
            minimum = 0;

            if (string.IsNullOrWhiteSpace(itemName))
            {
                MessageBox.Show("Enter a concession name please");
                return false;
            }

            if (!decimal.TryParse(txtPrice.Text.Trim(), out price) || price < 0)
            {
                MessageBox.Show("Enter a valid price");
                return false;
            }

            if (!int.TryParse(txtQuantity.Text.Trim(), out quantity) || quantity < 0)
            {
                MessageBox.Show("Enter a valid quantity");
                return false;
            }

            if (!int.TryParse(txtMinimum.Text.Trim(), out minimum) || minimum < 0)
            {
                MessageBox.Show("Enter a valid minimum quantity");
                return false;
            }

            return true;
        }

        private int? GetSelectedGridItemId()
        {
            if (gridConcessions.CurrentRow == null)
                return null;

            var value = gridConcessions.CurrentRow.Cells["ConcessionItemId"].Value;

            if (value == null)
                return null;

            if (int.TryParse(value.ToString(), out int itemId))
                return itemId;

            return null;
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (!TryGetCrudValues(out string itemName, out decimal price, out int quantity, out int minimum))
                return;

            using var context = new AppDbContext();

            int? locationId = GetSelectedLocationId();

            bool exists = context.ConcessionItem.Any(c =>
                c.ItemName.ToLower() == itemName.ToLower() &&
                c.LocationId == locationId.Value);

            if (exists)
            {
                MessageBox.Show("An item with that name already exists!");
                return;
            }

            if (locationId == null)
            {
                MessageBox.Show("Select a location.");
                return;
            }

            var item = new ConcessionItem
            {
                ItemName = itemName,
                Price = price,
                QuantityInStock = quantity,
                Minimum = minimum,
                LocationId = locationId.Value,
                IsActive = true
            };

            context.ConcessionItem.Add(item);
            context.SaveChanges();

            context.AuditLog.Add(new AuditLog
            {
                UserId = Session.UserId,
                ActionType = "ADD_CONCESSION_ITEM",
                TableName = "ConcessionItem",
                ObjectId = item.ConcessionItemId,
                ActionTime = DateTime.Now,
                Description = $"Added concession item '{item.ItemName}', Notes: '{txtNotes.Text}'",
                OldValues = null,
                NewValues = $"ItemName={item.ItemName}, Price={item.Price}, QuantityInStock={item.QuantityInStock}, Minimum={item.Minimum}, IsActive={item.IsActive}"
            });

            context.SaveChanges();

            ClearCrudFields();
            LoadItems();
            LoadStockItems();
            txtNotes.Text = "";
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            var itemId = GetSelectedGridItemId();

            if (itemId == null)
            {
                MessageBox.Show("Select an item from the grid to update");
                return;
            }

            if (!TryGetCrudValues(out string itemName, out decimal price, out int quantity, out int minimum))
                return;

            using var context = new AppDbContext();

            var item = context.ConcessionItem.FirstOrDefault(c => c.ConcessionItemId == itemId.Value);

            if (item == null)
            {
                MessageBox.Show("Item not found");
                return;
            }

            bool duplicateName = context.ConcessionItem.Any(c =>
                c.ConcessionItemId != item.ConcessionItemId &&
                c.ItemName.ToLower() == itemName.ToLower());

            if (duplicateName)
            {
                MessageBox.Show("Another item already has that name");
                return;
            }

            var oldName = item.ItemName;
            var oldPrice = item.Price;
            var oldQuantity = item.QuantityInStock;
            var oldMinimum = item.Minimum;
            var oldActive = item.IsActive;

            item.ItemName = itemName;
            item.Price = price;
            item.QuantityInStock = quantity;
            item.Minimum = minimum;

            context.AuditLog.Add(new AuditLog
            {
                UserId = Session.UserId,
                ActionType = "UPDATE_CONCESSION_ITEM",
                TableName = "ConcessionItem",
                ObjectId = item.ConcessionItemId,
                ActionTime = DateTime.Now,
                Description = $"Updated concession item '{oldName}', Notes: '{txtNotes.Text}'",
                OldValues = $"ItemName={oldName}, Price={oldPrice}, QuantityInStock={oldQuantity}, Minimum={oldMinimum}, IsActive={oldActive}",
                NewValues = $"ItemName={item.ItemName}, Price={item.Price}, QuantityInStock={item.QuantityInStock}, Minimum={item.Minimum}, IsActive={item.IsActive}"
            });

            context.SaveChanges();

            LoadItems();
            LoadStockItems();
            txtNotes.Text = "";
        }

        private void btnDeactivate_Click(object sender, EventArgs e)
        {
            var itemId = GetSelectedGridItemId();

            if (itemId == null)
            {
                MessageBox.Show("Select an item from the grid to deactivate it");
                return;
            }

            using var context = new AppDbContext();

            var item = context.ConcessionItem.FirstOrDefault(c => c.ConcessionItemId == itemId.Value);

            if (item == null)
            {
                MessageBox.Show("Item not found");
                return;
            }

            var oldActive = item.IsActive;
            item.IsActive = false;

            context.AuditLog.Add(new AuditLog
            {
                UserId = Session.UserId,
                ActionType = "DEACTIVATE_CONCESSION_ITEM",
                TableName = "ConcessionItem",
                ObjectId = item.ConcessionItemId,
                ActionTime = DateTime.Now,
                Description = $"Deactivated concession item '{item.ItemName}'",
                OldValues = $"IsActive={oldActive}",
                NewValues = $"IsActive={item.IsActive}"
            });
            context.SaveChanges();

            LoadItems();
            LoadStockItems();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadItems();
            LoadStockItems();
            ClearCrudFields();
        }

        private void btnIncreaseStock_Click(object sender, EventArgs e)
        {
            if (int.TryParse(txtStockQuantity.Text, out int qty))
            {
                qty++;
                txtStockQuantity.Text = qty.ToString();
            }
            else
            {
                txtStockQuantity.Text = "0";
            }
        }

        private void btnDecreaseStock_Click(object sender, EventArgs e)
        {
            if (int.TryParse(txtStockQuantity.Text, out int qty))
            {
                if (qty > 0)
                    qty--;

                txtStockQuantity.Text = qty.ToString();
            }
            else
            {
                txtStockQuantity.Text = "0";
            }
        }

        private void btnSaveStock_Click(object sender, EventArgs e)
        {
            if (comboConcessionItem.SelectedValue == null)
            {
                MessageBox.Show("Select an item");
                return;
            }

            if (!int.TryParse(comboConcessionItem.SelectedValue.ToString(), out int itemId))
            {
                MessageBox.Show("Invalid item selected");
                return;
            }

            if (!int.TryParse(txtStockQuantity.Text.Trim(), out int newQuantity) || newQuantity < 0)
            {
                MessageBox.Show("Enter a valid stock quantity");
                return;
            }

            using var context = new AppDbContext();

            var item = context.ConcessionItem.FirstOrDefault(c => c.ConcessionItemId == itemId);

            if (item == null)
            {
                MessageBox.Show("Item not found");
                return;
            }

            var oldQuantity = item.QuantityInStock;
            item.QuantityInStock = newQuantity;

            context.AuditLog.Add(new AuditLog
            {
                UserId = Session.UserId,
                ActionType = "UPDATE_CONCESSION_STOCK",
                TableName = "ConcessionItem",
                ObjectId = item.ConcessionItemId,
                ActionTime = DateTime.Now,
                Description = $"Updated stock for concession item '{item.ItemName}', Notes: '{txtNotes.Text}'",
                OldValues = $"QuantityInStock={oldQuantity}",
                NewValues = $"QuantityInStock={item.QuantityInStock}"
            });
            context.SaveChanges();

            LoadItems();
            LoadStockItems();
            txtNotes.Text = "";
        }

        private void comboConcessionItem_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboConcessionItem.SelectedValue == null)
                return;

            if (!int.TryParse(comboConcessionItem.SelectedValue.ToString(), out int itemId))
                return;

            using var context = new AppDbContext();

            var item = context.ConcessionItem.FirstOrDefault(c => c.ConcessionItemId == itemId);

            if (item != null)
            {
                txtStockQuantity.Text = item.QuantityInStock.ToString();
            }
        }

        private void gridConcessions_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (gridConcessions.CurrentRow == null)
                return;

            txtItemName.Text = gridConcessions.CurrentRow.Cells["ItemName"].Value?.ToString();
            txtPrice.Text = gridConcessions.CurrentRow.Cells["Price"].Value?.ToString();
            txtQuantity.Text = gridConcessions.CurrentRow.Cells["QuantityInStock"].Value?.ToString();
            txtMinimum.Text = gridConcessions.CurrentRow.Cells["Minimum"].Value?.ToString();
        }

        private void btnReactivate_Click(object sender, EventArgs e)
        {
            var itemId = GetSelectedGridItemId();

            if (itemId == null)
            {
                MessageBox.Show("Select an item to reactivate it");
                return;
            }

            using var context = new AppDbContext();

            var item = context.ConcessionItem.FirstOrDefault(c => c.ConcessionItemId == itemId.Value);

            if (item == null)
            {
                MessageBox.Show("Item not found");
                return;
            }

            var oldActive = item.IsActive;
            item.IsActive = true;

            context.AuditLog.Add(new AuditLog
            {
                UserId = Session.UserId,
                ActionType = "REACTIVATE_CONCESSION_ITEM",
                TableName = "ConcessionItem",
                ObjectId = item.ConcessionItemId,
                ActionTime = DateTime.Now,
                Description = $"Reactivated concession item '{item.ItemName}'",
                OldValues = $"IsActive={oldActive}",
                NewValues = $"IsActive={item.IsActive}"
            });
            context.SaveChanges();

            LoadItems();
            LoadStockItems();
        }
        private void LoadLocations()
        {
            using var context = new AppDbContext();

            var locations = context.Location
                .Where(l => l.IsActive)
                .OrderBy(l => l.LocationName)
                .Select(l => new
                {
                    l.LocationId,
                    l.LocationName
                })
                .ToList();

            comboLocation.DataSource = null;
            comboLocation.DisplayMember = "LocationName";
            comboLocation.ValueMember = "LocationId";
            comboLocation.DataSource = locations;
        }
        private int? GetSelectedLocationId()
        {
            if (comboLocation.SelectedValue == null)
                return null;

            if (int.TryParse(comboLocation.SelectedValue.ToString(), out int locationId))
                return locationId;

            return null;
        }

        private void comboLocation_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadItems();
            LoadStockItems();
            ClearCrudFields();
        }
    }
}