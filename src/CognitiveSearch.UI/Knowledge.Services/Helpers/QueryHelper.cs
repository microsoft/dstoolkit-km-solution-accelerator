// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Services.Helpers
{
    using Knowledge.Models;
    using Knowledge.Models.Ingress;
    using System;

    public static class QueryHelper
    {
        private static readonly char[] AZURE_SEARCH_ESCAPE_CHARACTERS = {'\\','+','-','&','|','!','(',')','{','}','[',']','^','"','~','*','?',':','/'};

        public const string QUERY_ALL = "*"; 

        public static void EnsureDefaultValues(IngressSearchRequest request)
        {
            if (string.IsNullOrEmpty(request.queryText))
            {
                request.queryText = QUERY_ALL;
            }

            if ( request.currentPage == 0 ) request.currentPage = 1;

            if ( request.options == null) request.options = new UserOptions();
        }

        public static void EscapeQueryText(SearchRequest query)
        {
            if (!query.QueryText.Equals(QUERY_ALL))
            {
                query.EscapedQueryText = RemoveQuestionMarkAndEscape(query.QueryText);

                query.EscapedTranslatedQueryText = RemoveQuestionMarkAndEscape(query.TranslatedQueryText);
            }
            else
            {
                query.EscapedQueryText = query.QueryText;
                query.EscapedTranslatedQueryText = query.TranslatedQueryText;
            }
        }

        public static string RemoveQuestionMarkAndEscape(string text)
        {
            text = text.Trim();

            // Azure Search Special Characters
            if (text.EndsWith(" ?") || text.EndsWith("?"))
            {
                text = text.TrimEnd('?').TrimEnd();
            }

            foreach (char item in AZURE_SEARCH_ESCAPE_CHARACTERS)
            {
                text = text.Replace(new string(item, 1), "\\" + item);
            }

            return text;
        }
        public static string EscapeAzureSearchCharacters(string text)
        {
            if ( text != null)
            {
                text = text.Trim();

                // Azure Search Special Characters
                foreach (char item in AZURE_SEARCH_ESCAPE_CHARACTERS)
                {
                    text = text.Replace(new string(item, 1), "\\" + item);
                }
                return text;
            }
            else
            {
                return String.Empty;
            }
        }

        public static string ConvertToStringSingleEscape(object value)
        {
            return Convert.ToString(value).Replace("\\\"", "\"");
        }

        public static string ODataFilterSingleQuoteConstraint(string text)
        {
            return text.Replace("'","''");
        }

        public static string AddFilter(string filter, string filterToAdd)
        {
            if (!String.IsNullOrEmpty(filterToAdd))
            {
                if (filter.Length > 0)
                {
                    filter += " and (" + filterToAdd + ")";
                }
                else
                {
                    filter += filterToAdd;
                }
            }

            return filter;
        }
    }
}
