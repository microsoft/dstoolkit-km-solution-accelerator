// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Knowledge.Services.AzureSearch.SDK;
using Knowledge.Services.Helpers;
using Knowledge.Services.Models;
using Microsoft.ApplicationInsights;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Knowledge.Services.Graph.Facet
{
    public class FacetGraphService : AbstractService, IFacetGraphService
    {
        private GraphConfig graphcfg;

        private IAzureSearchSDKService searchService;

        public FacetGraphService(TelemetryClient telemetry, IAzureSearchSDKService searchSvc, GraphConfig gConfig)
        {
            this.telemetryClient = telemetry;
            this.searchService = searchSvc;
            this.graphcfg = gConfig;
        }

        //public JsonGraphResponse GetGraphData(string query,
        //                                            List<string> facetNames,
        //                                            string graphType,
        //                                            int maxLevels = 1,
        //                                            int maxNodes = 10,
        //                                            SearchFacet[] searchFacets = null,
        //                                            string model = "Model2")
        public JsonGraphResponse GetGraphData(GraphRequest request)
        {

            // Calculate nodes for MaxLevels levels
            JsonGraphResponse graphResponse = new JsonGraphResponse();

            if (request.facets != null )
            {
                request.maxLevels = request.facets.Count;
            }

            if (request.maxLevels == 0)
            {
                request.maxLevels = graphcfg.MAX_LEVELS;
            }
            if (request.maxNodes == 0)
            {
                request.maxNodes = graphcfg.MAX_NODES;
            }


            //int MaxEdges = 90;
            int CurrentLevel = 1;
            int nodesCounter = 0;

            Dictionary<string, JsonGraphEdge> EdgeList = new Dictionary<string, JsonGraphEdge>();

            // Create a node map that will map a facet to a node - nodemap[0] always equals the q term
            Dictionary<string, JsonGraphNode> NodeMap = new Dictionary<string, JsonGraphNode>();

            // If blank search, assume they want to search everything
            if (string.IsNullOrWhiteSpace(request.queryText))
            {
                request.queryText = QueryHelper.QUERY_ALL;
            }
            else
            {
                if (!request.queryText.Equals(QueryHelper.QUERY_ALL))
                {
                    // Transliterate the query to comply to Azure Search full syntax
                    request.queryText = QueryHelper.RemoveQuestionMarkAndEscape(request.queryText);
                }
            }

            string root_key = this.GetRootKey(request.queryText);

            NodeMap[root_key] = this.createNode(nodesCounter++, root_key, "root", "query", 0, CurrentLevel, 0);

            this.ProcessFacets(request.queryText, NodeMap[root_key], request.facets, request.graphType, request.searchFacets, request.model, request.maxNodes, request.maxLevels, CurrentLevel, nodesCounter, EdgeList, NodeMap);

            // Create nodes for the response
            Dictionary<string, Dictionary<int, int>> levelsCount = new Dictionary<string, Dictionary<int, int>>();

            foreach (JsonGraphNode entry in NodeMap.Values.OrderBy(n => n.GetNodeSubType()).ThenBy(n => n.GetNodeLevel()).ThenByDescending(n => n.metadata["weight"]))
            {
                int level = entry.GetNodeLevel();
                string subtype = entry.GetNodeSubType();

                if (!levelsCount.ContainsKey(subtype)) 
                {
                    levelsCount.Add(subtype, new Dictionary<int, int>() { { 0, 0 } });
                }

                if (!levelsCount[subtype].ContainsKey(level))
                {
                    levelsCount[subtype].Add(level, 0);
                }

                if ( levelsCount[subtype][level] <= request.maxNodes)
                {
                    graphResponse.graph.nodes.Add(entry.label, entry);
                    levelsCount[subtype][level]++;
                }
            }

            // Create edges for the response
            foreach (string key in EdgeList.Keys.ToList())
            {
                JsonGraphEdge edge = EdgeList[key];

                if (graphResponse.graph.nodes.ContainsKey(edge.target) && graphResponse.graph.nodes.ContainsKey(edge.source))
                {
                    graphResponse.graph.edges.Add(edge);
                }
            }

            var edges_stats = from e in graphResponse.graph.edges
                         group e by e.metadata["level"] into g
                         select new
                         {
                             Level = g.Key,
                             Count = g.Count(),
                             TotalWeight = g.Sum(x => (int)x.metadata["weight"]),
                             AverageWeight = g.Average(x => (int)x.metadata["weight"]),
                             MinWeight = g.Min(x => (int)x.metadata["weight"]),
                             MaxWeight = g.Max(x => (int)x.metadata["weight"])
                         };

            graphResponse.graph.metadata["edges"] = edges_stats;


            var nodes_stats = from e in graphResponse.graph.edges
                         group e by e.metadata["level"] into g
                         select new
                         {
                             Level = g.Key,
                             Count = g.Count(),
                             TotalWeight = g.Sum(x => (int)x.metadata["weight"]),
                             AverageWeight = g.Average(x => (int)x.metadata["weight"]),
                             MinWeight = g.Min(x => (int)x.metadata["weight"]),
                             MaxWeight = g.Max(x => (int)x.metadata["weight"])
                         };

            graphResponse.graph.metadata["nodes"] = nodes_stats;

            return graphResponse;
        }

        private string GetRootKey(string query)
        {
            return "root_" + query;
        }

        private JsonGraphNode createNode(int relative_id, string label, string type, string subtype, int subtypeidx, int level, int count)
        {
            // This is a new node
            JsonGraphNode node = new JsonGraphNode
            {
                label = label
            };
            node.SetId(relative_id);

            node.metadata.Add("type", type);
            node.metadata.Add("subtype", subtype);
            node.metadata.Add("subtypeidx", subtypeidx);

            node.metadata.Add("level", level);
            node.metadata.Add("count", count);

            node.metadata.Add("weight", 0);
            node.metadata.Add("source_count", 0);
            node.metadata.Add("target_count", 0);

            return node;
        }


        private void ProcessFacets(string query,
                                   JsonGraphNode parent_node,
                                   List<string> facetNames,
                                   string graphType,
                                   SearchFacet[] searchFacets,
                                   string model,
                                   int maxNodes,
                                   int maxLevels,
                                   int currentLevel,
                                   int nodesCounter,
                                   Dictionary<string, JsonGraphEdge> FDEdgeList,
                                   Dictionary<string, JsonGraphNode> NodeMap)
        {
            SearchResults<SearchDocument> response;

            List<string> currentFacet = new List<string>();

            currentFacet.AddRange(facetNames); 

            if ( graphType.Equals("sunburst") || graphType.Equals("sankey"))
            {
                currentFacet = facetNames.GetRange(currentLevel - 1, 1);
            }

            if (!String.IsNullOrEmpty(model))
            {
                if (model.Equals("model1"))
                {
                    response = GetFacetsModel1(query, currentFacet, maxNodes, searchFacets);
                }
                else
                {
                    response = GetFacetsModel2(query, parent_node, currentFacet, (currentLevel == 1), maxNodes, searchFacets);
                }
            }
            else
            {
                response = GetFacetsModel2(query, parent_node, currentFacet, (currentLevel == 1), maxNodes, searchFacets);
            }

            currentLevel++;

            if (response != null)
            {
                foreach (string facetName in response.Facets.Keys)
                {
                    int facetidx = facetNames.IndexOf(facetName) + 1;

                    IList<FacetResult> facetVals = (response.Facets)[facetName];

                    // Here we need to check if the facet value is already present in the incoming search facets
                    var values = searchFacets.Where(n => n.Target.Equals(facetName)).Select(n => n.Values).SelectMany(n => n).Select(n => n.value).ToList();

                    // We will get typically maxNodes values per facet
                    foreach (FacetResult facet in facetVals)
                    {
                        if (!String.IsNullOrEmpty(facet.Value.ToString()))
                        {
                            string facet_node_value = facet.Value.ToString();

                            // If the facet value is already a filter then do nothing...
                            if (! values.Contains(facet_node_value))
                            {
                                JsonGraphNode node;

                                if (NodeMap.TryGetValue(facet_node_value, out node) == false)
                                {
                                    // This is a new node
                                    node = this.createNode(nodesCounter++, facet_node_value, "entity", facetName, facetidx, currentLevel, (int)facet.Count);

                                    NodeMap[node.label] = node;
                                }
                                else
                                {
                                    if ((int)node.metadata["level"] > currentLevel)
                                    {
                                        node.metadata["level"] = currentLevel;
                                    }
                                }

                                // Add this facet to the fd list
                                if (!parent_node.label.Equals(node.label))
                                {
                                    JsonGraphEdge edge = null;

                                    string edgekey = ("" + parent_node.label + "." + node.label);
                                    string inv_edgekey = ("" + node.label + "." + parent_node.label);

                                    if (FDEdgeList.TryGetValue(edgekey, out edge) != false || FDEdgeList.TryGetValue(inv_edgekey, out edge) != false)
                                    {
                                        // Todo add more weight to the edge as it is bi-directional
                                        edge.directed = false;
                                    }
                                    else
                                    {
                                        JsonGraphEdge newEdge = new JsonGraphEdge
                                        {
                                            id = edgekey,
                                            source = parent_node.label,
                                            target = node.label,
                                            label = edgekey,
                                            relation = facetName,
                                            directed = true
                                        };

                                        newEdge.metadata.Add("weight", NodeMap[facet_node_value].metadata["count"]);
                                        newEdge.metadata.Add("level", currentLevel);

                                        FDEdgeList.Add(edgekey, newEdge);

                                        int test = (int)parent_node.metadata["weight"];
                                        parent_node.metadata["weight"] = ++test;

                                        test = (int)parent_node.metadata["source_count"];
                                        parent_node.metadata["source_count"] = ++test;

                                        test = (int)node.metadata["target_count"];
                                        node.metadata["target_count"] = ++test;
                                    }
                                }
                            }
                        }
                    }

                }

                // Recursive
                // If there is more level to go through
                //
                // Select all entity nodes from that level and iterate through for next level
                if (currentLevel <= maxLevels)
                {
                    var entitiesNodes = NodeMap.Where(p => p.Value.GetNodeType() == "entity").Select(x => x.Value as JsonGraphNode).Where(x => x.GetNodeLevel() == currentLevel).ToList();

                    foreach (JsonGraphNode node in entitiesNodes)
                    {
                        this.ProcessFacets(query = node.label, parent_node = node, facetNames, graphType, searchFacets, model, maxNodes, maxLevels, currentLevel, nodesCounter, FDEdgeList, NodeMap);
                    }
                }
            }
        }

        private SearchResults<SearchDocument> GetFacetsModel1(string searchText,
                                                              List<String> selectFacets,
                                                              int maxCount = 10,
                                                              SearchFacet[] searchFacets = null)
        {
            if (string.IsNullOrEmpty(searchText))
            {
                searchText = ServicesConstants.QUERY_ALL;
            }

            // Execute search based on query string
            try
            {
                QueryParameters queryParameters = new() { RowCount = 10 };
                UserOptions userOptions = new();

                SearchOptions options = this.searchService.GenerateSearchOptions(queryParameters, selectFacets: selectFacets, searchFacets: searchFacets);
                options.Select.Clear();
                options.Facets.Clear();
                options.Select.Add(this.searchService.GetSearchConfig().KeyField);

                // Facets filter
                foreach (string f in selectFacets)
                {
                    options.Select.Add(f);

                    options.Facets.Add($"{f}, count:{maxCount}, sort:count");
                }

                return this.searchService.SearchDocuments(null, searchText, options);
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackException(ex);
            }
            return null;
        }

        private SearchResults<SearchDocument> GetFacetsModel2(string searchText,
                                                              JsonGraphNode node,
                                                              List<String> selectFacets,
                                                              bool initialQuery,
                                                              int maxCount = 10,
                                                              SearchFacet[] searchFacets = null)
        {
            // Execute search based on query string
            try
            {
                if (!initialQuery)
                {
                    searchFacets = new SearchFacet[] { }; 
                }

                QueryParameters queryParameters = new() { RowCount = 10 };

                UserOptions userOptions = new();

                SearchOptions options = this.searchService.GenerateSearchOptions(queryParameters, selectFacets: selectFacets, searchFacets: searchFacets);
                options.Select.Clear();
                options.Facets.Clear();
                options.Select.Add(this.searchService.GetSearchConfig().KeyField);

                // Facets & Select
                foreach (string f in selectFacets)
                {
                    options.Select.Add(f);

                    // Azure Search doesn't support dedicated Refinement Filter.
                    // So when you filter on a facet value, the facets returned will 
                    // contain the value you were filtering one...
                    int facetCount = maxCount;
                    //TODO new filter target support here
                    facetCount+=searchFacets.Where(n => n.Target.Equals(f)).ToList().Count();
                    options.Facets.Add($"{f}, count:{facetCount}, sort:count");
                }

                // This is a facet query not the initial query anymore...
                // ...here the searchText is a facet value found in the initial query
                if (!initialQuery)
                {
                    searchText = QueryHelper.ODataFilterSingleQuoteConstraint(searchText);

                    string f = (string) node.metadata["subtype"];

                    if (String.IsNullOrEmpty(options.Filter))
                    {
                        options.Filter = $"{f}/any(t: t eq '{searchText}')";
                    }
                    else
                    {
                        options.Filter += $" or {f}/any(t: t eq '{searchText}')";
                    }

                    searchText = QueryHelper.QUERY_ALL;
                }

                return this.searchService.SearchDocuments(null, searchText, options);
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackException(ex);
            }
            return null;
        }
    }
}
