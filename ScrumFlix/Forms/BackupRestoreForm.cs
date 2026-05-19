using ScrumFlix.Data;
using ScrumFlix.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace ScrumFlix.Forms
{
    public partial class BackupRestoreForm : Form
    {
        private readonly string backupFolder =
            Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Backup"));

        public BackupRestoreForm()
        {
            InitializeComponent();
        }

        private void BackupRestoreForm_Load(object sender, EventArgs e)
        {
            Directory.CreateDirectory(backupFolder);
            LoadBackups();
        }

        private void LoadBackups()
        {
            Directory.CreateDirectory(backupFolder);

            var files = Directory.GetFiles(backupFolder, "*.sql")
                .Select(f => new
                {
                    FileName = Path.GetFileName(f),
                    Created = File.GetCreationTime(f),
                    SizeKB = Math.Round(new FileInfo(f).Length / 1024.0, 2),
                    FullPath = f
                })
                .OrderByDescending(f => f.Created)
                .ToList();

            gridBackups.DataSource = files;

            if (gridBackups.Columns.Count > 0)
            {
                gridBackups.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            }

            if (gridBackups.Columns["FullPath"] != null)
            {
                gridBackups.Columns["FullPath"].Visible = false;
            }
        }

        private string? GetSelectedBackupPath()
        {
            if (gridBackups.CurrentRow == null)
                return null;

            var value = gridBackups.CurrentRow.Cells["FullPath"].Value;

            return value?.ToString();
        }

        private async void btnBackup_Click(object sender, EventArgs e)
        {
            progressBar.Visible = true;
            progressBar.Style = ProgressBarStyle.Marquee;
            btnBackup.Enabled = false;

            try
            {
                string message = await Task.Run(() =>
                {
                    Directory.CreateDirectory(backupFolder);

                    string fileName = $"ScrumFlix_Backup_{DateTime.Now:yyyy-MM-dd_HHmmss}.sql";
                    string backupPath = Path.Combine(backupFolder, fileName);

                    string mysqldumpPath = @"C:\Program Files\MySQL\MySQL Server 8.0\bin\mysqldump.exe";

                    string arguments =
                        "--host=mysql-scrumtheater-scrumflix-theater.b.aivencloud.com " +
                        "--port=12031 " +
                        "--user=avnadmin " +
                        "--password=AVNS_qfxTTR9RIG_piTLOwLl " +
                        "--ssl-mode=REQUIRED " +
                        "--single-transaction " +
                        "--routines " +
                        "--triggers " +
                        "--set-gtid-purged=OFF " +
                        "--no-tablespaces " +
                        "defaultdb";

                    var process = new Process();
                    process.StartInfo.FileName = mysqldumpPath;
                    process.StartInfo.Arguments = arguments;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;

                    process.Start();

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        throw new Exception(error);
                    }

                    File.WriteAllText(backupPath, output);

                    using var db = new AppDbContext();
                    db.AuditLog.Add(new AuditLog
                    {
                        UserId = Session.UserId,
                        ActionType = "BACKUP_DATABASE",
                        TableName = "Database",
                        ObjectId = null,
                        ActionTime = DateTime.Now,
                        Description = $"Created database backup '{fileName}'",
                        OldValues = null,
                        NewValues = $"BackupPath={backupPath}"
                    });
                    db.SaveChanges();

                    return $"Backup created successfully.\n\nSaved to:\n{backupPath}";
                });

                LoadBackups();
                MessageBox.Show(message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Backup failed:\n\n" + ex.Message);
            }
            finally
            {
                progressBar.Style = ProgressBarStyle.Blocks;
                progressBar.Visible = false;
                btnBackup.Enabled = true;
            }
        }

        private void btnRestore_Click(object sender, EventArgs e)
        {
            string? backupPath = GetSelectedBackupPath();

            if (string.IsNullOrWhiteSpace(backupPath) || !File.Exists(backupPath))
            {
                MessageBox.Show("Select a valid backup file first.");
                return;
            }

            var confirm = MessageBox.Show(
                "Restoring will replace current database data. Are you sure?",
                "Confirm Restore",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes)
                return;

            string mysqlPath = @"C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe";

            string arguments =
                "--host=mysql-scrumtheater-scrumflix-theater.b.aivencloud.com " +
                "--port=12031 " +
                "--user=avnadmin " +
                "--password=AVNS_qfxTTR9RIG_piTLOwLl " +
                "--ssl-mode=REQUIRED " +
                "defaultdb";

            var process = new Process();

            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = $"/C \"\"{mysqlPath}\" {arguments} < \"{backupPath}\"\"";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                MessageBox.Show("Restore failed:\n\n" + error);
                return;
            }

            MessageBox.Show("Restore completed successfully.");
            using var db = new AppDbContext();

            db.AuditLog.Add(new AuditLog
            {
                UserId = Session.UserId,
                ActionType = "RESTORE_DATABASE",
                TableName = "Database",
                ObjectId = null,
                ActionTime = DateTime.Now,
                Description = $"Restored database from '{Path.GetFileName(backupPath)}'",
                OldValues = null,
                NewValues = $"BackupPath={backupPath}"
            });

            db.SaveChanges();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadBackups();
        }
    }
}