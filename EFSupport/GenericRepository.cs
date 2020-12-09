using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.Extensions.DependencyInjection;

namespace EFSupport
{
    /// <summary>
    /// Used as a marker for reflection
    /// </summary>
    public interface IGenericRepositoryBase
    {

    }
    public interface IGenericRepository<T,TKey> : IGenericRepositoryBase
    {
        public IEnumerable<T> GetAll();
        public T GetById(TKey id);
        public void Insert(T entity);
        public void Update(T entity);
        public void Delete(TKey id);
    }

    public abstract class GenericRepository<T, TKey, TContext> : IGenericRepository<T, TKey> where T: class where TContext:DbContext
    {
        protected readonly TContext context;
        protected DbSet<T> entities;
        string errorMessage = string.Empty;

        public GenericRepository(TContext context)
        {
            this.context = context;
            entities = context.Set<T>();
        }
        public IEnumerable<T> GetAll()
        {
            return entities.AsEnumerable();
        }

        public abstract T GetById(TKey id);
        //{
        //    return entities.SingleOrDefault(s => s.Id == id);
        //}

        public void Insert(T entity)
        {
            if (entity == null) throw new ArgumentNullException("entity");

            entities.Add(entity);
            context.SaveChanges();
        }
        public void Update(T entity)
        {
            if (entity == null) throw new ArgumentNullException("entity");
            context.SaveChanges();
        }
        public void Delete(TKey id)
        {
            T entity = GetById(id);
            entities.Remove(entity);
            context.SaveChanges();
        }
    }

    public class DependencyInjectionPair
    {
        public Type ServiceInterface { get; set; }
        public Type ServiceImplementation { get; set; }
    }

    public static class TypeExtensions
    {
        public static List<DependencyInjectionPair> GetAllTypesForDependencyInjection(this Type InterfaceType)
        {
            List<DependencyInjectionPair> names = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
                .Where(x => InterfaceType.IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract )
                .Select(x =>
                {
                    Type[] interfaces = x.GetInterfaces();
                    Type serviceInterface = (from si in interfaces where !si.IsGenericType && si != InterfaceType select si).FirstOrDefault();
                    return new DependencyInjectionPair { ServiceInterface = serviceInterface, ServiceImplementation = x };
                })
                .ToList();

            return names;
        }


        /// <summary>
        /// Registers any repository that was derived from GenericRepository
        /// </summary>
        /// <param name="services">IServiceCollection used for dependency injection</param>
        public static void AddGenericRepositories(this IServiceCollection services)
        {
            List<DependencyInjectionPair> all = typeof(IGenericRepositoryBase).GetAllTypesForDependencyInjection();
            foreach (DependencyInjectionPair pair in all)
            {
                services.AddScoped(pair.ServiceInterface, pair.ServiceImplementation);
            }
        }

        /// <summary>
        /// Extension method that will load ALL DbContext derived classes into the IOC container
        /// to support dependency injection.
        /// NOTE: The assembly containing the DbContext classes MUST be loaded into the current domain
        ///         for this call to work. We put a dummy static class in there so we can call
        ///         AssemblyLoader.EnsureLoaded(); This will ensure the assembly containing the DbContext
        ///         classes is in the current domain.
        /// </summary>
        /// <param name="services">IServiceCollection</param>
        /// <param name="UseLazyLoading">true to allow lazy loading</param>
        public static void AddAllDbContextTypes(this IServiceCollection services, bool UseLazyLoading = true)
        {
            // Find all the DbContext derived classes available
            List<Type> all = AppDomain.CurrentDomain.GetAssemblies().OrderBy(x => x.FullName).SelectMany(x => x.GetTypes())
                .Where(x => x.IsClass && !x.IsAbstract && x.IsSubclassOf(typeof(DbContext)))
                .Select(x => x)
                .ToList();

            // Construct a generic method point to "DBContextFactory.AddDbContextScoped" . This method
            // will be called for each DbContext class to register it in the IOC container
            MethodInfo addDbContextScoped = (from t in Assembly.GetExecutingAssembly().GetTypes()
               where !t.IsGenericType && !t.IsNested
               from m in t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
               where m.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false)
               where m.GetParameters()[0].ParameterType == typeof(IServiceCollection)
                       where m.Name == nameof(DBContextFactory.AddDbContextScoped)
                       select m).SingleOrDefault();
        
           // Register all DbContext classes in IOC container    
           if (addDbContextScoped != null)
           {
               foreach (Type dbContext in all)
               {
                   var mymethod = addDbContextScoped.MakeGenericMethod(dbContext);
                   mymethod!.Invoke(services, new object[]{services, UseLazyLoading });
               }
            }
        }

        public static void AddAllDbContextAndRepositoryTypes(this IServiceCollection services, bool UseLazyLoading = true)
        {
            // Now that the DataAccessLayer is loaded, we can register all DbContext classes 
            // into the IOC container.
            services.AddAllDbContextTypes(UseLazyLoading);

            // Registers all XXXXXRepository classes that have be derived from GenericRepository
            services.AddGenericRepositories();
        }
    }
}

