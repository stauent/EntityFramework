using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace EFSupport
{
    /// <summary>
    /// Generic data pager for Entity Framework
    /// </summary>
    /// <typeparam name="T">Type of entity being paged</typeparam>
    public class EFPager<T> where T : class
    {
        public const int DefaultPageSize = 20;
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = DefaultPageSize;
        public int TotalRowCount { get; private set; } = -1;
        public int TotalPages { get; private set; } = -1;

        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;
        public int Skip => (PageIndex - 1) * PageSize;

        public EFPager(int PageSize = DefaultPageSize)
        {
            this.PageSize = PageSize;
        }

        /// <summary>
        /// Return the entire paging expression into a string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (WhereClause != null)
            {
                sb.Append($".Where({WhereClause})");
            }

            bool first = true;

            // Add on all sorting info
            foreach (ColumnSortInfo<T> sortColumn in SortOrder)
            {
                if (first)
                {
                    first = false;
                    if (sortColumn.SortAscending)
                        sb.Append($".OrderBy({sortColumn.ColumnName})");
                    else
                        sb.Append($".OrderByDescending({sortColumn.ColumnName})");
                }
                else
                {
                    if (sortColumn.SortAscending)
                        sb.Append($".ThenBy({sortColumn.ColumnName})");
                    else
                        sb.Append($".ThenByDescending({sortColumn.ColumnName})");
                }
            }

            // Now ask for ONLY the page of data we are looking for
            sb.Append($".Skip({Skip}).Take({PageSize})");
            return (sb.ToString());
        }

        /// <summary>
        /// Initializes the pager
        /// </summary>
        /// <param name="WhereClause">The linq expression used to filter the data being returned</param>
        /// <param name="PageSize">Number of rows of data returned a page</param>
        /// <param name="propertyLabda">Array of lambda expressions indicating column sort order. All columns are sorted ASC</param>
        public EFPager(Expression<Func<T, bool>> WhereClause, int PageSize = DefaultPageSize, params Expression<Func<T, object>>[] propertyLabda)
        {
            this.PageSize = PageSize;
            this.WhereClause = WhereClause;
            for (int i = 0; i < propertyLabda.Length; ++i)
            {
                AddSortOrderColumn(propertyLabda[i]);
            }
        }


        /// <summary>
        /// Ensures that the pager is initialized only once
        /// </summary>
        public bool Initialized { get; private set; }

        /// <summary>
        /// This method is called when we first determine the number of rows are in the result set
        /// </summary>
        /// <param name="TotalRowCount">Total rows of data to page through</param>
        public void InitializePageInfo(int TotalRowCount)
        {
            this.TotalRowCount = TotalRowCount;
            TotalPages = (int)Math.Ceiling(TotalRowCount / (double)PageSize);
            Initialized = true;
        }

        /// <summary>
        /// If there are more pages available, then increment the page index to the next page
        /// </summary>
        public void MoveToNextPage()
        {
            if (HasNextPage)
            {
                ++PageIndex;
            }
        }

        /// <summary>
        /// List of column the query will be sorted on
        /// </summary>
        public List<ColumnSortInfo<T>> SortOrder { get; private set; } = new List<ColumnSortInfo<T>>();

        /// <summary>
        /// Adds a new column to the sort order
        /// </summary>
        /// <param name="ColumnName">A lambda expression specifying the name of the column to sort on</param>
        /// <param name="SortAscending">If true, then sort this column in ascending order, else descending order</param>
        public void AddSortOrderColumn(Expression<Func<T, object>> ColumnName, bool SortAscending = true)
        {
            SortOrder.Add(new ColumnSortInfo<T>(ColumnName, SortAscending));
        }

        /// <summary>
        /// Predicate that will execute as the where clause of the query
        /// </summary>
        public Expression<Func<T, bool>> WhereClause { get; set; } = null;
    }

    public class ColumnSortInfo<T> where T : class
    {
        public bool SortAscending { get; set; }
        public Expression<Func<T, object>> ColumnName { get; set; }

        public ColumnSortInfo(Expression<Func<T, object>> ColumnName, bool SortAscending = true)
        {
            this.ColumnName = ColumnName;
            this.SortAscending = SortAscending;
        }
    }

}


