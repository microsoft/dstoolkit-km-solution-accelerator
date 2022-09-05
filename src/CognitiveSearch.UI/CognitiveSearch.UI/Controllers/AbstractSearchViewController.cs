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
            SearchViewModel vm = new()
            {
                searchId = Guid.NewGuid().ToString(),
                currentQuery = query ?? "*",
                config = _uiConfig.GetVerticalById(this._viewId)
            };
            return vm;
        }
    }
}
