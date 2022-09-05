// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.ApplicationInsights.DataContracts;

namespace Knowledge.Configuration
{
    public class SearchServiceConfig
    {
        public string KeyField { get; set; }

        public string ServiceName { get; set; }

        public string APIVersion { get; set; }

        public string AdminKey { get; set; }
        // Default index name
        public string IndexName { get; set; }
        // Default indexer name
        public string IndexerName { get; set; }

        public string Indexes { get; set; }

        public string Indexers { get; set; }

        public string QueryKey { get; set; }

        public string InstrumentationKey { get; set; }

        public int CacheExpirationForSchemaField { get; set; }

        // Flags 
        public bool IsPathBase64Encoded { get; set; }

        // Initial User controllable flags - User Options TODO
        public bool semanticSearchEnabled { get; set; }
        public bool TranslateQueryText { get; set; }
        public bool SpellCheckQueryText { get; set; }
        public bool SuggestionsAsFilter { get; set; }

        // Content Security Trimming
        public bool IsSecurityTrimmingEnabled { get; set; }
        public string PermissionsPublicFilter { get; set; }
        public string PermissionsProtectedFilter { get; set; }

        public string GraphFacet { get; set; }

        public string AzureMapsSubscriptionKey { get; set; }

        public bool EnableLogging { get; set; }

        public SeverityLevel MinLogLevel { get; set; }

        // Configure Facets, Tags (Facets) & retrievable fields
        public string FacetsAsString { get; set; }
        public string TagsAsString { get; set; }
        public string ResultFieldsAsString { get; set; }
        public string HHFieldsAsString { get; set; }

        // We don't need the full content of a document in most case, just the HH
        public string ExcludedResultFieldsAsString { get; set; }

        private List<string> SearchIndexes { get; set; }
        private List<string> SearchIndexers { get; set; }

        private List<string> Facets { get; set; }
        private List<string> Tags { get; set; }
        private List<string> ResultFields { get; set; }
        private List<string> HHFields { get; set; }
        private List<string> ExcludedResultFields { get; set; }

        // Cognitive Search filter strings on content type for Images & Videos
        public string ImagesFilter { get; set; }
        public string VideosFilter { get; set; }

        // Default constants 

        public static readonly int DefaultPageNumber = 1;

        public static readonly int DefaultNumberOfNeighbors = 5;

        public static readonly int DefaultPageSize = 10;

        public List<string> GetSearchIndexes()
        {
            if (SearchIndexes == null)
            {
                if (!string.IsNullOrEmpty(Indexes.Trim()))
                    SearchIndexes = new List<string>(Indexes.Split(','));
            }

            return SearchIndexes;
        }

        public List<string> GetSearchIndexers()
        {
            if (SearchIndexers == null)
            {
                if (!string.IsNullOrEmpty(Indexers.Trim()))
                    SearchIndexers = new List<string>(Indexers.Split(','));
            }

            return SearchIndexers;
        }

        public List<string> GetFacets()
        {
            if (Facets == null)
            {
                if (! string.IsNullOrEmpty(FacetsAsString.Trim()))
                    Facets = new List<string>(FacetsAsString.Split(','));
            }

            return Facets;
        }

        public List<string> GetTags()
        {
            if (Tags == null)
            {
                if (!string.IsNullOrEmpty(TagsAsString.Trim()))
                    Tags = new List<string>(TagsAsString.Split(','));
            }

            return Tags;
        }

        public List<string> GetResultFields()
        {
            if (ResultFields == null)
            {
                if (!string.IsNullOrEmpty(ResultFieldsAsString.Trim()))
                    ResultFields = new List<string>(ResultFieldsAsString.Split(','));
            }

            return ResultFields;
        }
        public List<string> GetExcludedResultFields()
        {
            if (ExcludedResultFields == null)
            {
                if (!string.IsNullOrEmpty(ExcludedResultFieldsAsString.Trim()))
                    ExcludedResultFields = new List<string>(ExcludedResultFieldsAsString.Split(','));
            }

            return ExcludedResultFields;
        }

        public List<string> GetHHFields()
        {
            if (HHFields == null)
            {
                if (!string.IsNullOrEmpty(HHFieldsAsString.Trim()))
                    HHFields = new List<string>(HHFieldsAsString.Split(','));
            }

            return HHFields;
        }


    }
}
