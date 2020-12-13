using DataAccessLayer.CodeFirstModels;
using DataAccessLayer.CodeFirstModels.Data;

using EFSupport;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataAccessLayer.Repositories
{

    public interface ICourseRepository 
    {
        Course GetById(int id);
        IQueryable<Course> GetAll();
        void Insert(Course entity);
        void Update(Course entity);
        void Delete(Int32 id);
    }

    public class CourseRepository : GenericRepository<Course, int, SchoolContext>, ICourseRepository
    {
        public override Course GetById(int id)
        {
            return entities.SingleOrDefault(s => s.CourseID == id);
        }
    }
}
