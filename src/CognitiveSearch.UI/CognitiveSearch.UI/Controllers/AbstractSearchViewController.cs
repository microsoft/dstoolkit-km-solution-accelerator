// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CognitiveSearch.UI.Models;
using System;
using Microsoft.AspNetCore.Authorization;

namespace CognitiveSearch.UI.Controllers
{
    [Authorize]
    public class AbstractSearchViewController : AbstractViewController
    {
        protected override SearchViewModel GetViewModel(string query = null)
        {
            SearchViewModel vm = new()
            {
                searchId = Guid.NewGuid().ToString(),
                currentQuery = query ?? "*",
                config = uiConfig.GetVerticalById(this.viewId)
            };
            return vm;
        }
    }
}
