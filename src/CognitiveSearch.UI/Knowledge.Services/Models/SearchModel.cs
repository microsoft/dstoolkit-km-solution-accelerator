// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Knowledge.Services.Configuration;
using Knowledge.Services.Graph;
using System.Collections.Generic;
using System.Linq;

namespace Knowledge.Services.Models
{
    public class SearchModel
    {
        public string IndexName { get; set; }

        private List<string> graphfacets = new List<string>();
        private List<string> facets = new List<string>();
        private List<string> tags = new List<string>();

        private List<string> resultFields = new List<string>();

        public List<SearchField> GraphFacets { get; set; }
        public List<SearchField> Facets { get; set; }
        public List<SearchField> Tags { get; set; }

        public string[] AllRetrievableFields { get; set; }
        public string[] ReducedRetrievableFields { get; set; }

        public string[] SearchableFields { get; set; }

        public Dictionary<string, string[]> SearchFacets { get; set; }

        private readonly SearchSchema IndexSchema;

        // Constructor
        public SearchModel(SearchSchema schema, SearchServiceConfig configuration, GraphConfig graphConfig, string IndexName = "")
        {
            this.IndexName = IndexName;
            this.IndexSchema = schema;

            GraphFacets = new List<SearchField>();
            Facets = new List<SearchField>();
            Tags = new List<SearchField>();

            List<string> validatedResultFields = new List<string>();

            if (configuration.GetResultFields() != null) resultFields = configuration.GetResultFields();
            foreach (string s in resultFields)
            {
                if (schema.Fields.ContainsKey(s))
                {
                    validatedResultFields.Add(s);
                }
            }

            if (validatedResultFields.Count == 0)
            {
                foreach (var field in schema.Fields.Where(f => !f.Value.IsHidden))
                {
                    validatedResultFields.Add(field.Value.Name);
                }
            }

            AllRetrievableFields = validatedResultFields.ToArray();

            resultFields = configuration.GetExcludedResultFields(); 
            ReducedRetrievableFields = validatedResultFields.Where(s => !resultFields.Contains(s)).ToArray();

            // Facets 
            if (configuration.GetFacets() != null) facets = configuration.GetFacets();
            if (facets.Count() > 0)
            {
                // add field to facets if in facets array
                foreach (var field in facets)
                {
                    if (schema.Fields.ContainsKey(field) && schema.Fields[field].IsFacetable)
                    {
                        Facets.Add(schema.Fields[field]);
                    }
                }
            }
            else
            {
                foreach (var field in schema.Fields.Where(f => f.Value.IsFacetable))
                {
                    Facets.Add(field.Value);
                }
            }

            // Graph Facets
            if (graphConfig != null)
            {
                if (graphConfig.GetGraphFacets() != null) graphfacets = graphConfig.GetGraphFacets();
                if (graphfacets.Count() > 0)
                {
                    // add field to facets if in facets arr
                    foreach (var field in graphfacets)
                    {
                        if (schema.Fields.ContainsKey(field) && schema.Fields[field].IsFacetable)
                        {
                            GraphFacets.Add(schema.Fields[field]);
                        }
                    }
                }
                else
                {
                    foreach (var field in schema.Fields.Where(f => f.Value.IsFacetable))
                    {
                        GraphFacets.Add(field.Value);
                    }
                }
            }

            // Tags
            if (configuration.GetTags() != null) tags = configuration.GetTags();
            if (tags.Count() > 0)
            {
                foreach (var field in tags)
                {
                    if (schema.Fields.ContainsKey(field) && schema.Fields[field].IsFacetable)
                    {
                        Tags.Add(schema.Fields[field]);
                    }
                }
            }
            else
            {
                foreach (var field in schema.Fields.Where(f => f.Value.IsFacetable))
                {
                    Tags.Add(field.Value);
                }
            }

            SearchableFields = schema.Fields.Where(f => f.Value.IsSearchable).Select(f => f.Key).ToArray();
        }

        public SearchField FindFacetField(string key)
        {
            var facet = this.Facets.Where(f => f.Name == key).FirstOrDefault();
        
            if (facet == null)
            {
                facet = this.GraphFacets.Where(f => f.Name == key).FirstOrDefault();
            }

            // Default to corresponding Index Schema.
            if (facet == null)
            {
                if (IndexSchema.Fields.ContainsKey(key) && IndexSchema.Fields[key].IsFacetable)
                {
                    facet = IndexSchema.Fields[key];
                }
            }

            return facet;
        }
    }
}
