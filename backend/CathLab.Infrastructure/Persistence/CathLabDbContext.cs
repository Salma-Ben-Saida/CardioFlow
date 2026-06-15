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
        e.Property(s => s.StudyInstanceUid).HasMaxLength(200);
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
}
}