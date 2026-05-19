// Connects the program to the database

using Microsoft.EntityFrameworkCore;
using ScrumFlix.Models;
using System.Data;

namespace ScrumFlix.Data;

public class AppDbContext : DbContext
{
    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<TheaterScreen> TheaterScreen => Set<TheaterScreen>();
    public DbSet<Showtime> Showtime => Set<Showtime>();
    public DbSet<Location> Location => Set<Location>();
    public DbSet<Ticket> Ticket { get; set; }
    public DbSet<User> Users => Set<User>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<ConcessionItem> ConcessionItem => Set<ConcessionItem>();
    public DbSet<ConcessionSale> ConcessionSale => Set<ConcessionSale>();
    public DbSet<ConcessionSaleItem> ConcessionSaleItem => Set<ConcessionSaleItem>();
    public DbSet<AuditLog> AuditLog => Set<AuditLog>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<ScheduleAssignment> ScheduleAssignments => Set<ScheduleAssignment>();
    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();
    public DbSet<PayPeriod> PayPeriods => Set<PayPeriod>();
    public DbSet<Timesheet> Timesheets => Set<Timesheet>();
    public DbSet<Payroll> Payrolls => Set<Payroll>();
    public DbSet<PayStub> PayStubs => Set<PayStub>();

    /* protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseSqlite($"Data Source={AppPaths.DatabasePath}");
    } < old sqlite connection*/

    // new aiven connection
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured) return;

        var aiven = "Server=mysql-scrumtheater-scrumflix-theater.b.aivencloud.com;Port=12031;Database=defaultdb;User=avnadmin;Password=AVNS_qfxTTR9RIG_piTLOwLl;SslMode=Required;";

        optionsBuilder.UseMySQL(aiven);
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TheaterScreen>()
            .Property(t => t.Capacity)
            .HasDefaultValue(50);

        modelBuilder.Entity<Ticket>()
            .HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserAtSale);

        modelBuilder.Entity<Ticket>()
            .HasOne(t => t.Showtime)
            .WithMany()
            .HasForeignKey(t => t.ShowtimeId);


        modelBuilder.Entity<ConcessionSale>()
            .HasOne(cs => cs.User)
            .WithMany()
            .HasForeignKey(cs => cs.UserId);

        modelBuilder.Entity<ConcessionSaleItem>()
            .HasOne(csi => csi.ConcessionSale)
            .WithMany(cs => cs.SaleItems)
            .HasForeignKey(csi => csi.ConcessionSaleId);

        modelBuilder.Entity<ConcessionSaleItem>()
            .HasOne(csi => csi.ConcessionItem)
            .WithMany()
            .HasForeignKey(csi => csi.ConcessionItemId);
    }
}
