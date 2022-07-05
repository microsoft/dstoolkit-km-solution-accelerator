// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Knowledge.Services.Models.Ingress
{
    public class IngressSearchRequest
    {
        public string index_key { get; set; }
        public string document_id { get; set; }
        public int page_number { get; set; }
        public string queryText { get; set; }
        public SearchFacet[] searchFacets { get; set; }
        public string[] content_sources{ get; set; }
        public int currentPage { get; set; }
        public QueryParameters parameters { get; set; }
        public UserOptions options { get; set; }
        public SearchPermission[] permissions {  get; set; }
        public string polygonString {  get; set; }
        public string[] retrievableFields {  get; set; }
        public string incomingFilter { get; set; }
        public string indexName { get; set; }
        // Keep track of the query transformations applied to the original query text
        private SortedDictionary<string,string> _queryTransformations = new();

        public void AddQueryTransformation(string key, string value)
        {
            this._queryTransformations.Add(key, value);
        }

        public SortedDictionary<string, string> GetQueryTransformation()
        {
            return _queryTransformations;
        }
    }
}
