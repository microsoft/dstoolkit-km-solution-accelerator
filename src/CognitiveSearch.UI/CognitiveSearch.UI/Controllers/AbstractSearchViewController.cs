// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CognitiveSearch.UI.Models;
using Knowledge.Services;
using Knowledge.Services.Configuration;

namespace CognitiveSearch.UI.Controllers
{
    public class AbstractSearchViewController : AbstractViewController
    {
        protected SearchServiceConfig _searchConfig { get; set; }

        protected IQueryService _queryService { get; set; }

        protected override SearchViewModel GetViewModel(string query = null)
        {
            var searchidId = _queryService.GetSearchId().ToString();

            SearchViewModel vm = new()
            {
                searchId = searchidId,
                currentQuery = query ?? "*",
                config = _uiConfig.GetVerticalById(this._viewId)
            };
            return vm;
        }

        /// <summary>
        /// Checks that the search client was intiailized successfully.
        /// If not, it will add the error reason to the ViewBag alert.
        /// </summary>
        /// <returns>A value indicating whether the search client was initialized succesfully.</returns>
        public bool CheckDocSearchInitialized()
        {
            if (_queryService == null)
            {
                ViewBag.Style = "alert-warning border-warning";
                ViewBag.Message = _configurationError;
                return false;
            }

            return true;
        }
    }

}
