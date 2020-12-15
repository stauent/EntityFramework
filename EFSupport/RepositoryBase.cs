using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;


namespace EFSupport
{

    public interface IIntIdRepositoryBase
    {
        public int Id { get; set; }
    }

    /// <summary>
    /// Base repository class for any entity that has an in Id property as a primary key
    /// </summary>
    /// <typeparam name="T">Type of entity</typeparam>
    /// <typeparam name="TContext">DbContext the entity belongs to</typeparam>
    public class IntIdRepositoryBase<T, TContext> : EFGenericRepository<T, int, TContext> where T : class, IIntIdRepositoryBase where TContext : DbContext, new()
    {
        public int Id { get; set; }
        public override T GetById(int id)
        {
            return _entities.SingleOrDefault(s => s.Id == id);
        }
    }

    public interface IUniqueIdentifierIdRepositoryBase
    {
        public Guid Id { get; set; }
    }

    /// <summary>
    /// Base repository class for any entity that has an in Guid property as a primary key
    /// </summary>
    /// <typeparam name="T">Type of entity</typeparam>
    /// <typeparam name="TContext">DbContext the entity belongs to</typeparam>
    public class UniqueIdentifierIdRepositoryBase<T, TContext> : EFGenericRepository<T, Guid, TContext> where T : class, IUniqueIdentifierIdRepositoryBase where TContext : DbContext, new()
    {
        public Guid Id { get; set; }
        public override T GetById(Guid id)
        {
            return _entities.SingleOrDefault(s => s.Id == id);
        }
    }

    public interface IStringIdentifierIdRepositoryBase
    {
        public string Id { get; set; }
    }

    /// <summary>
    /// Base repository class for any entity that has an in Guid property as a primary key
    /// </summary>
    /// <typeparam name="T">Type of entity</typeparam>
    /// <typeparam name="TContext">DbContext the entity belongs to</typeparam>
    public class StringIdentifierIdRepositoryBase<T, TContext> : EFGenericRepository<T, string, TContext> where T : class, IStringIdentifierIdRepositoryBase where TContext : DbContext, new()
    {
        public string Id { get; set; }
        public override T GetById(string id)
        {
            return _entities.SingleOrDefault(s => s.Id == id);
        }
    }

}
