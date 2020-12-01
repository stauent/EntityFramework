using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace EFSupport
{
    /// <summary>
    /// Extension methods used to simplify querying and paging through DbSet
    /// data.
    /// </summary>
    public static class EFHelper
    {
        /// <summary>
        /// Extension method to return the name of a property used in a lambda expression.
        ///     e.g.   X => X.LastName
        ///  ... returns "LastName"           
        /// </summary>
        /// <typeparam name="T">Type of object being used in the lambda</typeparam>
        /// <param name="propertyLabda">Any lambda expression returning a string</param>
        /// <returns></returns>
        public static string GetLambdaPropertyName<T>(this Expression<Func<T, string>> propertyLabda)
        {
            return (propertyLabda.Body as MemberExpression ?? ((UnaryExpression)propertyLabda.Body).Operand as MemberExpression).Member.Name;
        }

        /// <summary>
        /// Note that there are no async versions of some LINQ operators such as Where or OrderBy, 
        /// because these only build up the LINQ expression tree and don't cause the query to be executed in the database. 
        /// Only operators which cause query execution have async counterparts.
        /// </summary>
        /// <typeparam name="T">Type of entity you are querying</typeparam>
        /// <typeparam name="DT">Type of DbContext the entity is from</typeparam>
        /// <param name="Pager">A EFPager that simplifies paging through the entity table</param>
        /// <returns>A page of IQueryable sorted data of type T</returns>
        public static async Task<IQueryable<T>> QueryEntities<T, DT>(EFPager<T> Pager) where DT : DbContext where T : class
        {
            DT db = DBContextFactory.GetDbContext<DT>();
            DbSet<T> dbSet = db.Set<T>();
            IQueryable<T> filteredSet = dbSet; // (from x in dbSet select x);

            if (Pager.WhereClause != null)
            {
                filteredSet = filteredSet.Where(Pager.WhereClause);
            }

            IOrderedQueryable<T> ordered = null;

            // Apply all column sorting one at a time. We are simply modifying
            // the expression tree. No data is actually coming from the DB yet!
            foreach (ColumnSortInfo<T> sortColumn in Pager.SortOrder)
            {
                if (ordered == null)
                {
                    if (sortColumn.SortAscending)
                        ordered = filteredSet.OrderBy(sortColumn.ColumnName);
                    else
                        ordered = filteredSet.OrderByDescending(sortColumn.ColumnName);
                }
                else
                {
                    if (sortColumn.SortAscending)
                        ordered = ordered.ThenBy(sortColumn.ColumnName);
                    else
                        ordered = ordered.ThenByDescending(sortColumn.ColumnName);
                }
            }

            if (ordered != null)
            {
                filteredSet = ordered;
            }

            // Now that our query has been fully constructed, ask the database 
            // how many records will be in the result. We can then calculate
            // all the necessary paging information.
            if (!Pager.Initialized)
            {
                int TotalRowCount = await filteredSet.CountAsync();
                Pager.InitializePageInfo(TotalRowCount);
            }

            // Now ask for ONLY the page of data we are looking for
            filteredSet = filteredSet.Skip(Pager.Skip).Take(Pager.PageSize);

            // Update the page number so we can easily walk through a whole table
            Pager.MoveToNextPage();

            return (filteredSet);
        }

        /// <summary>
        /// As soon as you start to enumerate the IQueryable<T> returned from QueryEntities, 
        /// the code starts to synchronously pull the data from the database to fully enumerate
        /// the data that was queried. This means the caller blocks until the data is 
        /// returned. If this is a large amount of data, you might want to pull the data
        /// from the database asynchronously and once it all arrives, then process it.
        /// This extention method realizes the data from the database asynchrously.
        /// </summary>
        /// <typeparam name="T">Implicitly derived from the IQueryable</typeparam>
        /// <param name="QueryToRealize">IQueryable that is selecting the data</param>
        /// <param name="PerformWhileUWait">Action that can be executed while you wait for the data to be retreived. If null, then no action taken.</param>
        /// <returns>An awaitable task containing the requested List of data</returns>
        public static async Task<List<T>> RealizeData<T>(this IQueryable<T> QueryToRealize, Action PerformWhileUWait = null) where T : class
        {
            // Creates a List<T> from an IQueryable by enumerating it asynchronously.
            Task<List<T>> gettingData = QueryToRealize.ToListAsync();

            // We can perform other work here while ToListAsync is completing
            if (PerformWhileUWait != null)
                PerformWhileUWait();

            // Wait for the data to arrive
            List<T> realizedData = await gettingData;

            return (realizedData);
        }

        public static void DumpData<T>(this IEnumerable<T> ListOfData, string StartMessage = null) where T : class
        {
            if(StartMessage != null)
                Console.WriteLine(StartMessage);

            // Physically realize the data from the database. The act of enumeration causes the current thread
            // to block while data is retreived from the database. To perform this asynchronously, use the RealizeData extension.
            var data = ListOfData.ToList();
            Newtonsoft.Json.JsonSerializerSettings jSettings = new Newtonsoft.Json.JsonSerializerSettings()
            {
                ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore, MaxDepth = 1
            };

            foreach (var entity in data)
            {
                string serialized = JsonConvert.SerializeObject(entity, Formatting.Indented, jSettings);
                Console.WriteLine(serialized);
            }
        }


        public static IDictionary<string, object> GetKeys<T, DT>(this DT ctx, T entity) where DT : DbContext where T : class
        {
            if (ctx == null)
            {
                throw new ArgumentNullException(nameof(ctx));
            }

            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var entry = ctx.Entry(entity);
            var primaryKey = entry.Metadata.FindPrimaryKey();
            var keys = primaryKey.Properties.ToDictionary(x => x.Name, x => x.PropertyInfo.GetValue(entity));

            return keys;
        }

        public static IEnumerable<string> GetDirtyProperties<T, DT>(this DT ctx, T entity) where DT : DbContext where T : class
        {
            if (ctx == null)
            {
                throw new ArgumentNullException(nameof(ctx));
            }

            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var entry = ctx.Entry(entity);
            var originalValues = entry.OriginalValues;
            var currentValues = entry.CurrentValues;

            foreach (var prop in originalValues.Properties)
            {
                if (object.Equals(originalValues[prop.Name], currentValues[prop.Name]) == false)
                {
                    yield return prop.Name;
                }
            }
        }

    }

}
