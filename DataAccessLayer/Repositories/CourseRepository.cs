using DataAccessLayer.CodeFirstModels;
using DataAccessLayer.CodeFirstModels.Data;

using EFSupport;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories
{
    public interface ICourseRepository
    {
        Course GetById(int id);

        /// <summary>
        /// DbContext used by entities in this repository. We expose this as public because
        /// the class into which this repo is injected might have needs beyond what this simple
        /// repo interface can provide. This way, that class can use the supplied DbContext
        /// and DbSet to provide additional functionality not yet anticipated.
        /// </summary>
        SchoolContext _context { get; set; }

        DbSet<Course> _entities { get; set; }
        IQueryable<Course> GetAll();
        void Insert(Course entity);
        void Update(Course entity);
        void Delete(Int32 id);
        void Add(params Course[] items);
        void CallStoredProc(string name, params Tuple<string, string>[] parameters);
        IList<Course> GetAll(params Expression<Func<Course, object>>[] navigationProperties);
        IList<Course> GetList(Expression<Func<Course, bool>> where, params Expression<Func<Course, object>>[] navigationProperties);
        Course GetSingle(Expression<Func<Course, bool>> where, params Expression<Func<Course, object>>[] navigationProperties);
        void Remove(params Course[] items);
        void Update(params Course[] items);
    }

    public class CourseRepository : EFGenericRepository<Course, int, SchoolContext>, ICourseRepository
    {
        public override Course GetById(int id)
        {
            return _entities.SingleOrDefault(s => s.CourseID == id);
        }
    }
}
