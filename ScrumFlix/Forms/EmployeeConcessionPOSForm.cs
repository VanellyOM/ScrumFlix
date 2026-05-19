using ScrumFlix.Data;
using ScrumFlix.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using static Google.Protobuf.Reflection.SourceCodeInfo.Types;

namespace ScrumFlix.Forms
{
    public partial class EmployeeConcessionPOSForm : Form
    {
        private List<ConcessionCartItem> cart = new();

        public EmployeeConcessionPOSForm()
        {
            InitializeComponent();
        }

        private void EmployeeConcessionPOSForm_Load(object sender, EventArgs e)
        {
            LoadItems();
            RefreshCartGrid();
            lblTotal.Text = "Total: $0.00";
        }

        private void LoadItems()
        {
            using var context = new AppDbContext();

            var items = context.ConcessionItem
                .Where(c => c.IsActive && c.LocationId == GetLoggedInEmployeeLocationId() && c.QuantityInStock > 0)
                .OrderBy(c => c.ItemName)
                .ToList();

            comboItems.DataSource = null;
            comboItems.DataSource = items;
            comboItems.DisplayMember = "ItemName";
            comboItems.ValueMember = "ConcessionItemId";
        }

        private bool IsValidEmail(string email)
        {
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        private void RefreshCartGrid()
        {
            gridCart.DataSource = null;
            gridCart.DataSource = cart
                .Select(c => new
                {
                    c.ConcessionItemId,
                    c.ItemName,
                    c.Quantity,
                    c.UnitPrice,
                    c.LineTotal
                })
                .ToList();

            if (gridCart.Columns.Count > 0)
            {
                gridCart.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            }

            UpdateTotalLabel();
        }

        private void UpdateTotalLabel()
        {
            decimal total = cart.Sum(c => c.LineTotal);
            lblTotal.Text = $"Total: {total:C}";
        }

        private void btnAddToCart_Click(object sender, EventArgs e)
        {
            if (comboItems.SelectedValue == null)
            {
                MessageBox.Show("Select a concession first");
                return;
            }

            if (!int.TryParse(comboItems.SelectedValue.ToString(), out int itemId))
            {
                MessageBox.Show("Not a valid item");
                return;
            }

            if (!int.TryParse(txtQuantity.Text.Trim(), out int quantity) || quantity <= 0)
            {
                MessageBox.Show("Enter a valid quantity");
                return;
            }

            using var context = new AppDbContext();

            var item = context.ConcessionItem.FirstOrDefault(c => c.ConcessionItemId == itemId);

            if (item == null)
            {
                MessageBox.Show("Concession not found");
                return;
            }

            var existingCartItem = cart.FirstOrDefault(c => c.ConcessionItemId == itemId);
            int quantityAlreadyInCart = existingCartItem?.Quantity ?? 0;

            if (item.QuantityInStock < quantityAlreadyInCart + quantity)
            {
                MessageBox.Show("Not enough stock available");
                return;
            }

            if (existingCartItem != null)
            {
                existingCartItem.Quantity += quantity;
            }
            else
            {
                cart.Add(new ConcessionCartItem
                {
                    ConcessionItemId = item.ConcessionItemId,
                    ItemName = item.ItemName,
                    Quantity = quantity,
                    UnitPrice = item.Price
                });
            }

            txtQuantity.Text = "";
            RefreshCartGrid();
        }

        private void btnRemoveItem_Click(object sender, EventArgs e)
        {
            if (gridCart.CurrentRow == null)
            {
                MessageBox.Show("Select an item to remove from the cart");
                return;
            }

            var value = gridCart.CurrentRow.Cells["ConcessionItemId"].Value;

            if (value == null || !int.TryParse(value.ToString(), out int itemId))
            {
                MessageBox.Show("Could not identify selected cart item");
                return;
            }

            var cartItem = cart.FirstOrDefault(c => c.ConcessionItemId == itemId);

            if (cartItem != null)
            {
                cart.Remove(cartItem);
                RefreshCartGrid();
            }
        }

        private void btnClearCart_Click(object sender, EventArgs e)
        {
            cart.Clear();
            RefreshCartGrid();
        }

        private void btnCheckout_Click(object sender, EventArgs e)
        {
            string email = txtEmail.Text.Trim();

            if (!IsValidEmail(email))
            {
                MessageBox.Show("Enter a valid email");
                return;
            }

            if (cart.Count == 0)
            {
                MessageBox.Show("Cart is empty");
                return;
            }

            using var context = new AppDbContext();

            var itemIds = cart.Select(c => c.ConcessionItemId).ToList();
            var dbItems = context.ConcessionItem
                .Where(c => itemIds.Contains(c.ConcessionItemId))
                .ToList();

            foreach (var cartItem in cart)
            {
                var dbItem = dbItems.FirstOrDefault(c => c.ConcessionItemId == cartItem.ConcessionItemId);

                if (dbItem == null)
                {
                    MessageBox.Show($"Item not found: {cartItem.ItemName}");
                    return;
                }

                if (dbItem.QuantityInStock < cartItem.Quantity)
                {
                    MessageBox.Show($"Not enough stock for {cartItem.ItemName}");
                    return;
                }
            }

            decimal total = cart.Sum(c => c.LineTotal);

            int? locationId = GetLoggedInEmployeeLocationId();

            if (locationId == null)
            {
                MessageBox.Show("Could not determine employee location.");
                return;
            }

            var sale = new ConcessionSale
            {
                UserId = Session.UserId,
                LocationId = locationId.Value,
                CustomerEmail = email,
                TimeOfSale = DateTime.Now,
                Total = total
            };

            context.ConcessionSale.Add(sale);
            context.SaveChanges();

            foreach (var cartItem in cart)
            {
                var dbItem = dbItems.First(c => c.ConcessionItemId == cartItem.ConcessionItemId);

                dbItem.QuantityInStock -= cartItem.Quantity;

                var saleItem = new ConcessionSaleItem
                {
                    ConcessionSaleId = sale.ConcessionSaleId,
                    ConcessionItemId = cartItem.ConcessionItemId,
                    Quantity = cartItem.Quantity,
                    UnitPrice = cartItem.UnitPrice,
                    LineTotal = cartItem.LineTotal
                };

                context.ConcessionSaleItem.Add(saleItem);
            }

            context.SaveChanges();

            string cartSummary = string.Join("; ", cart.Select(c =>
                $"{c.ItemName} x{c.Quantity} @ {c.UnitPrice:C} = {c.LineTotal:C}"
            ));

            context.AuditLog.Add(new AuditLog
            {
                UserId = Session.UserId,
                ActionType = "SELL_CONCESSION",
                TableName = "ConcessionSale",
                ObjectId = sale.ConcessionSaleId,
                ActionTime = DateTime.Now,
                Description = $"Sold concessions to '{email}' for {total:C}",
                OldValues = null,
                NewValues = $"CustomerEmail={email}, Total={total}, Items={cartSummary}"
            });

            context.SaveChanges();

            try
            {
                SendReceiptEmail(email, sale, cart);
                MessageBox.Show("Sale completed and receipt sent! :D");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Sale completed, but receipt email failed (Sale still logged in database): " + ex.Message);
            }

            cart.Clear();
            RefreshCartGrid();
            txtEmail.Text = "";
            txtQuantity.Text = "";
            LoadItems();
        }

        private void SendReceiptEmail(string toEmail, ConcessionSale sale, List<ConcessionCartItem> cartItems)
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

            var body = new StringBuilder();
            body.AppendLine("Thank you for your purchase from ScrumFlix!");
            body.AppendLine();
            body.AppendLine("Items:");

            foreach (var item in cartItems)
            {
                body.AppendLine($"{item.ItemName} x{item.Quantity} - {item.LineTotal:C}");
            }

            body.AppendLine();
            body.AppendLine($"Total: {sale.Total:C}");
            body.AppendLine($"Time: {sale.TimeOfSale}");
            body.AppendLine($"Sold By User ID: {sale.UserId}");

            var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = "ScrumFlix Concession Receipt",
                Body = body.ToString()
            };

            smtp.Send(message);
        }

        private void EmployeeConcessionPOSForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void comboItems_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboItems.SelectedItem is ConcessionItem item)
            {
                lblQuantity.Text = $"Stock: {item.QuantityInStock}";
            }
        }

        private void EmployeeConcessionPOSForm_FormClosing(object sender, FormClosingEventArgs e)
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
        private int? GetLoggedInEmployeeLocationId()
        {
            using var context = new AppDbContext();

            return context.Users
                .Where(u => u.UserId == Session.UserId)
                .Select(u => (int?)u.Employee!.LocationId)
                .FirstOrDefault();
        }
    }
}