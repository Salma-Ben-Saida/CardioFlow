using CathLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CathLab.Infrastructure.Persistence;

public class CathLabDbContext : DbContext
{
    public CathLabDbContext(DbContextOptions<CathLabDbContext> options)
        : base(options) { }

    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<RiskProfile> RiskProfiles => Set<RiskProfile>();
    public DbSet<CathStudy> CathStudies => Set<CathStudy>();

    public DbSet<ProcedureReport> ProcedureReports => Set<ProcedureReport>();
    public DbSet<CoronaryLesion> CoronaryLesions => Set<CoronaryLesion>();

    // No DbSet<LesionTreatment> - it's abstract (TPT base). Query concrete
    // types directly, or CoronaryLesion.Treatments / InventoryItem.Treatment.
    public DbSet<StentImplant> StentImplants => Set<StentImplant>();
    public DbSet<BalloonDilation> BalloonDilations => Set<BalloonDilation>();

    public DbSet<InventoryProduct> InventoryProducts => Set<InventoryProduct>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<UserActivityLog> UserActivityLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Patient>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).ValueGeneratedOnAdd();
            e.Property(p => p.FullName).HasMaxLength(200);
            e.Property(p => p.ConquestPatientID).HasMaxLength(100);
            e.Property(p => p.NationalId).HasMaxLength(50);
            e.Property(p => p.Sex).HasMaxLength(1);
            e.Property(p => p.BloodGroup).HasMaxLength(10);
            e.HasIndex(p => p.ConquestPatientID)
             .IsUnique()
             .HasFilter("[ConquestPatientID] IS NOT NULL");
            e.Property(p => p.RowVersion).IsRowVersion();
            e.Property(p => p.DeleteReason).HasMaxLength(500);
            e.HasQueryFilter(p => !p.IsDeleted);
            e.HasOne(p => p.RiskProfile)
             .WithOne(r => r.Patient)
             .HasForeignKey<RiskProfile>(r => r.PatientId);
            e.HasMany(p => p.Studies)
             .WithOne(s => s.Patient)
             .HasForeignKey(s => s.PatientId);
        });

        modelBuilder.Entity<CathStudy>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).ValueGeneratedOnAdd();
            e.Property(s => s.AccessionNumber).HasMaxLength(100);
            e.Property(s => s.StudyInstanceUid).IsRequired().HasMaxLength(200);
            e.HasIndex(s => s.StudyInstanceUid).IsUnique();
            e.HasIndex(s => s.AccessionNumber);
            e.Property(s => s.DeleteReason).HasMaxLength(500);
            e.HasQueryFilter(s => !s.IsDeleted);
            e.Property(s => s.Status)
             .HasConversion<string>()
             .HasMaxLength(50);
        });

        modelBuilder.Entity<AppUser>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Id).ValueGeneratedOnAdd();
            e.Property(u => u.Username).HasMaxLength(100);
            e.Property(u => u.FullName).HasMaxLength(200);
            e.Property(u => u.Role)
             .HasConversion<string>()
             .HasMaxLength(50);
        });

        modelBuilder.Entity<RiskProfile>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.ObesityBMI).HasPrecision(5, 2);
            e.Property(r => r.EjectionFraction).HasPrecision(5, 4);
        });

        // ── Procedure report & lesions ──────────────────────────────

        modelBuilder.Entity<ProcedureReport>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).ValueGeneratedOnAdd();
            e.Property(p => p.Approach).HasMaxLength(100);
            e.Property(p => p.CatheterUsed).HasMaxLength(100);
            e.Property(p => p.ContrastVolumeMl).HasPrecision(6, 2);
            e.Property(p => p.DoseAreaProductGycm2).HasPrecision(8, 2);
            e.Property(p => p.DominanceType).HasMaxLength(50);
            e.Property(p => p.PdfStoragePath).HasMaxLength(500);

            e.HasOne(p => p.CathStudy)
             .WithOne(s => s.ProcedureReport)
             .HasForeignKey<ProcedureReport>(p => p.CathStudyId);

            e.HasMany(p => p.Lesions)
             .WithOne(l => l.ProcedureReport)
             .HasForeignKey(l => l.ProcedureReportId);
        });

        modelBuilder.Entity<CoronaryLesion>(e =>
        {
            e.HasKey(l => l.Id);
            e.Property(l => l.Id).ValueGeneratedOnAdd();
            e.Property(l => l.Vessel).HasMaxLength(50);
            e.Property(l => l.LesionType)
             .HasConversion<string>()
             .HasMaxLength(20);

            e.HasMany(l => l.Treatments)
             .WithOne(t => t.Lesion)
             .HasForeignKey(t => t.LesionId);
        });

        // ── Lesion treatments (TPT: LesionTreatment / StentImplant / BalloonDilation) ──

        modelBuilder.Entity<LesionTreatment>(e =>
        {
            e.ToTable("LesionTreatments");
            e.HasKey(t => t.Id);
            e.Property(t => t.Id).ValueGeneratedOnAdd();
            e.Property(t => t.Notes).HasMaxLength(1000);

            // One physical item is consumed by exactly one treatment.
            // Restrict delete: never let deleting a treatment cascade
            // into deleting inventory/stock history.
            e.HasOne(t => t.InventoryItem)
             .WithOne(i => i.Treatment)
             .HasForeignKey<LesionTreatment>(t => t.InventoryItemId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StentImplant>(e =>
        {
            e.ToTable("StentImplants");
            e.Property(s => s.LandingZoneProximal).HasMaxLength(50);
            e.Property(s => s.LandingZoneDistal).HasMaxLength(50);
            e.Property(s => s.StentDiameterMm).HasPrecision(4, 2);
            e.Property(s => s.StentLengthMm).HasPrecision(5, 2);
        });

        modelBuilder.Entity<BalloonDilation>(e =>
        {
            e.ToTable("BalloonDilations");
            e.Property(b => b.BalloonDiameterMm).HasPrecision(4, 2);
            e.Property(b => b.BalloonLengthMm).HasPrecision(5, 2);
        });

        // ── Pharmacy / inventory ────────────────────────────────────

        modelBuilder.Entity<InventoryProduct>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).ValueGeneratedOnAdd();
            e.Property(p => p.Reference).HasMaxLength(100);
            e.Property(p => p.ProductName).HasMaxLength(200);
            e.Property(p => p.Manufacturer).HasMaxLength(200);
            e.Property(p => p.UnitOfMeasure).HasMaxLength(20);
            e.Property(p => p.DeviceType)
             .HasConversion<string>()
             .HasMaxLength(30);

            e.HasIndex(p => p.Reference).IsUnique();
        });

        modelBuilder.Entity<InventoryItem>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.Id).ValueGeneratedOnAdd();
            e.Property(i => i.Barcode).HasMaxLength(100);
            e.Property(i => i.LotNumber).HasMaxLength(100);
            e.Property(i => i.SerialNumber).HasMaxLength(100);
            e.Property(i => i.Status)
             .HasConversion<string>()
             .HasMaxLength(20);

            e.HasIndex(i => i.Barcode).IsUnique();

            e.HasOne(i => i.Product)
             .WithMany(p => p.Items)
             .HasForeignKey(i => i.ProductId);

            // Reservation lock against a study, cleared (not cascaded)
            // if the study is ever deleted.
            e.HasOne(i => i.ReservedForCathStudy)
             .WithMany(s => s.ReservedItems)
             .HasForeignKey(i => i.ReservedForCathStudyId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<StockMovement>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.Id).ValueGeneratedOnAdd();
            e.Property(m => m.MovementType)
             .HasConversion<string>()
             .HasMaxLength(20);
            e.Property(m => m.ReferenceNote).HasMaxLength(500);

            e.HasOne(m => m.Product)
             .WithMany(p => p.Movements)
             .HasForeignKey(m => m.ProductId);

            e.HasOne(m => m.CreatedByUser)
             .WithMany()
             .HasForeignKey(m => m.CreatedBy)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}