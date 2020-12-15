using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using DataAccessLayer.Models;
using EFSupport;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories
{
    public interface ICarRepository
    {
        Car GetById(int id);

        /// <summary>
        /// DbContext used by entities in this repository. We expose this as public because
        /// the class into which this repo is injected might have needs beyond what this simple
        /// repo interface can provide. This way, that class can use the supplied DbContext
        /// and DbSet to provide additional functionality not yet anticipated.
        /// </summary>
        DSuiteContext _context { get; set; }

        DbSet<Car> _entities { get; set; }
        IQueryable<Car> GetAll();
        void Insert(Car entity);
        void Update(Car entity);
        void Delete(Int32 id);
        void Add(params Car[] items);
        void CallStoredProc(string name, params Tuple<string, string>[] parameters);
        IList<Car> GetAll(params Expression<Func<Car, object>>[] navigationProperties);
        IList<Car> GetList(Expression<Func<Car, bool>> where, params Expression<Func<Car, object>>[] navigationProperties);
        Car GetSingle(Expression<Func<Car, bool>> where, params Expression<Func<Car, object>>[] navigationProperties);
        void Remove(params Car[] items);
        void Update(params Car[] items);
    }

    /// <summary>
    /// Can't use IntIdRepositoryBase as base class because the key field is uniquely named
    /// </summary>
    public class CarRepository : EFGenericRepository<Car, int, DSuiteContext>, ICarRepository
    {
        public override Car GetById(int id)
        {
            return _entities.SingleOrDefault(s => s.CarId == id);
        }
    }
}
