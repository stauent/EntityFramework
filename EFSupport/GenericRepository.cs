using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace EFSupport
{

    public interface IGenericRepository<T,TKey> 
    {
        public IEnumerable<T> GetAll();
        public T GetById(TKey id);
        public void Insert(T entity);
        public void Update(T entity);
        public void Delete(TKey id);
    }

    public abstract class GenericRepository<T, TKey, TContext> : IGenericRepository<T, TKey> where T: class where TContext:DbContext
    {
        protected readonly TContext context;
        protected DbSet<T> entities;
        string errorMessage = string.Empty;

        public GenericRepository(TContext context)
        {
            this.context = context;
            entities = context.Set<T>();
        }
        public IEnumerable<T> GetAll()
        {
            return entities.AsEnumerable();
        }

        public abstract T GetById(TKey id);
        //{
        //    return entities.SingleOrDefault(s => s.Id == id);
        //}

        public void Insert(T entity)
        {
            if (entity == null) throw new ArgumentNullException("entity");

            entities.Add(entity);
            context.SaveChanges();
        }
        public void Update(T entity)
        {
            if (entity == null) throw new ArgumentNullException("entity");
            context.SaveChanges();
        }
        public void Delete(TKey id)
        {
            T entity = GetById(id);
            entities.Remove(entity);
            context.SaveChanges();
        }
    }
}
