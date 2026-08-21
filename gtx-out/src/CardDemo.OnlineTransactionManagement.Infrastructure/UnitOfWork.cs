using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using CardDemo.OnlineTransactionManagement.Application;
using CardDemo.OnlineTransactionManagement.Data;

namespace CardDemo.OnlineTransactionManagement.Infrastructure;

/// <summary>
/// Unit of work backed by a real database transaction at READ COMMITTED isolation,
/// relying on row-level FOR UPDATE locks for id-generation safety.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly OnlineTransactionManagementDbContext _db;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(OnlineTransactionManagementDbContext db)
    {
        _db = db;
    }

    public async Task BeginAsync(CancellationToken ct)
    {
        if (_transaction is not null)
            return;

        _transaction = await _db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.ReadCommitted, ct);
    }

    public async Task CommitAsync(CancellationToken ct)
    {
        if (_transaction is null)
            throw new InvalidOperationException("No transaction to commit.");

        await _db.SaveChangesAsync(ct);
        await _transaction.CommitAsync(ct);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackAsync(CancellationToken ct)
    {
        if (_transaction is null)
            return;

        await _transaction.RollbackAsync(ct);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public void Dispose()
    {
        _transaction?.Dispose();
    }
}
