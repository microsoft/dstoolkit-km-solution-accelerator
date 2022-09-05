// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CognitiveSearch.UI.Models;
using System;

namespace CognitiveSearch.UI.Controllers
{
    public class AbstractSearchViewController : AbstractViewController
    {
        protected override SearchViewModel GetViewModel(string query = null)
        {
            var searchidId = Guid.NewGuid().ToString();

            SearchViewModel vm = new()
            {
                searchId = searchidId,
                currentQuery = query ?? "*",
                config = _uiConfig.GetVerticalById(this._viewId)
            };
            return vm;
        }
    }
}
