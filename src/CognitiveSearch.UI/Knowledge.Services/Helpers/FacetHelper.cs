// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Knowledge.Models;
using Knowledge.Services.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Knowledge.Services.Helpers
{
    public class FacetHelper
    {
        private const string AZURE_SEARCH_OR_OPERATOR = " | ";
        private const string FACET_VALUE_SEPARATOR = "||";

        public static string GenerateFacetFilter(SearchModel Model, SearchFacet[] searchFacets)
        {
            string filter = String.Empty;

            //https://docs.microsoft.com/en-us/azure/search/search-filters#filter-usage-patterns
            if (searchFacets != null)
            {
                foreach (var item in searchFacets)
                {
                    string facetFilterStr;

                    if (item.Type.Equals("static"))
                    {
                        var newfilter = String.Empty;

                        if (String.IsNullOrEmpty(item.Target))
                        {
                            // if there is no global target, look into each facet
                            newfilter = BuildTargetFacetValues(item);
                        }
                        else
                        {
                            if (item.Target.Equals("fulltext"))
                            {
                                //facetFilterStr = string.Join(" | ", item.Values.Select(valueEntry => QueryHelper.EScapeAzureSearchCharacters(valueEntry.value)).ToArray());
                                facetFilterStr = string.Join(AZURE_SEARCH_OR_OPERATOR, IterateFacetValues(item, true));
                                facetFilterStr = QueryHelper.ODataFilterSingleQuoteConstraint(facetFilterStr);

                                newfilter = $"search.ismatchscoring('{facetFilterStr}')";
                            }
                            else
                            {
                                facetFilterStr = string.Join(FACET_VALUE_SEPARATOR, IterateFacetValues(item));
                                facetFilterStr = QueryHelper.ODataFilterSingleQuoteConstraint(facetFilterStr);

                                newfilter = $"{item.Target}/any(t: search.in(t, '{facetFilterStr}', '{FACET_VALUE_SEPARATOR}'))";
                            }
                        }

                        if (string.IsNullOrEmpty(filter))
                            filter = newfilter;
                        else
                            filter += " and " + newfilter;
                    }                    
                    else if (item.Type.Equals("daterange"))
                    {
                        var newfilter = String.Empty;

                        if (item.Values[0].value.EndsWith('Z'))
                        {
                            newfilter = $"({item.Target} ge {item.Values[0].value} and {item.Target} le {item.Values[1].value})";
                        }
                        else
                        {
                            newfilter = $"({item.Target} ge {item.Values[0].value}T00:00:00Z and {item.Target} le {item.Values[1].value}T23:59:59Z)";
                        }

                        if (string.IsNullOrEmpty(filter))
                            filter = newfilter;
                        else
                            filter += " and " + newfilter;
                    }
                    else
                    {
                        var facet = Model.FindFacetField(item.Key);

                        if ( facet != null)
                        {
                            facetFilterStr = string.Join(FACET_VALUE_SEPARATOR, IterateFacetValues(item));
                            facetFilterStr = QueryHelper.ODataFilterSingleQuoteConstraint(facetFilterStr);

                            // Construct Collection(string) facet query
                            if (facet.Type == typeof(string[]))
                            {
                                if (string.IsNullOrEmpty(filter))
                                    filter = $"{item.Key}/any(t: search.in(t, '{facetFilterStr}', '{FACET_VALUE_SEPARATOR}'))";
                                else
                                    filter += $" and {item.Key}/any(t: search.in(t, '{facetFilterStr}', '{FACET_VALUE_SEPARATOR}'))";
                            }
                            // Construct string facet query
                            else if (facet.Type == typeof(string))
                            {
                                if (string.IsNullOrEmpty(filter))
                                    filter = $"{item.Key} eq '{facetFilterStr}'";
                                else
                                    filter += $" and {item.Key} eq '{facetFilterStr}'";
                            }
                            // Construct DateTime facet query
                            else if (facet.Type == typeof(DateTime))
                            {
                                // TODO: Date filters
                            }
                        }
                    }
                }
            }
            return filter;
        }

        private static string BuildTargetFacetValues(SearchFacet facet, bool phraseSearch = false)
        {
            string filter = String.Empty;
            string newfilter, facetFilterStr;

            foreach (var facetValue in facet.Values)
            {
                if (String.IsNullOrEmpty(facetValue.target))
                {
                    facetFilterStr = string.Join(" or ", BuildFacetValue(facetValue, phraseSearch));
                    facetFilterStr = QueryHelper.ODataFilterSingleQuoteConstraint(facetFilterStr);

                    newfilter = $"search.ismatch('{facetFilterStr}')";
                }
                else
                {
                    facetFilterStr = string.Join(FACET_VALUE_SEPARATOR, BuildFacetValue(facetValue, phraseSearch));
                    facetFilterStr = QueryHelper.ODataFilterSingleQuoteConstraint(facetFilterStr);

                    if (facetValue.singlevalued)
                    {
                        //fielded search since we can't evaluate a non-collection field
                        newfilter = $"search.ismatch('{facetValue.target}:{facetFilterStr}')";
                    }
                    else
                    {
                        //collection field
                        newfilter = $"{facetValue.target}/any(t: search.in(t, '{facetFilterStr}', '{FACET_VALUE_SEPARATOR}'))";
                    }
                }

                if (string.IsNullOrEmpty(filter))
                    filter = newfilter;
                else
                    filter += " and " + newfilter;

            }

            return filter;
        }

        private static List<string> BuildFacetValue(FacetValue facetValue, bool phraseSearch = false)
        {
            List<string> valuesTokens = new List<string>();

            if (facetValue.query != null && facetValue.query.Length > 0)
            {
                //valuesTokens.AddRange(facetValue.query.Select(entry => QueryHelper.EscapeAzureSearchCharacters(entry)).ToArray());
                if (phraseSearch)
                {
                    foreach (var queryItem in facetValue.query)
                    {
                        valuesTokens.Add('\"' + queryItem + '\"');
                    }
                }
                else
                {
                    valuesTokens.AddRange(facetValue.query);
                }
            }
            else
            {
                //valuesTokens.Add(QueryHelper.EscapeAzureSearchCharacters(facetValue.value));
                if (phraseSearch)
                    valuesTokens.Add('\"' + facetValue.value + '\"');
                else
                    valuesTokens.Add(facetValue.value);
            }
            return valuesTokens;
        }


        private static List<string> IterateFacetValues(SearchFacet facet, bool phraseSearch = false)
        {
            List<string> valuesTokens = new List<string>();

            foreach (var facetValue in facet.Values)
            {
                valuesTokens.AddRange(BuildFacetValue(facetValue, phraseSearch));
            }

            return valuesTokens;
        }
    }


}
