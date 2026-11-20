using System.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace BizSpace.Repositories;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    IDbContextTransaction BeginTransaction(IsolationLevel isolationLevel);
}
