using System.Data;
using BizSpace.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BizSpace.Repositories;

public sealed class UnitOfWork(StoreDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);

    public IDbContextTransaction BeginTransaction(IsolationLevel isolationLevel) =>
        context.Database.BeginTransaction(isolationLevel);
}
