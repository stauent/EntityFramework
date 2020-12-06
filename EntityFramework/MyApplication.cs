using ConfigurationAssistant;

using DataAccessLayer.CodeFirstModels;
using DataAccessLayer.CodeFirstModels.Data;
using DataAccessLayer.Models;

using EFSupport;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EntityFramework
{
    public class MyApplication 
    {
        private readonly IApplicationRequirements<MyApplication> _requirements;

        /// <summary>
        /// Application initialization. 
        /// </summary>
        /// <param name="IApplicationRequirements">Supplied by DI. All required interfaces</param>
        public MyApplication(IApplicationRequirements<MyApplication> requirements)
        {
            _requirements = requirements;

            EFCrud.InitializeLogger(_requirements.ApplicationLogger);
        }

        /// <summary>
        /// This is the application entry point. 
        /// </summary>
        /// <returns></returns>
        internal async Task Run()
        {
            $"Application Started at {DateTime.UtcNow}".TraceInformation();

            await DoWork();

            $"Application Ended at {DateTime.UtcNow}".TraceInformation();

            Console.WriteLine("PRESS <ENTER> TO EXIT");
            Console.ReadKey();
        }

        /// <summary>
        /// All tests/work are performed here
        /// </summary>
        /// <returns></returns>
        internal async Task DoWork()
        {
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
            EFPager<Car> pager = new EFPager<Car>(x => x.Mileage > 50000, 50, x => x.Make, x => x.Model, x => x.Mileage);

            do
            {
                // Note: The database will NOT be accessed during this call. 
                IQueryable<Car> resultQuery = await EFHelper.QueryEntities<Car, DSuiteContext>(pager);

                // Physically realize the data from the database. The act of enumeration causes the current thread
                // to block while data is retrieved from the database. To perform this asynchronously, use the RealizeData extension.
                resultQuery.DumpData($"\r\n\r\n==============Page {pager.PageIndex} of Car data==============================");

            } while (pager.HasNextPage);
            Console.WriteLine($"Query {pager}:: {pager.TotalRowCount} total rows in {pager.TotalPages} pages were displayed\r\n\r\n");


            // Delete all play data
            EFCrud.DeleteAll<Car, DSuiteContext>();

            //-------------------- Use Code First ----------------------
            // Ask the DBContextFactory to create the desired DbContext object for use for database access
            SchoolContext schoolContext = DBContextFactory.GetDbContext<SchoolContext>();

            DbInitializer.Initialize(schoolContext);

            IQueryable<Student> studentList = EFCrud.FindMultiple<Student, SchoolContext>((x) => x.LastName.StartsWith("A"), (x) => x.LastName, false);
            studentList.DumpData($"\r\n\r\n==============EFCrud.FindMultiple==============================");

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
            IQueryable<Enrollment> enrolledList = EFCrud.FindMultiple<Enrollment, SchoolContext>(x => x.StudentID == newStudent.ID);
            Console.WriteLine("\r\nMeredith Alonso was inrolled in the following courses:");
            foreach (Enrollment enrolled in enrolledList.ToList())
            {
                Console.WriteLine($"{enrolled.Course.Title} enrolled on {enrolled.Student.EnrollmentDate}");
            }

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

        public void FindMultipleUsingHelperClass()
        {
            // Demonstrate the deferred execution POWER of IQueryable!
            // Find a small list of cars that we don't need to page through.
            IQueryable<Car> fordBunnies = EFCrud.FindMultiple<Car, DSuiteContext>((x) => x.Make == "Ford" && x.Model == "Bunny" && x.Year < 2006);

            // But I DO want to sort them too. Still no DB call here!
            fordBunnies = fordBunnies.OrderBy(bunny => bunny.Mileage);

            // NOW, go ahead and execute the query against the database
            fordBunnies.DumpData($"\r\n\r\n==============FindMultipleUsingHelperClass==============================");
        }

        public void FindMultipleUsingLINQ()
        {
            // The ConfigFactory static constructor reads the "MyProjectSettings" from appsettings.json
            // or secrets.json and exposes the IUserConfiguration interface. We use that interface
            // to retreive the connection string mapped to the DatabaseName.
            string ConnectionString = _requirements.UserConfiguration.ConnectionString("DSuite");

            DbContextOptionsBuilder<DSuiteContext> optionsBuilder = new DbContextOptionsBuilder<DSuiteContext>();
            optionsBuilder.UseSqlServer(ConnectionString);
            DSuiteContext dSuiteContext = new DSuiteContext(optionsBuilder.Options);

            // Demonstrate the deferred execution POWER of IQueryable!
            // Find a small list of cars that we don't need to page through.
            IQueryable<Car> linqFordBunnies = (from c in dSuiteContext.Cars where c.Make == "Ford" && c.Model == "Bunny" && c.Year < 2006 select c);

            // But I DO want to sort them too. Still no DB call here!
            linqFordBunnies = linqFordBunnies.OrderBy(bunny => bunny.Mileage);

            // NOW, go ahead and execute the query against the database
            linqFordBunnies.DumpData("\r\n\r\n==============FindMultipleUsingLINQ==============================");
        }

        public async Task CRUDUsingHelperClass()
        {
            Car newCar = GenerateCar();

            // Demonstrate basic CRUD
            DSuiteContext db = DBContextFactory.GetDbContext<DSuiteContext>();
            await EFCrud.Create(db, newCar);
            Car found = await EFCrud.GetByKey(db, newCar);
            EFCrud.Delete(db, found);
        }

        public async Task CRUDUsingLINQ()
        {
            // The ConfigFactory static constructor reads the "MyProjectSettings" from appsettings.json
            // or secrets.json and exposes the IUserConfiguration interface. We use that interface
            // to retreive the connection string mapped to the DatabaseName.
            string ConnectionString = _requirements.UserConfiguration.ConnectionString("DSuite");

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
