using System;
using System.Collections.Generic;
using System.Linq;

using DataAccessLayer.Models;
using EFSupport;

namespace DataAccessLayer.Repositories
{
    public interface ICarRepository 
    {
        Car GetById(int id);
        IQueryable<Car> GetAll();
        void Insert(Car entity);
        void Update(Car entity);
        void Delete(Int32 id);
    }

    public class CarRepository : GenericRepository<Car, int, DSuiteContext>, ICarRepository
    {
        public override Car GetById(int id)
        {
            return entities.SingleOrDefault(s => s.CarId == id);
        }
    }
}
