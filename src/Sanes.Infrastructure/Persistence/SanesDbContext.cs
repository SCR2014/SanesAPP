using Microsoft.EntityFrameworkCore;
using Sanes.Domain.Entities;

namespace Sanes.Infrastructure.Persistence;

public class SanesDbContext : DbContext
{
    public SanesDbContext(DbContextOptions<SanesDbContext> options)
        : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Investor> Investors => Set<Investor>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<CollectionRoute> CollectionRoutes => Set<CollectionRoute>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("tenants");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.LegalName)
                .HasMaxLength(200);

            entity.Property(x => x.Phone)
                .HasMaxLength(30);

            entity.Property(x => x.Email)
                .HasMaxLength(150);

            entity.Property(x => x.CurrencyCode)
                .HasMaxLength(3)
                .IsRequired();

            entity.Property(x => x.CurrencySymbol)
                .HasMaxLength(10)
                .IsRequired();
        });

        modelBuilder.Entity<Investor>(entity =>
        {
            entity.ToTable("investors");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.Phone)
                .HasMaxLength(30);

            entity.Property(x => x.Email)
                .HasMaxLength(150);

            entity.Property(x => x.Identification)
                .HasMaxLength(100);

            entity.Property(x => x.Notes)
                .HasMaxLength(1000);

            entity.HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.TenantId);

            entity.HasIndex(x => new { x.TenantId, x.Identification })
                .IsUnique();    
        });

        modelBuilder.Entity<Client>(entity =>
        {
            entity.ToTable("clients");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.FirstName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.LastName)
                .HasMaxLength(100);

            entity.Property(x => x.Phone)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(x => x.SecondaryPhone)
                .HasMaxLength(30);

            entity.Property(x => x.IdentificationType)
                .HasMaxLength(50);

            entity.Property(x => x.Identification)
                .HasMaxLength(100);

            entity.Property(x => x.SocialNumber)
                .HasMaxLength(150);

            entity.Property(x => x.Address)
                .HasMaxLength(500);

            entity.Property(x => x.Notes)
                .HasMaxLength(1000);

            entity.Property(x => x.Latitude)
                .HasPrecision(9, 6);

            entity.Property(x => x.Longitude)
                .HasPrecision(9, 6);

            entity.HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.TenantId);

            entity.HasIndex(x => new { x.TenantId, x.Phone });

            entity.HasIndex(x => new { x.TenantId, x.Identification })
            .IsUnique();

            entity.HasOne(x => x.CollectionRoute)
                .WithMany()
                .HasForeignKey(x => x.CollectionRouteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.CollectionRouteId);

            entity.HasIndex(x => new
            {
                x.TenantId,
                x.CollectionRouteId,
                x.CollectionRouteOrder
            });
        });

        modelBuilder.Entity<Loan>(entity =>
        {
            entity.ToTable("loans");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.PrincipalAmount)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(x => x.InstallmentAmount)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(x => x.TotalInstallments)
                .IsRequired();

            entity.Property(x => x.PaymentFrequency)
                .IsRequired();

            entity.Property(x => x.StartDate)
                .IsRequired();

            entity.Property(x => x.NextPaymentDate)
                .IsRequired();

            entity.Property(x => x.Status)
                .IsRequired();

            entity.Property(x => x.Notes)
                .HasMaxLength(1000);

            entity.HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Investor)
                .WithMany()
                .HasForeignKey(x => x.InvestorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Client)
                .WithMany()
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.TenantId);

            entity.HasIndex(x => new
            {
                x.TenantId,
                x.InvestorId
            });

            entity.HasIndex(x => new
            {
                x.TenantId,
                x.ClientId
            });

            entity.HasIndex(x => new
            {
                x.TenantId,
                x.Status
            });
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("payments");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Amount)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(x => x.PaymentDate)
                .IsRequired();

            entity.Property(x => x.PaymentType)
                .IsRequired();

            entity.Property(x => x.Notes)
                .HasMaxLength(1000);

            entity.HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Loan)
                .WithMany()
                .HasForeignKey(x => x.LoanId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.TenantId);

            entity.HasIndex(x => new { x.TenantId, x.LoanId });

            entity.HasIndex(x => new { x.TenantId, x.PaymentDate });
        });

        modelBuilder.Entity<CollectionRoute>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(x => x.Description)
                .HasMaxLength(500);

            entity.Property(x => x.OrderMode)
                .IsRequired();

            entity.HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.TenantId);

            entity.HasIndex(x => new { x.TenantId, x.Name });
        });
    }
}
