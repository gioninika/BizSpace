using BizSpace.Data;
using Microsoft.EntityFrameworkCore;

namespace BizSpace.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    private readonly StoreDbContext _context;
    private readonly DbSet<T> _dbSet;

    public GenericRepository(StoreDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public IQueryable<T> Query => _dbSet;
    public async Task<IEnumerable<T>> GetAllAsync() => await _dbSet.ToListAsync();
    public async Task<T?> GetByIdAsync(int id) => await _dbSet.FindAsync(id);
    public async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);
    public void Update(T entity) => _dbSet.Update(entity);
    public void Delete(T entity) => _dbSet.Remove(entity);
    public void DeleteRange(IEnumerable<T> entities) => _dbSet.RemoveRange(entities);
    public async Task SaveAsync() => await _context.SaveChangesAsync();
}
