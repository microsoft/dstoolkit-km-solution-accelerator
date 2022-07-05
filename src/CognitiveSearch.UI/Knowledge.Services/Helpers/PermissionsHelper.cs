// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Knowledge.Services.Models;
using System;
using System.Collections.Generic;

namespace Knowledge.Services.Helpers
{
    public class PermissionsHelper
    {
        public static string GeneratePermissionsFilter(string publicFilter, string securedFilter, SearchPermission[] permissions)
        {
            string filter = String.Empty;

            if (!String.IsNullOrWhiteSpace(publicFilter))
            {
                filter += "(" + publicFilter + ")";
            }

            //https://docs.microsoft.com/en-us/azure/search/search-filters#filter-usage-patterns
            if (permissions != null && permissions.Length > 0)
            {
                if (!String.IsNullOrWhiteSpace(securedFilter))
                {
                    filter += " or (" + securedFilter + " and ";
                }
                else
                {
                    filter += " or (";
                }

                string facetFilterStr = string.Join(",", BuildPermissionsValue(permissions));
                facetFilterStr = QueryHelper.ODataFilterSingleQuoteConstraint(facetFilterStr);

                filter += $"permissions/any(t: search.in(t, '{facetFilterStr}', ','))";

                filter += ")";
            }
            return filter;
        }

        private static List<string> BuildPermissionsValue(SearchPermission[] permissions)
        {
            List<string> valuesTokens = new List<string>();

            foreach (var facetValue in permissions)
            {
                if (! String.IsNullOrEmpty(facetValue.group))
                {
                    valuesTokens.Add(facetValue.group);
                }
            }

            return valuesTokens;
        }

    }
}
