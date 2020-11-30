using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DataAccessLayer;
using DataAccessLayer.Models;
using DataAccessLayer.CodeFirstModels;
using EFSupport;


using DataAccessLayer.CodeFirstModels.Data;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ConfigurationAssistant;

namespace EntityFramework
{
    // ------------------------ Instructions for Database First -----------------------------
    // To set up the entities in the DataAccessLayer assembly, open the PackageManagerConsole
    // and select the "Default Project" to be "DataAccessLayer". Set this assembly as the startup project.
    // Now enter the following command to scaffold all the entities:
    //
    //  XXX = Name of your Sql Server Instance
    //  YYY = Your Sql Server User ID
    //  ZZZ = Your Sql Server Password
    //
    //      Scaffold-DbContext  "Data Source = XXX; Initial Catalog = Dsuite; Persist Security Info = True; User ID = YYY; Password=ZZZ;" Microsoft.EntityFrameworkCore.SqlServer -DataAnnotations -OutputDir D:\Humber\EntityFramework\DataAccessLayer\Models\ 
    //
    //  NOTE: If you want to start using Migrations on this existing database, you need to perofrm a few steps. First, add your first migration:
    //        https://cmatskas.com/ef-core-migrations-with-existing-database-schema-and-data/  
    //
    //              Add-Migration DSuiteFirstMigration -Context DSuiteContext
    //
    //      Now, edit the XXXXXDSuiteFirstMigration.cs file and delete ALL the code inside the "Up" method, but don't delete the "Up" method.
    //      Next, update the database by running the migration:
    //
    //              Update-Database -Context DSuiteContext -verbose
    //
    //      This will generate a new table "__EFMigrationsHistory" in your database. From this point forward you can keep your models and database schema
    //      synchronized by modifying your model, creating a new migration, and then call "Update-Database" (as above).
    //
    // -------------------- Instructions for Code First  ---------------------------------
    //
    //  Setting up migrations:
    //
    //      NOTE: The connection string is retrieved using the ConfigFactory helper class. It inspects the "MyProjectSettings" section of the appsettings.json
    //            file to get configuration data. In order for "secrets.json" to properly work in this environment you must ensure that the DataAccessLayer.csproj
    //            has copied the "UserSecretsId" section from the main project .csproj file. The reason is that the secrets.json file is only supposed to be used
    //            in development. So when we run our application under visual studio in development, everything works fine. However, EF Core Migrations runs 
    //            in "Production" context. So, when migrations are run it that context, it will only read your appsettings.json file and ignore secrets.json. If however, you 
    //            ensure that the .csproj project containing your DBContext class contains the correct "UserSecretsId" section, then migrations will work properly.
    //
    //      Go to the  Package Manager Console (PMC) and enter: get-help entityframework
    //      This will show you all the powershell commands available related to entity framework.
    //      The commands you're interested in are:
    //      - Add-Migration
    //      - Update-Database
    //
    //      In the PMC select Default Project "DataAccessLayer" but in the solution explorer, set startup project
    //      to be the main application "EntityFramework"
    //      Let's create our initial migration by entering (FirstMigration will be the name of the migration): 
    //
    //        -->Syntax:     Add-Migration [-Name] <String> [-OutputDir <String>] [-Context <String>] [-Project <String>] [-StartupProject <String>] [-Environment <String>] [<CommonParameters>]
    //                       Add-Migration FirstMigration -Context SchoolContext 
    //
    //      This will generate a folder called "Migrations" and a file called XXXXX_FirstMigration.cs in that folder (where XXXX is the timestamp).
    //      Also a file called "SchoolContextModelSnapshot.cs" is created. It contains a snapshot of what the DB schema currently looks like. 
    //      EF uses this to compare changes in your model to determine what changed.
    //
    //      Now that you've created  the first migration script, we run migrations to create the database from the model.
    //      In the PMC enter this command to generate a T-SQL script that will perform the migration.
    //          script-migration
    //      This script can now be tweaked (if necessary) before running it against a PROD database.
    //
    //      If you want to run the migration directly against the database, then run the command:
    //          Update-Database  -Context SchoolContext  -verbose
    //      NOTE: Nothing happens if the database is already up to date.
    //
    //      Now, add a string property to the "Student" class called "MyFavoriteClass".
    //      Create a migration that will update the database with this new information.
    //
    //              Add-Migration SecondMigration -Context SchoolContext
    //
    //      The database has not been updated yet. To do that you need to execute the "Update-Database" command
    //              Update-Database -Context SchoolContext -verbose
    //
    //      The migrations infrastructure is smart enough to know which migrations have been applied to the database
    //      and which have not. So the previous commands will only apply migrations that have not yet been applied.
    //      At any time you can attempt to apply a specific migration by name:
    //
    //          Update-Database -Context SchoolContext -Migration SecondMigration -verbose
    //
    //      If the migration has already been applied then you'll see the message:
    //          "No migrations were applied. The database is already up to date."

    class Program
    {
        private static IUserConfiguration userConfiguration = null;

        static async Task Main(string[] args)
        {
            // Get the configuration needed for this demo. It assumes user secrets defined in the current entry assembly.
            userConfiguration = ConfigFactory.Initialize();

            // Create DSuite dummy data to play with
            await PopulateDummyCars(500);

            // Demonstrate the deferred execution POWER of IQueryable!
            // Find a small list of cars that we don't need to page through.
            FindMultipleUsingHelperClass();

            //------- Same query using pure LINQ -----------------
            FindMultipleUsingLINQ();

            // Ask the DBContextFactory to create the desired DbContext object for use for database access
            await CRUDUsingHelperClass();
      
            // Equivalent CRUD using just linq
            await CRUDUsingLINQ();


            // Demonstrate data PAGING! Construct a pager that specifies the where clause, page size and column order criteria
            EFPager < Car > pager = new EFPager<Car>(x => x.Mileage > 50000, 50, x => x.Make, x => x.Model, x => x.Mileage);

            do
            {
                // Note: The database will NOT be accessed during this call. 
                IQueryable<Car> resultQuery = await EFHelper.QueryEntities<Car, DSuiteContext>(pager);

                // Physically realize the data from the database. The act of enumeration causes the current thread
                // to block while data is retrieved from the database. To perform this asynchronously, use the RealizeData extension.
                resultQuery.DumpData();

            } while (pager.HasNextPage);
            Console.WriteLine($"Query {pager}:: {pager.TotalRowCount} total rows in {pager.TotalPages} pages were displayed\r\n\r\n");


            // Delete all play data
            EFCrud.DeleteAll<Car, DSuiteContext>();

            //-------------------- Use Code First ----------------------
            // Ask the DBContextFactory to create the desired DbContext object for use for database access
            SchoolContext schoolContext = DBContextFactory.GetDbContext<SchoolContext>();

            DbInitializer.Initialize(schoolContext);

            IQueryable<Student> studentList = EFCrud.FindMultiple<Student, SchoolContext>((x) => x.LastName.StartsWith("A"), (x) => x.LastName, false);
            studentList.DumpData();

            // Demonstrate basic CRUD with related tables
            Student newStudent = new Student { FirstMidName = "Tony", LastName = "Franklin", EnrollmentDate = DateTime.Parse("2009-09-09") };
            await EFCrud.Create(schoolContext, newStudent);
            Course newCourse = new Course { CourseID = 5022, Title = "Advanced C#", Credits = 4 };
            await EFCrud.Create(schoolContext, newCourse);
            Enrollment newEnrollment = new Enrollment { StudentID = newStudent.ID, CourseID = newCourse.CourseID, Grade = Grade.A };
            await EFCrud.Create(schoolContext, newEnrollment);

            // Now find a specific enrollment. 
            Enrollment enrollmentFound = EFCrud.FindSingle<Enrollment, SchoolContext>(x => x.EnrollmentID == newEnrollment.EnrollmentID);
            string grade = enrollmentFound?.Grade.ToString() ?? "??";
            Console.WriteLine($"Student({enrollmentFound.Student.FirstMidName} {enrollmentFound.Student.LastName}) Course({enrollmentFound.Course.Title}) Grade({grade})");

            // Delete the student that was just added
            EFCrud.Delete(schoolContext, newEnrollment);
            EFCrud.Delete(schoolContext, newCourse);
            EFCrud.Delete(schoolContext, newStudent);

            // Find all classes a student is enrolled into. NOTE:!!!! DbContextFactory uses "UseLazyLoadingProxies"
            // to ensure chold properties "Student" and "Course" are lazy loaded when their properties are accessed.
            // If that were not the case, then you've have to manually load the data based on ID values.
            newStudent = EFCrud.FindSingle<Student, SchoolContext>(x => x.LastName == "Alonso" && x.FirstMidName == "Meredith");
            IQueryable<Enrollment> enrolledList = EFCrud.FindMultiple<Enrollment, SchoolContext>(x=>x.StudentID == newStudent.ID);
            Console.WriteLine("\r\nMeredith Alonso was inrolled in the following courses:");
            foreach(Enrollment enrolled in enrolledList.ToList())
            {
                Console.WriteLine($"{enrolled.Course.Title} enrolled on {enrolled.Student.EnrollmentDate}");
            }



            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }

        public static async Task PopulateDummyCars(int NumberOfCars)
        {
            // Ask the DBContextFactory to create the desired DbContext object for use for database access
            DSuiteContext db = DBContextFactory.GetDbContext<DSuiteContext>();

            for (int i = 0; i < NumberOfCars; ++i)
            {
                Car NewCar = GenerateCar();
                await EFCrud.Create(db, NewCar);
            }
        }

        public static Car GenerateCar()
        {
            Random _random = new Random();
            int makeIndex = _random.Next(0, 5);
            int modelIndex = _random.Next(0, 5);
            int year = 1990 + _random.Next(0, 19);
            int mileage = _random.Next(100, 100000);
            List<string> Makes = new List<string> { "Ford", "GMC", "Infinity", "Honda", "Lexus", "Saturn" };
            List<string> Models = new List<string> { "Bunny", "Ducky", "Kitty", "Junk", "Cool", "Hot" };
            Car NewCar = new Car { Make = Makes[makeIndex], Model = Models[modelIndex], Year = year, Mileage = mileage };
            return (NewCar);
        }

        public static void FindMultipleUsingHelperClass()
        {
            // Demonstrate the deferred execution POWER of IQueryable!
            // Find a small list of cars that we don't need to page through.
            IQueryable<Car> fordBunnies = EFCrud.FindMultiple<Car, DSuiteContext>((x) => x.Make == "Ford" && x.Model == "Bunny" && x.Year < 2006);

            // But I DO want to sort them too. Still no DB call here!
            fordBunnies = fordBunnies.OrderBy(bunny => bunny.Mileage);

            // NOW, go ahead and execute the query against the database
            fordBunnies.DumpData();
        }

        public static void FindMultipleUsingLINQ()
        {
            // The ConfigFactory static constructor reads the "MyProjectSettings" from appsettings.json
            // or secrets.json and exposes the IUserConfiguration interface. We use that interface
            // to retreive the connection string mapped to the DatabaseName.
            string ConnectionString = userConfiguration.ConnectionString("DSuite");

            DbContextOptionsBuilder<DSuiteContext> optionsBuilder = new DbContextOptionsBuilder<DSuiteContext>();
            optionsBuilder.UseSqlServer(ConnectionString);
            DSuiteContext dSuiteContext = new DSuiteContext(optionsBuilder.Options);

            // Demonstrate the deferred execution POWER of IQueryable!
            // Find a small list of cars that we don't need to page through.
            IQueryable<Car> linqFordBunnies = (from c in dSuiteContext.Cars where c.Make == "Ford" && c.Model == "Bunny" && c.Year < 2006 select c);

            // But I DO want to sort them too. Still no DB call here!
            linqFordBunnies = linqFordBunnies.OrderBy(bunny => bunny.Mileage);

            // NOW, go ahead and execute the query against the database
            linqFordBunnies.DumpData();
        }

        public static async Task CRUDUsingHelperClass()
        {
            Car newCar = GenerateCar();

            // Demonstrate basic CRUD
            DSuiteContext db = DBContextFactory.GetDbContext<DSuiteContext>();
            await EFCrud.Create(db, newCar);
            Car found = await EFCrud.GetByKey(db, newCar);
            EFCrud.Delete(db, found);
        }

        public static async Task CRUDUsingLINQ()
        {
            // The ConfigFactory static constructor reads the "MyProjectSettings" from appsettings.json
            // or secrets.json and exposes the IUserConfiguration interface. We use that interface
            // to retreive the connection string mapped to the DatabaseName.
            string ConnectionString = userConfiguration.ConnectionString("DSuite");

            DbContextOptionsBuilder<DSuiteContext> optionsBuilder = new DbContextOptionsBuilder<DSuiteContext>();
            optionsBuilder.UseSqlServer(ConnectionString);
            DSuiteContext dSuiteContext = new DSuiteContext(optionsBuilder.Options);

            // Equivalent CRUD using just linq
            Car newCar = GenerateCar();
            dSuiteContext.Cars.Add(newCar);
            await dSuiteContext.SaveChangesAsync();
            Car found = (from c in dSuiteContext.Cars where c.CarId == newCar.CarId select c).SingleOrDefault();
            dSuiteContext.Remove(found);
            await dSuiteContext.SaveChangesAsync();
        }
    }

}
