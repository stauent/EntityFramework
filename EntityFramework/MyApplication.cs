using ConfigurationAssistant;

using DataAccessLayer.CodeFirstModels;
using DataAccessLayer.CodeFirstModels.Data;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories;

using EFSupport;

using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EntityFramework
{
    // NOTE: This constructor is set up to simply demonstrate how we can get various parameters
    //       using dependency injection. This is not necessarily an example of how you should write code.
    //       One of the reasons its structure this way is so that we can examine/discuss the pros
    //       and cons of injecting repository interfaces vs. DbContext objects.  While the repository
    //       interfaces help support unit testing, we can still achieve the same goal using "InMemory"
    //       database (dotnet add package Microsoft.EntityFrameworkCore.InMemory)
    //       https://entityframeworkcore.com/providers-inmemory
    //
    //       Because it's difficult to mock out a DbContext, EF Core supports "UseInMemoryDatabase"
    //       https://entityframeworkcore.com/knowledge-base/47553878/mocking-entity-framework-core-context
    //
    //       In order for the EFGenericRepository to be "really" useful, all DbSet entities should use
    //       the same key type (uniqueidentifier or int) and all classes should use the same key
    //       property name of "id". Then, EFGenericRepository would not have to be
    //       an abstract class and could a generic implementation of GetById.
    //       This would also simplify the registration of the all repositories because then
    //       we could use:
    //             services.AddScoped(typeof(IDataRepository<>), typeof(EFGenericRepository<>));
    //
    public class MyApplication 
    {
        private readonly IApplicationRequirements<MyApplication> _requirements;

        private readonly DSuiteContext _dSuite;
        private readonly SchoolContext _school;
        private readonly ICarRepository _cars;
        private readonly ICourseRepository _courses;
        private readonly IStudentRepository _students;
        private readonly IEnrollmentRepository _enrollments;
        private readonly IDataRepository<Student, int, SchoolContext> _genericStudentDataRepository;

        /// <summary>
        /// Application initialization.
        /// </summary>
        /// <param name="requirements">IApplicationRequirements Supplied by DI. All required interfaces</param>
        /// <param name="dSuiteContext">DbContext for DSuite database</param>
        /// <param name="schoolContext">DbContext for School database</param>
        /// <param name="cars">ICarRepository Example of using repository pattern</param>
        /// <param name="courses">ICourseRepository Example of using repository pattern</param>
        /// <param name="students">IStudentsRepository Example of using repository pattern</param>
        /// <param name="enrollments">IEnrollmentsRepository Example of using repository pattern</param>
        public MyApplication(IApplicationRequirements<MyApplication> requirements, 
            DSuiteContext dSuiteContext, 
            SchoolContext schoolContext, 
            ICarRepository cars, 
            ICourseRepository courses, 
            IStudentRepository students, 
            IEnrollmentRepository enrollments,
            IDataRepository<Student, int, SchoolContext> genericStudentDataRepository
            )
        {
            _requirements = requirements;
            _dSuite = dSuiteContext;
            _school = schoolContext;
            _cars = cars;
            _courses = courses;
            _students = students;
            _enrollments = enrollments;

            // Same as the _students interface, except it's an open generic type
            _genericStudentDataRepository = genericStudentDataRepository;

            EFCrud.InitializeLogger(_requirements.ApplicationLogger);
            EFHelper.InitializeLogger(_requirements.ApplicationLogger);
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
            FindMultipleUsingHelperClass(_dSuite);

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
            // Ask the DBContextFactory to create the desired DbContext object for use for database access.
            // NOTE: This is for demo purposes only! The DbContexts for this application were injected
            //       in the constructor, and this is how you would "USUALLY" access the DbContext to work with.
            SchoolContext schoolContext = DBContextFactory.GetDbContext<SchoolContext>();

            DbInitializer.Initialize(schoolContext);

            IQueryable<Student> studentList = schoolContext.FindMultiple<Student, SchoolContext>((x) => x.LastName.StartsWith("A"), (x) => x.LastName, false);
            studentList.DumpData($"\r\n\r\n==============EFCrud.FindMultiple==============================");

            // Demonstrate basic CRUD with related tables
            Student newStudent = new Student { FirstMidName = "Tony", LastName = "Franklin", EnrollmentDate = DateTime.Parse("2009-09-09") };
            await schoolContext.Create(newStudent);
            Course newCourse = new Course { CourseID = 5022, Title = "Advanced C#", Credits = 4 };
            await schoolContext.Create(newCourse);
            Enrollment newEnrollment = new Enrollment { StudentID = newStudent.Id, CourseID = newCourse.CourseID, Grade = Grade.A };
            await schoolContext.Create(newEnrollment);

            // Now find a specific enrollment. 
            Enrollment enrollmentFound = schoolContext.FindSingle<Enrollment, SchoolContext>(x => x.EnrollmentID == newEnrollment.EnrollmentID);
            string grade = enrollmentFound?.Grade.ToString() ?? "??";
            Console.WriteLine($"Student({enrollmentFound.Student.FirstMidName} {enrollmentFound.Student.LastName}) Course({enrollmentFound.Course.Title}) Grade({grade})");

            // Delete the student that was just added
            schoolContext.Delete(newEnrollment);
            schoolContext.Delete(newCourse);
            schoolContext.Delete(newStudent);

            // Find all classes a student is enrolled into. NOTE:!!!! DbContextFactory uses "UseLazyLoadingProxies"
            // to ensure that properties "Student" and "Course" are lazy loaded when their properties are accessed.
            // If that were not the case, then you've have to manually load the data based on ID values.
            newStudent = schoolContext.FindSingle<Student, SchoolContext>(x => x.LastName == "Alonso" && x.FirstMidName == "Meredith");
            IQueryable<Enrollment> enrolledList = schoolContext.FindMultiple<Enrollment, SchoolContext>(x => x.StudentID == newStudent.Id);
            Console.WriteLine("\r\nMeredith Alonso was inrolled in the following courses:");
            foreach (Enrollment enrolled in enrolledList.ToList())
            {
                Console.WriteLine($"{enrolled.Course.Title} enrolled on {enrolled.Student.EnrollmentDate}");
            }

            // Use the DbContext classes that were passed in using Dependency Injection
            Car myCar = GenerateCar();
            _dSuite.Cars.Add(myCar);
            _dSuite.SaveChanges();
            myCar.TraceInformation("Using DI created context");
            Car foundMyCar = (from c in _dSuite.Cars where c.CarId == myCar.CarId select c).FirstOrDefault();
            foundMyCar.TraceInformation("Example of how to use LINQ to find an entity");

            // Use generic repository pattern (using dependency injection)
            foundMyCar = _cars.GetById(myCar.CarId);
            foundMyCar.TraceInformation("Found using generic repository");

            // Remove the car to clean up the DB
            _dSuite.Cars.Remove(myCar);
            _dSuite.SaveChanges();

            // Use the open generic type provided by dependency injection for the "school" repo
            Student Bobby = new Student { FirstMidName = "Bobby", LastName = "Simpson", EnrollmentDate = DateTime.Parse("2010-08-01") };
            _genericStudentDataRepository.Insert(Bobby);
            Bobby.TraceInformation("New student inserted using dependency injection open generic");
            Student foundStudent = _genericStudentDataRepository.GetById(Bobby.Id);
            foundStudent.TraceInformation("Found student using dependency injection open generic");
            _genericStudentDataRepository.Delete(Bobby.Id);
            foundStudent = _genericStudentDataRepository.GetById(Bobby.Id);
            if (foundStudent == null)
                Bobby.TraceInformation("Successfully deleted student");
            else
                Bobby.TraceInformation("Failed to deleted student");

            // Perform the same action with the IStudentRepository
            Bobby = new Student { FirstMidName = "Bobby", LastName = "Simpson", EnrollmentDate = DateTime.Parse("2010-08-01") };
            _students.Insert(Bobby);
            Bobby.TraceInformation("New student inserted using dependency injection open generic");
            foundStudent = _students.GetById(Bobby.Id);
            foundStudent.TraceInformation("Found student using dependency injection open generic");
            _students.Delete(Bobby.Id);
            foundStudent = _students.GetById(Bobby.Id);
            if (foundStudent == null)
                Bobby.TraceInformation("Successfully deleted student");
            else
                Bobby.TraceInformation("Failed to deleted student");


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

        public void FindMultipleUsingHelperClass(DSuiteContext dSuite)
        {
            // Demonstrate the deferred execution POWER of IQueryable!
            // Find a small list of cars that we don't need to page through.
            IQueryable<Car> fordBunnies = dSuite.FindMultiple<Car, DSuiteContext>((x) => x.Make == "Ford" && x.Model == "Bunny" && x.Year < 2006,x => x.Year);

            // But I DO want to sort them too. Still no DB call here!
            fordBunnies = fordBunnies.OrderBy(bunny => bunny.Mileage);

            // NOW, go ahead and execute the query against the database
            fordBunnies.DumpData($"\r\n\r\n==============FindMultipleUsingHelperClass==============================");
        }

        public void FindMultipleUsingLINQ()
        {
            DSuiteContext dSuiteContext = DBContextFactory.GetDbContext<DSuiteContext>();

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
            // to retrieve the connection string mapped to the DatabaseName.
            string ConnectionString = _requirements.UserConfiguration.ConnectionString("DSuite");

            DbContextOptionsBuilder<DSuiteContext> optionsBuilder = new DbContextOptionsBuilder<DSuiteContext>();
            optionsBuilder.UseSqlServer(ConnectionString);
            DSuiteContext dSuiteContext = new DSuiteContext(optionsBuilder.Options);

            // NOTE: All the code above could be replaced with a single line of code. 
            //       We write out the code here just to show you what's going on 
            //       under the covers:
            //
            //          DSuiteContext dSuiteContext = DBContextFactory.GetDbContext<DSuiteContext>(); 


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
