using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using DataAccessLayer.CodeFirstModels;
using DataAccessLayer.CodeFirstModels.Data;
using EFSupport;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories
{
    public interface IEnrollmentRepository
    {
        Enrollment GetById(int id);

        /// <summary>
        /// DbContext used by entities in this repository. We expose this as public because
        /// the class into which this repo is injected might have needs beyond what this simple
        /// repo interface can provide. This way, that class can use the supplied DbContext
        /// and DbSet to provide additional functionality not yet anticipated.
        /// </summary>
        SchoolContext _context { get; set; }

        DbSet<Enrollment> _entities { get; set; }
        IQueryable<Enrollment> GetAll();
        void Insert(Enrollment entity);
        void Update(Enrollment entity);
        void Delete(Int32 id);
        void Add(params Enrollment[] items);
        void CallStoredProc(string name, params Tuple<string, string>[] parameters);
        IList<Enrollment> GetAll(params Expression<Func<Enrollment, object>>[] navigationProperties);
        IList<Enrollment> GetList(Expression<Func<Enrollment, bool>> where, params Expression<Func<Enrollment, object>>[] navigationProperties);
        Enrollment GetSingle(Expression<Func<Enrollment, bool>> where, params Expression<Func<Enrollment, object>>[] navigationProperties);
        void Remove(params Enrollment[] items);
        void Update(params Enrollment[] items);
    }

    public class EnrollmentRepository : EFGenericRepository<Enrollment, int, SchoolContext>, IEnrollmentRepository
    {
        public override Enrollment GetById(int id)
        {
            return _entities.SingleOrDefault(s => s.EnrollmentID == id);
        }
    }
}
