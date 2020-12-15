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
    public interface IStudentRepository
    {
        Student GetById(int id);

        /// <summary>
        /// DbContext used by entities in this repository. We expose this as public because
        /// the class into which this repo is injected might have needs beyond what this simple
        /// repo interface can provide. This way, that class can use the supplied DbContext
        /// and DbSet to provide additional functionality not yet anticipated.
        /// </summary>
        SchoolContext _context { get; set; }

        DbSet<Student> _entities { get; set; }
        IQueryable<Student> GetAll();
        void Insert(Student entity);
        void Update(Student entity);
        void Delete(Int32 id);
        void Add(params Student[] items);
        void CallStoredProc(string name, params Tuple<string, string>[] parameters);
        IList<Student> GetAll(params Expression<Func<Student, object>>[] navigationProperties);
        IList<Student> GetList(Expression<Func<Student, bool>> where, params Expression<Func<Student, object>>[] navigationProperties);
        Student GetSingle(Expression<Func<Student, bool>> where, params Expression<Func<Student, object>>[] navigationProperties);
        void Remove(params Student[] items);
        void Update(params Student[] items);
    }

    public class StudentRepository : IntIdRepositoryBase<Student, SchoolContext>, IStudentRepository
    {
    }
}
