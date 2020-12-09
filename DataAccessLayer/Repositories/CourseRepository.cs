﻿using DataAccessLayer.CodeFirstModels;
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
        IEnumerable<Course> GetAll();
        void Insert(Course entity);
        void Update(Course entity);
        void Delete(Int32 id);
    }

    public class CourseRepository : GenericRepository<Course, int, SchoolContext>, ICourseRepository
    {
        public CourseRepository(SchoolContext context) : base(context)
        {
        }
        public override Course GetById(int id)
        {
            return entities.SingleOrDefault(s => s.CourseID == id);
        }
    }
}