﻿using System;
using System.Diagnostics;

using ConfigurationAssistant;

using EFSupport;

using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Models
{
    public interface IDSuiteContext
    {
        DbSet<Car> Cars { get; set; }
    }

    public partial class DSuiteContext : DbContext, IDSuiteContext
    {
        /// <summary>
        /// NOTE:!!!!!!! Both parameterless and DbContextOptions constructors MUST exist or Migarations will FAIL!!!!!
        /// </summary>
        public DSuiteContext()
        {
        }

        /// <summary>
        /// NOTE:!!!!!!! Both parameterless and DbContextOptions constructors MUST exist or Migarations will FAIL!!!!!
        /// </summary>
        /// <param name="options"></param>
        public DSuiteContext(DbContextOptions<DSuiteContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Car> Cars { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning We use ConfigurationAssistant to retrieve connection string information from appsettings.json or secrets.json or environment variables. So no hard coded information goes here!
                this.ConfigureSqlServer(optionsBuilder);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
