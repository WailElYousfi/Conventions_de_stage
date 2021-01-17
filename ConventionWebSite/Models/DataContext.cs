using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace ConventionWebSite.Models
{
    public class DataContext : DbContext
    {
        public DataContext() : base("StringDBContext")
        {
            Database.SetInitializer<DataContext>(new CreateDatabaseIfNotExists<DataContext>());
        }
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasMany(x => x.ConventionsProcessed).WithOptional(x => x.Employee).HasForeignKey(x => x.EmployeeId).WillCascadeOnDelete(false);
            modelBuilder.Entity<User>().HasMany(x => x.ConventionsRequested).WithOptional(x => x.Student).HasForeignKey(x => x.StudentId).WillCascadeOnDelete(false);
        }

        public DbSet<User> users { get; set; }
        public DbSet<Convention> conventions { get; set; }
        public object DeleteBehavior { get; private set; }
    }
}