// File: Interfaces/IGenericRepository.cs
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks; // Add this

// Keep the nested structure if that's your standard
public interface IGenericRepository
{
    public interface IGenericRepository<T> : IDisposable where T : class
    {
        // Keep sync versions if needed for backward compatibility, or remove them
        IEnumerable<T> GetAll();
        T GetById(object id);
        void Insert(T obj);
        void Update(T obj);
        void Delete(object id);
        void Save(); // Sync Save

        // --- Add Async Versions ---
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[] includeProperties);
        Task<T?> GetByIdAsync(object id); // Return nullable T for FindAsync
        Task InsertAsync(T obj);
        // Update is often synchronous in EF Core before SaveChangesAsync
        // Delete can often just use FindAsync + Remove (sync) before SaveChangesAsync
        Task DeleteAsync(object id);
        Task<int> SaveAsync(); // Async Save returns number of changes
    }
}