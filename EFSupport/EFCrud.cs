using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace EFSupport
{
    /// <summary>
    /// Generic class provides CRUD and search support when working with Entity Framework
    /// </summary>
    public static class EFCrud
    {
        private static ILogger _logger;

        public static void InitializeLogger(ILogger logger)
        {
            _logger = logger;
        }


        /// <summary>
        /// Create a new entity in the database
        /// </summary>
        /// <typeparam name="T">Entity of type T to be added into the database</typeparam>
        /// <typeparam name="DT">Your DbContext type</typeparam>
        /// <param name="db">Reference to your DbContext</param>
        /// <param name="newEntity">Reference to the entity you want to add into the database</param>
        /// <returns>The created entity from the database</returns>
        public static async Task<T> Create<T, DT>(DT db, T newEntity) where DT : DbContext where T : class
        {
            try
            {
                DbSet<T> dbSet = db.Set<T>();
                dbSet.Add(newEntity);
                await db.SaveChangesAsync();
            }
            catch (Exception Err)
            {
                _logger?.LogError(Err, "Could not create entity");
            }

            return (newEntity);
        }

        /// <summary>
        /// Updates an entity in the database with modifications provided in the parameter
        /// </summary>
        /// <typeparam name="T">Entity of type T to be updated in the database</typeparam>
        /// <typeparam name="DT">Your DbContext type</typeparam>
        /// <param name="db">Reference to your DbContext</param>
        /// <param name="updateEntity">Reference to the entity you want to update in the database</param>
        /// <returns>The updated entity</returns>
        public static async Task<T> Update<T, DT>(DT db, T updateEntity) where DT : DbContext where T : class
        {
            try
            {
                object[] keyValues = GetKeys(db, updateEntity);
                T found = await GetByKey<T, DT>(db, keyValues);
                if (found != null)
                {
                    db.Entry(found).CurrentValues.SetValues(updateEntity);
                    db.SaveChanges();
                }
            }
            catch (Exception Err)
            {
                _logger?.LogError(Err, "Could not update entity");
            }

            return (updateEntity);
        }

        /// <summary>
        /// Inspects the entity and returns its key values
        /// </summary>
        /// <typeparam name="T">Entity of type T that you're getting keys from</typeparam>
        /// <typeparam name="DT">Your DbContext type</typeparam>
        /// <param name="db">Reference to your DbContext</param>
        /// <param name="entity">Reference to the entity you're getting key values from</param>
        /// <returns>object array containing all the keys in the entity</returns>
        public static object[] GetKeys<T, DT>(DT db, T entity) where DT : DbContext where T : class
        {
            IDictionary<string, object> keys = db.GetKeys(entity);
            int keyCount = keys.Count;
            object[] keyValues = new object[keyCount];
            foreach (KeyValuePair<string, object> kvPair in keys)
                keyValues[0] = kvPair.Value;
            return (keyValues);
        }

        /// <summary>
        /// Find's a single entity that matches the keys provided
        /// </summary>
        /// <typeparam name="T">Entity of type T</typeparam>
        /// <typeparam name="DT">Your DbContext type</typeparam>
        /// <param name="db">Reference to your DbContext</param>
        /// <param name="keyValues">object array containing the key values you're searching for</param>
        /// <returns>The entity with matching keys</returns>
        public static async Task<T> GetByKey<T, DT>(DT db, params object[] keyValues) where DT : DbContext where T : class
        {
            DbSet<T> dbSet = db.Set<T>();
            T found = await dbSet.FindAsync(keyValues);
            return (found);
        }

        /// <summary>
        /// Find's a single entity that matches the keys provided
        /// </summary>
        /// <typeparam name="T">Entity of type T</typeparam>
        /// <typeparam name="DT">Your DbContext type</typeparam>
        /// <param name="db">Reference to your DbContext</param>
        /// <param name="entity">Reference to the entity you're getting key values from</param>
        /// <returns>The entity with matching keys</returns>
        public static async Task<T> GetByKey<T, DT>(DT db, T entity) where DT : DbContext where T : class
        {
            DbSet<T> dbSet = db.Set<T>();
            object[] keyValues = GetKeys(db, entity);
            T found = await dbSet.FindAsync(keyValues);
            return (found);
        }

        /// <summary>
        /// Finds one or more entities matching the WhereClause criteria
        /// </summary>
        /// <typeparam name="T">Entity of type T</typeparam>
        /// <typeparam name="DT">Your DbContext type</typeparam>
        /// <param name="WhereClause">LINQ expression used to filter the data</param>
        /// <param name="OrderBy">LINQ expression used to order the data</param>
        /// <param name="Ascending">Order is ascending if true, else descending</param>
        /// <param name="Take">Max number of records to return</param>
        /// <returns>IQueryable of T. This means that the DB is not accessed until you start to enumerate the result.</returns>
        public static IQueryable<T> FindMultiple<T, DT>(Expression<Func<T, bool>> WhereClause, Expression<Func<T, object>> OrderBy = null, bool Ascending = true, int Take = 50) where DT : DbContext where T : class
        {
            DT db = DBContextFactory.GetDbContext<DT>();
            DbSet<T> dbSet = db.Set<T>();
            IQueryable<T> found = (from entity in dbSet select entity).Where(WhereClause);
            if (OrderBy != null)
            {
                if (Ascending)
                    found = found.OrderBy(OrderBy);
                else
                    found = found.OrderByDescending(OrderBy);
            }
            found = found.Take(Take);
            return (found);
        }

        /// <summary>
        /// Finds a single entity matching the WhereClause criteria
        /// </summary>
        /// <typeparam name="T">Entity of type T</typeparam>
        /// <typeparam name="DT">Your DbContext type</typeparam>
        /// <param name="WhereClause">LINQ expression used to filter the data</param>
        /// <returns>The matching entity is returned</returns>
        public static T FindSingle<T, DT>(Expression<Func<T, bool>> WhereClause) where DT : DbContext where T : class
        {
            DT db = DBContextFactory.GetDbContext<DT>();
            DbSet<T> dbSet = db.Set<T>();
            T found = (from entity in dbSet select entity).SingleOrDefault(WhereClause);
            return (found);
        }

        /// <summary>
        /// Deletes a single entity from the database
        /// </summary>
        /// <typeparam name="T">Entity of type T</typeparam>
        /// <typeparam name="DT">Your DbContext type</typeparam>
        /// <param name="db">Reference to your DbContext</param>
        /// <param name="keyValues">object array containing the key values you're searching for</param>
        /// <returns>The deleted entity</returns>
        public static async Task<T> Delete<T, DT>(DT db, params object[] keyValues) where DT : DbContext where T : class
        {
            T Deleted = null;
            try
            {
                T ToDelete = await GetByKey<T, DT>(db, keyValues);
                Deleted = Delete(db,ToDelete);
            }
            catch (Exception Err)
            {
                _logger?.LogError(Err, "Could not delete entity");
            }

            return (Deleted);
        }

        /// <summary>
        /// Deletes a single entity from the database
        /// </summary>
        /// <typeparam name="T">Entity of type T</typeparam>
        /// <typeparam name="DT">Your DbContext type</typeparam>
        /// <param name="db">Reference to your DbContext</param>
        /// <param name="ToDelete">Reference to the entity you want to delete</param>
        /// <returns>The deleted entity</returns>
        public static T Delete<T, DT>(DT db, T ToDelete) where DT : DbContext where T : class
        {
            T Deleted = null;
            if (ToDelete != null)
            {
                DbSet<T> dbSet = db.Set<T>();
                dbSet.Remove(ToDelete);
                db.SaveChanges();
                Deleted = ToDelete;
            }

            return (Deleted);
        }

        /// <summary>
        /// Deletes all entities from the table of the specified type
        /// </summary>
        /// <typeparam name="T">Entity of type T</typeparam>
        /// <typeparam name="DT">Your DbContext type</typeparam>
        public static void DeleteAll<T, DT>() where DT : DbContext where T : class
        {
            DT db = DBContextFactory.GetDbContext<DT>();
            DbSet<T> dbSet = db.Set<T>();
            dbSet.RemoveRange(dbSet);
            db.SaveChanges();
        }

        /// <summary>
        /// Deletes all entities that match the WhereClause
        /// </summary>
        /// <typeparam name="T">Entity of type T</typeparam>
        /// <typeparam name="DT">Your DbContext type</typeparam>
        /// <param name="WhereClause">LINQ expression used to filter the data</param>
        public static void DeleteMultiple<T, DT>(Expression<Func<T, bool>> WhereClause) where DT : DbContext where T : class
        {
            DT db = DBContextFactory.GetDbContext<DT>();
            DbSet<T> dbSet = db.Set<T>();
            dbSet.RemoveRange(dbSet.Where(WhereClause));
        }
    }

}
