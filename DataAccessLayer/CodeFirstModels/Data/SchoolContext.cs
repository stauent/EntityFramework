﻿using ConfigurationAssistant;
using EFSupport;

using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.CodeFirstModels.Data
{
    public partial class SchoolContext : DbContext
    {
        /// <summary>
        /// NOTE:!!!!!!! Both parameterless and DbContextOptions constructors MUST exist or Migarations will FAIL!!!!!
        /// </summary>
        public SchoolContext()
        {
        }

        /// <summary>
        /// NOTE:!!!!!!! Both parameterless and DbContextOptions constructors MUST exist or Migarations will FAIL!!!!!
        /// </summary>
        /// <param name="options"></param>
        public SchoolContext(DbContextOptions<SchoolContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning We use ConfigurationAssistant to retrieve connection string information from appsettings.json or secrets.json or environment variables. So no hard coded information goes here!
                optionsBuilder.UseSqlServer(ConfigFactory.UserConfiguration.ConnectionString(this.DBNameFromContext()));
            }
        }

        public DbSet<Course> Courses { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Student> Students { get; set; }

        /// <summary>
        /// Ensure the table names are NOT pluralized
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Course>().ToTable("Course");
            modelBuilder.Entity<Enrollment>().ToTable("Enrollment");
            modelBuilder.Entity<Student>().ToTable("Student");
        }
    }
}