using Microsoft.EntityFrameworkCore;
using Sanes.Domain.Entities;
using Sanes.Domain.Enums;

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
    public DbSet<LoanGuarantee> LoanGuarantees =>
        Set<LoanGuarantee>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentAllocation> PaymentAllocations =>
    Set<PaymentAllocation>();
    public DbSet<LateFeeCharge> LateFeeCharges =>
        Set<LateFeeCharge>();
    public DbSet<LateFeeAdjustment> LateFeeAdjustments =>
        Set<LateFeeAdjustment>();
    public DbSet<CollectionRoute> CollectionRoutes => Set<CollectionRoute>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<AppUserCollectionRoute> AppUserCollectionRoutes
    => Set<AppUserCollectionRoute>();
    public DbSet<CollectionRouteSchedule> CollectionRouteSchedules
    => Set<CollectionRouteSchedule>();

    public DbSet<LoanBalanceAdjustment> LoanBalanceAdjustments =>
        Set<LoanBalanceAdjustment>();

    public DbSet<EarlySettlement> EarlySettlements =>
        Set<EarlySettlement>();

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

            entity.Property(x => x.DefaultLateFeeEnabled)
                .HasDefaultValue(false);

            entity.Property(x => x.DefaultLateFeeCalculationType)
                .HasDefaultValue(LateFeeCalculationType.FixedAmountPerInstallment)
                .HasSentinel((LateFeeCalculationType)0);

            entity.Property(x => x.DefaultLateFeeAmount)
                .HasPrecision(18, 2)
                .HasDefaultValue(0m);

            entity.Property(x => x.DefaultLateFeeGraceDays)
                .HasDefaultValue(0);

            entity.Property(x => x.GuaranteeRequiredFromAmount)
                .HasPrecision(18, 2);
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

            entity.Property(x => x.LateFeeEnabled)
                .HasDefaultValue(false);

            entity.Property(x => x.LateFeeCalculationType)
                .HasDefaultValue(LateFeeCalculationType.FixedAmountPerInstallment)
                .HasSentinel((LateFeeCalculationType)0);

            entity.Property(x => x.LateFeeAmount)
                .HasPrecision(18, 2)
                .HasDefaultValue(0m);

            entity.Property(x => x.LateFeeGraceDays)
                .HasDefaultValue(0);

            entity.Property(x => x.GuaranteeRequired)
                .HasDefaultValue(false);

            entity.Property(x => x.GuaranteeThresholdAtCreation)
                .HasPrecision(18, 2);

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

        modelBuilder.Entity<LoanGuarantee>(entity =>
        {
            entity.ToTable("loan_guarantees");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Type)
                .IsRequired();

            entity.Property(x => x.Reference)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.Description)
                .HasMaxLength(1000);

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.Property(x => x.UpdatedAt)
                .IsRequired();

            entity.HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Loan)
                .WithOne(x => x.Guarantee)
                .HasForeignKey<LoanGuarantee>(
                    x => x.LoanId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.TenantId);

            entity.HasIndex(x => new
            {
                x.TenantId,
                x.LoanId
            })
            .IsUnique();
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

            entity.HasOne(x => x.CollectedByAppUser)
                .WithMany()
                .HasForeignKey(x => x.CollectedByAppUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.CollectionRoute)
                .WithMany()
                .HasForeignKey(x => x.CollectionRouteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.CollectedByAppUserId);

            entity.HasIndex(x => x.CollectionRouteId);

            entity.HasIndex(x => x.TenantId);

            entity.HasIndex(x => new { x.TenantId, x.LoanId });

            entity.HasIndex(x => new { x.TenantId, x.PaymentDate });
        });

        modelBuilder.Entity<PaymentAllocation>(entity =>
        {
            entity.ToTable("payment_allocations");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.AllocationType)
                .IsRequired();

            entity.Property(x => x.Amount)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Payment)
                .WithMany(x => x.Allocations)
                .HasForeignKey(x => x.PaymentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.LateFeeCharge)
                .WithMany(x => x.PaymentAllocations)
                .HasForeignKey(x => x.LateFeeChargeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.TenantId);

            entity.HasIndex(x => new
            {
                x.TenantId,
                x.PaymentId
            });

            entity.HasIndex(x => new
            {
                x.TenantId,
                x.LateFeeChargeId
            });
        });

        modelBuilder.Entity<LateFeeCharge>(entity =>
        {
            entity.ToTable("late_fee_charges");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.InstallmentNumber)
                .IsRequired();

            entity.Property(x => x.InstallmentDueDate)
                .IsRequired();

            entity.Property(x => x.EffectiveDate)
                .IsRequired();

            entity.Property(x => x.CalculationType)
                .IsRequired();

            entity.Property(x => x.Amount)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Loan)
                .WithMany()
                .HasForeignKey(x => x.LoanId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.TenantId);

            entity.HasIndex(x => new
            {
                x.TenantId,
                x.LoanId
            });

            entity.HasIndex(x => new
            {
                x.TenantId,
                x.LoanId,
                x.InstallmentNumber
            });

            entity.HasIndex(x => new
            {
                x.TenantId,
                x.LoanId,
                x.InstallmentNumber,
                x.EffectiveDate
            })
            .IsUnique();
        });

        modelBuilder.Entity<LateFeeAdjustment>(entity =>
        {
            entity.ToTable("late_fee_adjustments");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.AdjustmentType)
                .IsRequired();

            entity.Property(x => x.Amount)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(x => x.Reason)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.LateFeeCharge)
                .WithMany(x => x.Adjustments)
                .HasForeignKey(x => x.LateFeeChargeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.AppUser)
                .WithMany()
                .HasForeignKey(x => x.AppUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.TenantId);

            entity.HasIndex(x => new
            {
                x.TenantId,
                x.LateFeeChargeId
            });

            entity.HasIndex(x => new
            {
                x.TenantId,
                x.AppUserId
            });
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

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(x => x.Username)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.PasswordHash)
                .HasMaxLength(500);

            entity.Property(x => x.Email)
                .HasMaxLength(150);

            entity.Property(x => x.Phone)
                .HasMaxLength(30);

            entity.Property(x => x.Role)
                .IsRequired();

            entity.HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.TenantId);

            entity.HasIndex(x => new
            {
                x.TenantId,
                x.Username
            })
            .IsUnique();
        });

        modelBuilder.Entity<AppUserCollectionRoute>(entity =>
        {
            entity.HasKey(x => new
            {
                x.AppUserId,
                x.CollectionRouteId
            });

            entity.HasOne(x => x.AppUser)
                .WithMany(x => x.CollectionRoutes)
                .HasForeignKey(x => x.AppUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.CollectionRoute)
                .WithMany(x => x.AssignedUsers)
                .HasForeignKey(x => x.CollectionRouteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.CollectionRouteId);
        });

        modelBuilder.Entity<CollectionRouteSchedule>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.DayOfWeek)
                .IsRequired();

            entity.Property(x => x.StartTime);

            entity.Property(x => x.EndTime);

            entity.HasOne(x => x.CollectionRoute)
                .WithMany(x => x.Schedules)
                .HasForeignKey(x => x.CollectionRouteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.CollectionRouteId);

            entity.HasIndex(x => new
            {
                x.CollectionRouteId,
                x.DayOfWeek
            })
            .IsUnique();
        });

        modelBuilder.Entity<LoanBalanceAdjustment>(entity =>
        {
            entity.ToTable("loan_balance_adjustments");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.AdjustmentType)
                .IsRequired();

            entity.Property(x => x.Amount)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(x => x.Reason)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Loan)
                .WithMany(x => x.BalanceAdjustments)
                .HasForeignKey(x => x.LoanId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.AppUser)
                .WithMany()
                .HasForeignKey(x => x.AppUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.TenantId);

            entity.HasIndex(x =>
                new
                {
                    x.TenantId,
                    x.LoanId
                });

            entity.HasIndex(x =>
                new
                {
                    x.TenantId,
                    x.AppUserId
                });
        });

        modelBuilder.Entity<EarlySettlement>(entity =>
        {
            entity.ToTable("early_settlements");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.DiscountType)
                .IsRequired();

            entity.Property(x => x.DiscountValue)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(x => x.ContractualBalanceBefore)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(x => x.LateFeeBalanceBefore)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(x => x.TotalOutstandingBefore)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(x => x.DiscountAmount)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(x => x.SettlementAmount)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(x => x.CompletedInstallments)
                .IsRequired();

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Loan)
                .WithOne(x => x.EarlySettlement)
                .HasForeignKey<EarlySettlement>(
                    x => x.LoanId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.AppUser)
                .WithMany()
                .HasForeignKey(x => x.AppUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Payment)
                .WithOne(x => x.EarlySettlement)
                .HasForeignKey<EarlySettlement>(
                    x => x.PaymentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.LoanBalanceAdjustment)
                .WithOne()
                .HasForeignKey<EarlySettlement>(
                    x => x.LoanBalanceAdjustmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.TenantId);

            entity.HasIndex(x =>
                new
                {
                    x.TenantId,
                    x.LoanId
                })
                .IsUnique();

            entity.HasIndex(x =>
                new
                {
                    x.TenantId,
                    x.AppUserId
                });

            entity.HasIndex(x => x.PaymentId)
                .IsUnique();

            entity.HasIndex(x => x.LoanBalanceAdjustmentId)
                .IsUnique();
        });
    }
}
