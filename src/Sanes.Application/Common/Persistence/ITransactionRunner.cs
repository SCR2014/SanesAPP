namespace Sanes.Application.Common.Persistence;

public interface ITransactionRunner
{
    Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);
}