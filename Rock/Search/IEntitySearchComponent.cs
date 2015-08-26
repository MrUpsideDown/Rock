using System;
using System.Collections.Generic;
using System.Linq;

namespace Rock.Search
{
    /// <summary>
    /// A component that implements a search resulting in a set of Entities of a specific type.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity returned by the search.</typeparam>
    public interface IEntitySearchComponent<out TEntity>
    {
        /// <summary>
        /// A unique label by which the search is identified in the user interface.
        /// </summary>
        string SearchLabel { get; }

        /// <summary>
        /// The URL specifying the page that will process and display the detailed search results.
        /// </summary>
        string ResultUrl { get; }

        /// <summary>
        /// Gets a Query that returns a list of descriptions of matching entities for the specified search string.
        /// </summary>
        /// <param name="searchString">The search string.</param>
        /// <returns></returns>
        IQueryable<string> Search( string searchString );

        /// <summary>
        /// Gets a Query that returns a list of matching entities for the specified search string.
        /// </summary>
        /// <param name="searchString">The search string.</param>
        /// <returns></returns>
        IOrderedQueryable<TEntity> GetResultsQuery( string searchString );

        /// <summary>
        /// Gets a Query that returns a list of suggested alternate searches for the specified search string.
        /// Suggested searches are only requested if there are no results for the search.
        /// </summary>
        /// <param name="searchString">The search string.</param>
        /// <param name="excludedEntityKeys">The key values of entities to exclude from the list of search suggestions.</param>
        /// <returns>An ordered query providing a list of suggested searches, or null if no suggestions are available.</returns>
        List<string> GetSearchSuggestions( string searchString, List<int> excludedEntityKeys = null );
    }
}