// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Knowledge.Models;
using Knowledge.Services.QnA;
using System.Collections.Generic;

namespace Knowledge.Services.Models
{
    // Results Object
    public class SearchResponse
    {
        public string IndexName { get; set; }

        public Dictionary<string, object> Result { get; set; }

        public IList<Dictionary<string, object>> Results { get; set; }

        public int? Count { get; set; }

        public IDictionary<string, string> Tokens { get; set; }

        public int StorageIndex { get; set; }

        public string DecodedPath { get; set; }

        public IDictionary<string, IList<FacetValue>> Facets { get; set; }

        public IDictionary<string, IList<FacetValue>> Tags { get; set; }

        public string SearchId { get; set; }

        public string IdField { get; set; }

        public bool IsSemanticSearch { get; set; }

        public bool IsPathBase64Encoded { get; set; }

        // QnA Answers
        public IList<Answer> QnaAnswers { get; set; }

        public IList<Dictionary<string, object>> SemanticAnswers { get; set; }

        public string webSearchResults { get; set; }

        public SortedDictionary<string, string> QueryTransformations { get; set; }
    }

}
