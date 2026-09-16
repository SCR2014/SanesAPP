using Sanes.Application.Clients.Repositories;
using Sanes.Application.Investors.Repositories;
using Sanes.Application.Loans.DTOs;
using Sanes.Application.Loans.Repositories;
using Sanes.Application.Tenants.Repositories;
using Sanes.Application.Payments.Repositories;
using Sanes.Application.LateFees.Services;
using Sanes.Domain.Entities;
using Sanes.Domain.Enums;

namespace Sanes.Application.Loans.Services;

public class LoanService : ILoanService
{
    private readonly ILoanRepository _loanRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IInvestorRepository _investorRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IPaymentAllocationRepository _paymentAllocationRepository;
    private readonly ILateFeeAccrualService _lateFeeAccrualService;
    private readonly ILateFeeBalanceService _lateFeeBalanceService;

    public LoanService(
    ILoanRepository loanRepository,
    ITenantRepository tenantRepository,
    IInvestorRepository investorRepository,
    IClientRepository clientRepository,
    IPaymentAllocationRepository paymentAllocationRepository,
    ILateFeeAccrualService lateFeeAccrualService,
    ILateFeeBalanceService lateFeeBalanceService)
{
    _loanRepository = loanRepository;
    _tenantRepository = tenantRepository;
    _investorRepository = investorRepository;
    _clientRepository = clientRepository;
    _paymentAllocationRepository = paymentAllocationRepository;
    _lateFeeAccrualService = lateFeeAccrualService;
    _lateFeeBalanceService = lateFeeBalanceService;
}

    public async Task<LoanResponse> CreateAsync(
        Guid tenantId,
        CreateLoanRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(
            tenantId,
            cancellationToken);

        if (tenant is null || !tenant.IsActive)
        {
            throw new InvalidOperationException(
                "Tenant not found or inactive.");
        }

        var lateFeePolicy =
            ResolveLateFeePolicy(
                request,
                tenant);

        var investor = await _investorRepository.GetByIdAsync(
            tenantId,
            request.InvestorId,
            cancellationToken);

        if (investor is null)
        {
            throw new InvalidOperationException(
                "Investor not found, inactive, or does not belong to the specified tenant.");
        }

        var client = await _clientRepository.GetByIdAsync(
            tenantId,
            request.ClientId,
            cancellationToken);

        if (client is null)
        {
            throw new InvalidOperationException(
                "Client not found, inactive, or does not belong to the specified tenant.");
        }

        var startDate = NormalizeUtc(request.StartDate);

        var loan = new Loan
        {
            TenantId = tenantId,
            InvestorId = request.InvestorId,
            ClientId = request.ClientId,

            PrincipalAmount = request.PrincipalAmount,
            InstallmentAmount = request.InstallmentAmount,
            TotalInstallments = request.TotalInstallments,
            PaymentFrequency = request.PaymentFrequency,
            LateFeeEnabled =
                lateFeePolicy.Enabled,

            LateFeeCalculationType =
                lateFeePolicy.CalculationType,

            LateFeeAmount =
                lateFeePolicy.Amount,

            LateFeeGraceDays =
                lateFeePolicy.GraceDays,

            StartDate = startDate,
            NextPaymentDate = CalculateNextPaymentDate(
                startDate,
                request.PaymentFrequency),

            Status = LoanStatus.Active,
            Notes = NormalizeOptional(request.Notes),

            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _loanRepository.AddAsync(
            loan,
            cancellationToken);

        await _loanRepository.SaveChangesAsync(
            cancellationToken);

        return Map(loan);
    }

    public async Task<List<LoanResponse>> GetAllAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var loans = await _loanRepository.GetAllAsync(
            tenantId,
            cancellationToken);

        return loans
            .Select(Map)
            .ToList();
    }

    public async Task<LoanResponse?> GetByIdAsync(
        Guid tenantId,
        Guid loanId,
        CancellationToken cancellationToken = default)
    {
        var loan = await _loanRepository.GetByIdAsync(
            tenantId,
            loanId,
            cancellationToken);

        return loan is null
            ? null
            : Map(loan);
    }

    public async Task<LoanResponse?> UpdateAsync(
        Guid tenantId,
        Guid loanId,
        UpdateLoanRequest request,
        CancellationToken cancellationToken = default)
    {
        var loan = await _loanRepository.GetByIdForUpdateAsync(
            tenantId,
            loanId,
            cancellationToken);

        if (loan is null)
        {
            return null;
        }

        if (loan.Status != LoanStatus.Active)
        {
            throw new InvalidOperationException(
                "Only active loans can be updated.");
        }

        var startDate = NormalizeUtc(request.StartDate);

        loan.PrincipalAmount = request.PrincipalAmount;
        loan.InstallmentAmount = request.InstallmentAmount;
        loan.TotalInstallments = request.TotalInstallments;
        loan.PaymentFrequency = request.PaymentFrequency;
        loan.StartDate = startDate;

        loan.NextPaymentDate = CalculateNextPaymentDate(
            startDate,
            request.PaymentFrequency);

        loan.Notes = NormalizeOptional(request.Notes);
        loan.UpdatedAt = DateTime.UtcNow;

        await _loanRepository.SaveChangesAsync(
            cancellationToken);

        return Map(loan);
    }

    public async Task<bool> CancelAsync(
        Guid tenantId,
        Guid loanId,
        CancellationToken cancellationToken = default)
    {
        var loan = await _loanRepository.GetByIdForUpdateAsync(
            tenantId,
            loanId,
            cancellationToken);

        if (loan is null)
        {
            return false;
        }

        if (loan.Status != LoanStatus.Active)
        {
            throw new InvalidOperationException(
                "Only active loans can be cancelled.");
        }

        loan.Status = LoanStatus.Cancelled;
        loan.UpdatedAt = DateTime.UtcNow;

        await _loanRepository.SaveChangesAsync(
            cancellationToken);

        return true;
    }

    public async Task<LoanFinancialSummaryResponse?> GetFinancialSummaryAsync(
        Guid tenantId,
        Guid loanId,
        CancellationToken cancellationToken = default)
    {
        await _lateFeeAccrualService.AccrueForLoanAsync(
            tenantId,
            loanId,
            DateTime.UtcNow,
            cancellationToken);

        var loan = await _loanRepository.GetByIdAsync(
            tenantId,
            loanId,
            cancellationToken);

        if (loan is null)
        {
            return null;
        }

        var totalAmount =
            loan.InstallmentAmount * loan.TotalInstallments;

        var interestAmount =
            totalAmount - loan.PrincipalAmount;

        var amountPaid =
            await _paymentAllocationRepository.GetTotalAppliedToLoanAsync(
                tenantId,
                loanId,
                cancellationToken);

        var balance =
            totalAmount - amountPaid;

        if (balance < 0)
        {
            balance = 0;
        }

        var lateFeeBalance =
            await _lateFeeBalanceService.GetOutstandingBalanceAsync(
                tenantId,
                loanId,
                cancellationToken);

        var totalOutstanding =
            balance + lateFeeBalance;

        var hasOutstandingLateFees =
            lateFeeBalance > 0;

        var completedInstallments = 0;
        var remainingInstallments = loan.TotalInstallments;

        var currentInstallmentPaidAmount = 0m;
        var currentInstallmentRemainingAmount = 0m;
        var nextInstallmentAmountDue = 0m;

        if (loan.InstallmentAmount > 0)
        {
            completedInstallments =
                (int)Math.Floor(
                    amountPaid / loan.InstallmentAmount);

            if (completedInstallments >
                loan.TotalInstallments)
            {
                completedInstallments =
                    loan.TotalInstallments;
            }

            remainingInstallments =
                loan.TotalInstallments -
                completedInstallments;

            if (balance > 0)
            {
                currentInstallmentPaidAmount =
                    amountPaid % loan.InstallmentAmount;

                currentInstallmentRemainingAmount =
                    loan.InstallmentAmount -
                    currentInstallmentPaidAmount;

                /*
                * La última cuota nunca debe solicitar más
                * que el saldo real pendiente.
                */
                nextInstallmentAmountDue =
                    Math.Min(
                        currentInstallmentRemainingAmount,
                        balance);
            }
        }

        var percentagePaid = 0m;

        if (totalAmount > 0)
        {
            percentagePaid =
                Math.Round(
                    (amountPaid / totalAmount) * 100m,
                    2);

            if (percentagePaid > 100m)
            {
                percentagePaid = 100m;
            }
        }

        var isOverdue = false;
        var daysOverdue = 0;
        var overdueInstallments = 0;
        var overdueAmount = 0m;

        if (loan.Status == LoanStatus.Active)
        {
            var today = DateTime.UtcNow.Date;
            var nextPaymentDate = loan.NextPaymentDate.Date;

            if (nextPaymentDate < today)
            {
                isOverdue = true;

                daysOverdue =
                    (today - nextPaymentDate).Days;

                var expectedInstallments =
                    CalculateExpectedInstallments(
                        loan.StartDate,
                        today,
                        loan.PaymentFrequency,
                        loan.TotalInstallments);

                overdueInstallments =
                    expectedInstallments -
                    completedInstallments;

                if (overdueInstallments < 0)
                {
                    overdueInstallments = 0;
                }

                var expectedAmountPaid =
                    expectedInstallments *
                    loan.InstallmentAmount;

                overdueAmount =
                    expectedAmountPaid -
                    amountPaid;

                if (overdueAmount < 0)
                {
                    overdueAmount = 0;
                }

                if (overdueAmount > balance)
                {
                    overdueAmount = balance;
                }
            }
        }

        var totalOverdueAmountDue =
            overdueAmount + lateFeeBalance;

        return new LoanFinancialSummaryResponse
        {
            LoanId = loan.Id,

            PrincipalAmount =
                loan.PrincipalAmount,

            TotalAmount =
                totalAmount,

            InterestAmount =
                interestAmount,

            AmountPaid =
                amountPaid,

            Balance =
                balance,

            LateFeeBalance =
                lateFeeBalance,

            TotalOutstanding =
                totalOutstanding,

            InstallmentAmount =
                loan.InstallmentAmount,

            TotalInstallments =
                loan.TotalInstallments,

            CompletedInstallments =
                completedInstallments,

            RemainingInstallments =
                remainingInstallments,

            CurrentInstallmentPaidAmount =
                currentInstallmentPaidAmount,

            CurrentInstallmentRemainingAmount =
                currentInstallmentRemainingAmount,

            NextInstallmentAmountDue =
                nextInstallmentAmountDue,

            PercentagePaid =
                percentagePaid,

            IsOverdue =
                isOverdue,

            DaysOverdue =
                daysOverdue,

            OverdueInstallments =
                overdueInstallments,

            OverdueAmount =
                overdueAmount,

            TotalOverdueAmountDue =
                totalOverdueAmountDue,

            HasOutstandingLateFees =
                hasOutstandingLateFees,

            PaymentFrequency =
                loan.PaymentFrequency,

            StartDate =
                loan.StartDate,

            NextPaymentDate =
                loan.NextPaymentDate,

            Status =
                loan.Status
        };
    }

    private static DateTime CalculateNextPaymentDate(
        DateTime startDate,
        PaymentFrequency frequency)
    {
        return frequency switch
        {
            PaymentFrequency.Daily =>
                startDate.AddDays(1),

            PaymentFrequency.Weekly =>
                startDate.AddDays(7),

            PaymentFrequency.Biweekly =>
                startDate.AddDays(14),

            PaymentFrequency.Monthly =>
                startDate.AddMonths(1),

            _ => throw new InvalidOperationException(
                "Invalid payment frequency.")
        };
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        if (value.Kind == DateTimeKind.Utc)
        {
            return value;
        }

        if (value.Kind == DateTimeKind.Local)
        {
            return value.ToUniversalTime();
        }

        return DateTime.SpecifyKind(
            value,
            DateTimeKind.Utc);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static LoanResponse Map(Loan loan)
    {
        var totalAmount =
            loan.InstallmentAmount * loan.TotalInstallments;

        var interestAmount =
            totalAmount - loan.PrincipalAmount;

        return new LoanResponse
        {
            Id = loan.Id,
            TenantId = loan.TenantId,
            InvestorId = loan.InvestorId,
            ClientId = loan.ClientId,

            PrincipalAmount = loan.PrincipalAmount,
            InstallmentAmount = loan.InstallmentAmount,
            TotalInstallments = loan.TotalInstallments,

            TotalAmount = totalAmount,
            InterestAmount = interestAmount,

            PaymentFrequency = loan.PaymentFrequency,

            LateFeeEnabled =
                loan.LateFeeEnabled,

            LateFeeCalculationType =
                loan.LateFeeCalculationType,

            LateFeeAmount =
                loan.LateFeeAmount,

            LateFeeGraceDays =
                loan.LateFeeGraceDays,

            StartDate = loan.StartDate,
            NextPaymentDate = loan.NextPaymentDate,

            Status = loan.Status,
            Notes = loan.Notes,

            CreatedAt = loan.CreatedAt,
            UpdatedAt = loan.UpdatedAt
        };
    }

    private static (
        bool Enabled,
        LateFeeCalculationType CalculationType,
        decimal Amount,
        int GraceDays)
        ResolveLateFeePolicy(
            CreateLoanRequest request,
            Tenant tenant)
    {
        var enabled =
            request.LateFeeEnabled
            ?? tenant.DefaultLateFeeEnabled;

        var calculationType =
            request.LateFeeCalculationType
            ?? tenant.DefaultLateFeeCalculationType;

        var amount =
            request.LateFeeAmount
            ?? tenant.DefaultLateFeeAmount;

        var graceDays =
            request.LateFeeGraceDays
            ?? tenant.DefaultLateFeeGraceDays;

        ValidateLateFeePolicy(
            enabled,
            calculationType,
            amount,
            graceDays);

        return (
            enabled,
            calculationType,
            amount,
            graceDays);
    }

    private static void ValidateLateFeePolicy(
        bool enabled,
        LateFeeCalculationType calculationType,
        decimal amount,
        int graceDays)
    {
        if (!Enum.IsDefined(
                typeof(LateFeeCalculationType),
                calculationType))
        {
            throw new InvalidOperationException(
                "Late fee calculation type is invalid.");
        }

        if (
            calculationType !=
            LateFeeCalculationType.FixedAmountPerInstallment)
        {
            throw new InvalidOperationException(
                "Only fixed late fees are currently supported.");
        }

        if (amount < 0)
        {
            throw new InvalidOperationException(
                "Late fee amount cannot be negative.");
        }

        if (graceDays < 0)
        {
            throw new InvalidOperationException(
                "Late fee grace days cannot be negative.");
        }

        if (enabled && amount <= 0)
        {
            throw new InvalidOperationException(
                "Late fee amount must be greater than zero when late fees are enabled.");
        }
    }

    private static DateTime AddFrequency(
        DateTime date,
        PaymentFrequency frequency)
    {
        return frequency switch
        {
            PaymentFrequency.Daily =>
                date.AddDays(1),

            PaymentFrequency.Weekly =>
                date.AddDays(7),

            PaymentFrequency.Biweekly =>
                date.AddDays(14),

            PaymentFrequency.Monthly =>
                date.AddMonths(1),

            _ => throw new InvalidOperationException(
                "Invalid payment frequency.")
        };
    }

    private static int CalculateExpectedInstallments(
        DateTime startDate,
        DateTime today,
        PaymentFrequency frequency,
        int totalInstallments)
    {
        var expectedInstallments = 0;

        var dueDate =
            AddFrequency(
                startDate,
                frequency);

        while (
            dueDate.Date <= today.Date &&
            expectedInstallments < totalInstallments)
        {
            expectedInstallments++;

            dueDate =
                AddFrequency(
                    dueDate,
                    frequency);
        }

        return expectedInstallments;
    }

    public async Task<List<ActiveLoanPortfolioItemResponse>> GetActivePortfolioAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(
            tenantId,
            cancellationToken);

        if (tenant is null || !tenant.IsActive)
        {
            throw new InvalidOperationException(
                "Tenant not found or inactive.");
        }

        await _lateFeeAccrualService.AccrueForTenantAsync(
            tenantId,
            DateTime.UtcNow,
            cancellationToken);

        var loans = await _loanRepository.GetActiveByTenantAsync(
            tenantId,
            cancellationToken);

        var loanIds = loans
            .Select(x => x.Id)
            .ToList();

        var totalPaidByLoan =
            await _paymentAllocationRepository.GetTotalAppliedToLoansAsync(
                tenantId,
                loanIds,
                cancellationToken);

        var lateFeeBalanceByLoan =
            await _lateFeeBalanceService
                .GetOutstandingBalancesByLoansAsync(
                    tenantId,
                    loanIds,
                    cancellationToken);

        var result = new List<ActiveLoanPortfolioItemResponse>();

        var today = DateTime.UtcNow.Date;

        foreach (var loan in loans)
        {
            var totalAmount =
                loan.InstallmentAmount * loan.TotalInstallments;

            var amountPaid =
                totalPaidByLoan.TryGetValue(
                    loan.Id,
                    out var paid)
                    ? paid
                    : 0m;

            var balance =
                totalAmount - amountPaid;

            if (balance < 0)
            {
                balance = 0;
            }

            var lateFeeBalance =
                lateFeeBalanceByLoan.TryGetValue(
                    loan.Id,
                    out var outstandingLateFees)
                    ? outstandingLateFees
                    : 0m;

            var totalOutstanding =
                balance + lateFeeBalance;

            var hasOutstandingLateFees =
                lateFeeBalance > 0;

            var completedInstallments = 0;
            var remainingInstallments = loan.TotalInstallments;
            var currentInstallmentPaidAmount = 0m;
            var currentInstallmentRemainingAmount = 0m;
            var nextInstallmentAmountDue = 0m;

            if (loan.InstallmentAmount > 0)
            {
                completedInstallments =
                    (int)Math.Floor(
                        amountPaid / loan.InstallmentAmount);

                if (completedInstallments >
                    loan.TotalInstallments)
                {
                    completedInstallments =
                        loan.TotalInstallments;
                }

                remainingInstallments =
                    loan.TotalInstallments -
                    completedInstallments;

                if (balance > 0)
                {
                    currentInstallmentPaidAmount =
                        amountPaid % loan.InstallmentAmount;

                    currentInstallmentRemainingAmount =
                        loan.InstallmentAmount -
                        currentInstallmentPaidAmount;

                    nextInstallmentAmountDue =
                        Math.Min(
                            currentInstallmentRemainingAmount,
                            balance);
                }
            }

            var percentagePaid = 0m;

            if (totalAmount > 0)
            {
                percentagePaid =
                    Math.Round(
                        (amountPaid / totalAmount) * 100m,
                        2);

                if (percentagePaid > 100m)
                {
                    percentagePaid = 100m;
                }
            }

            var isOverdue = false;
            var daysOverdue = 0;
            var overdueInstallments = 0;
            var overdueAmount = 0m;

            var nextPaymentDate =
                loan.NextPaymentDate.Date;

            if (nextPaymentDate < today)
            {
                isOverdue = true;

                daysOverdue =
                    (today - nextPaymentDate).Days;

                var expectedInstallments =
                    CalculateExpectedInstallments(
                        loan.StartDate,
                        today,
                        loan.PaymentFrequency,
                        loan.TotalInstallments);

                overdueInstallments =
                    expectedInstallments -
                    completedInstallments;

                if (overdueInstallments < 0)
                {
                    overdueInstallments = 0;
                }

                var expectedAmountPaid =
                    expectedInstallments *
                    loan.InstallmentAmount;

                overdueAmount =
                    expectedAmountPaid -
                    amountPaid;

                if (overdueAmount < 0)
                {
                    overdueAmount = 0;
                }

                if (overdueAmount > balance)
                {
                    overdueAmount = balance;
                }
            }

            var totalOverdueAmountDue =
                overdueAmount + lateFeeBalance;

            result.Add(
                new ActiveLoanPortfolioItemResponse
                {
                    LoanId = loan.Id,
                    InvestorId = loan.InvestorId,
                    ClientId = loan.ClientId,

                    ClientName = string.Join(
                        " ",
                        new[]
                        {
                            loan.Client.FirstName,
                            loan.Client.LastName
                        }
                        .Where(x => !string.IsNullOrWhiteSpace(x))),

                    ClientPhone = loan.Client.Phone,

                    ClientAddress = loan.Client.Address,
                    
                    PrincipalAmount =
                        loan.PrincipalAmount,

                    TotalAmount =
                        totalAmount,

                    AmountPaid =
                        amountPaid,

                    Balance =
                        balance,

                    LateFeeBalance =
                        lateFeeBalance,

                    TotalOutstanding =
                        totalOutstanding,

                    InstallmentAmount =
                        loan.InstallmentAmount,

                    TotalInstallments =
                        loan.TotalInstallments,

                    CompletedInstallments =
                        completedInstallments,

                    RemainingInstallments =
                        remainingInstallments,

                    NextInstallmentAmountDue =
                        nextInstallmentAmountDue,

                    IsOverdue =
                        isOverdue,

                    DaysOverdue =
                        daysOverdue,

                    OverdueInstallments =
                        overdueInstallments,

                    OverdueAmount =
                        overdueAmount,

                    TotalOverdueAmountDue =
                        totalOverdueAmountDue,

                    HasOutstandingLateFees =
                        hasOutstandingLateFees,

                    PercentagePaid =
                        percentagePaid,

                    PaymentFrequency =
                        loan.PaymentFrequency,

                    NextPaymentDate =
                        loan.NextPaymentDate,

                    Status =
                        loan.Status
                });
        }

        return result;
    }

    public async Task<ActivePortfolioSummaryResponse> GetActivePortfolioSummaryAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var portfolio = await GetActivePortfolioAsync(
            tenantId,
            cancellationToken);

        var totalPortfolioAmount =
            portfolio.Sum(x => x.TotalAmount);

        var totalAmountPaid =
            portfolio.Sum(x => x.AmountPaid);

        var collectionPercentage = 0m;

        if (totalPortfolioAmount > 0)
        {
            collectionPercentage = Math.Round(
                (totalAmountPaid / totalPortfolioAmount) * 100m,
                2);
        }

        var totalLateFeeBalance =
            portfolio.Sum(x => x.LateFeeBalance);

        var totalOutstanding =
            portfolio.Sum(x => x.TotalOutstanding);

        var totalOverdueAmountDue =
            portfolio.Sum(x => x.TotalOverdueAmountDue);

        return new ActivePortfolioSummaryResponse
        {
            ActiveLoansCount =
                portfolio.Count,

            TotalPrincipalAmount =
                portfolio.Sum(x => x.PrincipalAmount),

            TotalPortfolioAmount =
                totalPortfolioAmount,

            TotalAmountPaid =
                totalAmountPaid,

            TotalBalance =
                portfolio.Sum(x => x.Balance),

            TotalLateFeeBalance =
                totalLateFeeBalance,

            TotalOutstanding =
                totalOutstanding,

            OverdueLoansCount =
                portfolio.Count(x => x.IsOverdue),

            TotalOverdueAmount =
                portfolio.Sum(x => x.OverdueAmount),

            TotalOverdueAmountDue =
                totalOverdueAmountDue,

            CollectionPercentage =
                collectionPercentage
        };
    }

    public async Task<List<CollectionLoanItemResponse>> GetCollectionPortfolioAsync(
        Guid tenantId,
        bool overdueOnly = false,
        DateTime? collectionDate = null,
        DateTime? dueDate = null,
        string? search = null,
        Guid? investorId = null,
        Guid? clientId = null,
        Guid? collectionRouteId = null,
        CancellationToken cancellationToken = default)
    {
        var portfolio = await GetActivePortfolioAsync(
            tenantId,
            cancellationToken);

        var query = portfolio.AsEnumerable();

        if (overdueOnly)
        {
            query = query.Where(x =>
                x.IsOverdue ||
                x.HasOutstandingLateFees);
        }

        if (collectionDate.HasValue)
        {
            var date = collectionDate.Value.Date;

            query = query.Where(
                x => x.NextPaymentDate.Date <= date);
        }

        if (collectionDate.HasValue)
        {
            var date = collectionDate.Value.Date;

            query = query.Where(x =>
                x.NextPaymentDate.Date <= date ||
                x.HasOutstandingLateFees);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();

            query = query.Where(x =>
                x.ClientName.Contains(
                    normalizedSearch,
                    StringComparison.OrdinalIgnoreCase) ||
                x.ClientPhone.Contains(
                    normalizedSearch,
                    StringComparison.OrdinalIgnoreCase));
        }

        if (investorId.HasValue)
        {
            query = query.Where(
                x => x.InvestorId == investorId.Value);
        }

        if (clientId.HasValue)
        {
            query = query.Where(
                x => x.ClientId == clientId.Value);
        }

        if (collectionRouteId.HasValue)
        {
            var routeClients =
                await _clientRepository.GetAllAsync(
                    tenantId,
                    collectionRouteId,
                    cancellationToken);

            var routeClientIds =
                routeClients
                    .Select(x => x.Id)
                    .ToHashSet();

            query = query.Where(
                x => routeClientIds.Contains(x.ClientId));
        }

        return query
            .OrderByDescending(x =>
                x.IsOverdue ||
                x.HasOutstandingLateFees)
            .ThenBy(x => x.NextPaymentDate)
            .ThenByDescending(x => x.TotalOverdueAmountDue)
            .Select(x => new CollectionLoanItemResponse
            {
                LoanId = x.LoanId,
                ClientId = x.ClientId,
                ClientName = x.ClientName,
                ClientPhone = x.ClientPhone,
                ClientAddress = x.ClientAddress,
                Balance = x.Balance,
                LateFeeBalance =
                    x.LateFeeBalance,
                TotalOutstanding =
                    x.TotalOutstanding,
                InstallmentAmount = x.InstallmentAmount,
                NextInstallmentAmountDue = x.NextInstallmentAmountDue,
                NextPaymentDate = x.NextPaymentDate,
                IsOverdue = x.IsOverdue,
                TotalOverdueAmountDue =
                    x.TotalOverdueAmountDue,
                HasOutstandingLateFees =
                    x.HasOutstandingLateFees,
                DaysOverdue = x.DaysOverdue,
                OverdueInstallments = x.OverdueInstallments,
                OverdueAmount = x.OverdueAmount,
                PercentagePaid = x.PercentagePaid,
                PaymentFrequency = x.PaymentFrequency
            })
            .ToList();
    }

    public async Task<CollectionPortfolioSummaryResponse> GetCollectionPortfolioSummaryAsync(
        Guid tenantId,
        bool overdueOnly = false,
        DateTime? collectionDate = null,
        DateTime? dueDate = null,
        string? search = null,
        Guid? investorId = null,
        Guid? clientId = null,
        Guid? collectionRouteId = null,
        CancellationToken cancellationToken = default)
    {
        var portfolio = await GetCollectionPortfolioAsync(
            tenantId,
            overdueOnly,
            collectionDate,
            dueDate,
            search,
            investorId,
            clientId,
            collectionRouteId,
            cancellationToken);

        return new CollectionPortfolioSummaryResponse
        {
            LoansCount = portfolio.Count,

            ClientsCount = portfolio
                .Select(x => x.ClientId)
                .Distinct()
                .Count(),

            TotalBalance =
                portfolio.Sum(x => x.Balance),

            TotalLateFeeBalance =
                portfolio.Sum(x => x.LateFeeBalance),

            TotalOutstanding =
                portfolio.Sum(x => x.TotalOutstanding),

            TotalNextInstallmentAmountDue =
                portfolio.Sum(x => x.NextInstallmentAmountDue),

            OverdueLoansCount =
                portfolio.Count(x => x.IsOverdue),

            TotalOverdueAmount =
                portfolio.Sum(x => x.OverdueAmount),

            TotalOverdueAmountDue =
                portfolio.Sum(x => x.TotalOverdueAmountDue)
        };
    }
}