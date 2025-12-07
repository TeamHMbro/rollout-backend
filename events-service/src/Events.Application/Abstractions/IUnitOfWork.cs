using System;
using System.Threading;
using System.Threading.Tasks;

namespace Events.Application.Abstractions;

public interface IUnitOfWork
{
    Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct);
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct);
}
