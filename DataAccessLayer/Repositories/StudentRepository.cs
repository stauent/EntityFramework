using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataAccessLayer.CodeFirstModels;
using DataAccessLayer.CodeFirstModels.Data;
using EFSupport;

namespace DataAccessLayer.Repositories
{
    public interface IStudentRepository
    {
        Student GetById(int id);
        IQueryable<Student> GetAll();
        void Insert(Student entity);
        void Update(Student entity);
        void Delete(Int32 id);
    }

    public class StudentRepository : GenericRepository<Student, int, SchoolContext>, IStudentRepository
    {
        public StudentRepository(SchoolContext context) : base(context)
        {
        }
        public override Student GetById(int id)
        {
            return entities.SingleOrDefault(s => s.ID == id);
        }
    }

}
