﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataAccessLayer.CodeFirstModels;
using DataAccessLayer.CodeFirstModels.Data;
using EFSupport;

namespace DataAccessLayer.Repositories
{
    public interface IEnrollmentRepository
    {
        Enrollment GetById(int id);
        IQueryable<Enrollment> GetAll();
        void Insert(Enrollment entity);
        void Update(Enrollment entity);
        void Delete(Int32 id);
    }

    public class EnrollmentRepository : GenericRepository<Enrollment, int, SchoolContext>, IEnrollmentRepository
    {
        public override Enrollment GetById(int id)
        {
            return entities.SingleOrDefault(s => s.EnrollmentID == id);
        }
    }
}
