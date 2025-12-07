using System;
using System.Threading;
using System.Threading.Tasks;
using Events.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Events.Infrastructure;

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly EventDbContext _dbContext;

    public EfUnitOfWork(EventDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
        await action(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
        var result = await action(ct);
        await transaction.CommitAsync(ct);
        return result;
    }
}
