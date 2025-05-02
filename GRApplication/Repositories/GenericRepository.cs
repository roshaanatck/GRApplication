// File: Repositories/GenericRepository.cs
using Microsoft.EntityFrameworkCore;
using GRApplication.Data; // Assuming your ApplicationDbContext is in this namespace
using GRApplication.Interfaces; // Assuming your IGenericRepository is in this namespace
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks; // Add this
using static CKPortal.Interfaces.IGenericRepository; // For nested interface access

namespace CKPortal.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        private ApplicationDbContext _context; // Remove "= null" if always injected
        private DbSet<T> table;
        private bool _disposed = false;

        // Prefer constructor injection for DbContext in web apps
        public GenericRepository(ApplicationDbContext context) // Use injected context
        {
            _context = context;
            table = _context.Set<T>();
        }

        // --- Sync Methods (Keep or Remove) ---
        public IEnumerable<T> GetAll() { return table.ToList(); }
        public IEnumerable<T> GetAll(params Expression<Func<T, object>>[] includeProperties)
        {
            IQueryable<T> query = table;
            foreach (var includeProperty in includeProperties) { query = query.Include(includeProperty); }
            return query.ToList();
        }
        public T GetById(object id) { return table.Find(id); } // Find is sync
        public void Insert(T obj) { table.Add(obj); }
        public void Update(T obj) { table.Attach(obj); _context.Entry(obj).State = EntityState.Modified; }
        public void Delete(object id) { T existing = table.Find(id); if (existing != null) table.Remove(existing); }
        public void Save() { _context.SaveChanges(); }

        // --- Async Methods ---
        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await table.ToListAsync();
        }

        public async Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[] includeProperties)
        {
            IQueryable<T> query = table;
            foreach (var includeProperty in includeProperties) { query = query.Include(includeProperty); }
            return await query.ToListAsync();
        }

        public async Task<T?> GetByIdAsync(object id)
        {
            // FindAsync works best with primary key(s)
            // Ensure 'id' is of the correct type or cast appropriately if FindAsync requires specific types
            if (id is object[] keyValues) // Handle composite keys if needed
            {
                return await table.FindAsync(keyValues);
            }
            else
            {
                return await table.FindAsync(id);
            }
        }

        public async Task InsertAsync(T obj)
        {
            await table.AddAsync(obj);
        }

        public async Task DeleteAsync(object id)
        {
            T? existing = await GetByIdAsync(id); // Use async version
            if (existing != null)
            {
                table.Remove(existing);
            }
        }

        public async Task<int> SaveAsync()
        {
            return await _context.SaveChangesAsync();
        }

        // --- Dispose Pattern ---
        protected virtual void Dispose(bool disposing) { /* ... same as before ... */ }
        public void Dispose() { /* ... same as before ... */ }
    }
}