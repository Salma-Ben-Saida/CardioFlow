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
            e.HasOne(p => p.RiskProfile)
             .WithOne(r => r.Patient)
             .HasForeignKey<RiskProfile>(r => r.PatientId);
            e.HasMany(p => p.Studies)
             .WithOne(s => s.Patient)
             .HasForeignKey(s => s.PatientId);
        });

        modelBuilder.Entity<RiskProfile>(e =>
{
    e.HasKey(r => r.Id);
    e.Property(r => r.ObesityBMI)
     .HasPrecision(5, 2);      // ex: 999.99
    e.Property(r => r.EjectionFraction)
     .HasPrecision(5, 4);      // ex: 0.6543 (fraction entre 0 et 1)
});

        modelBuilder.Entity<CathStudy>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<AppUser>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Id).ValueGeneratedOnAdd();
        });
    }
}