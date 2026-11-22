namespace BizSpace.Repositories;

public interface IGenericRepository<T> where T : class
{
    IQueryable<T> Query { get; }
    Task<IEnumerable<T>> GetAllAsync();
    Task<T?> GetByIdAsync(int id);
    Task AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
    void DeleteRange(IEnumerable<T> entities);
    Task SaveAsync();
}
