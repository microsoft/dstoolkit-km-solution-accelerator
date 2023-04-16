// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Services.Graph.Custom
{
    using Knowledge.Configuration;
    using Knowledge.Configuration.Graph;
    using Knowledge.Services.AzureSearch;
    using Knowledge.Services.Helpers;
    using Microsoft.ApplicationInsights;
    using Microsoft.Extensions.Caching.Distributed;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class GraphService : AbstractSearchService, IGraphCustomService
    {
        private readonly List<string> ignoreFields = new();

        private readonly new GraphConfig config;

        public GraphService(TelemetryClient telemetry, IDistributedCache cache, SearchServiceConfig serviceConfig, GraphConfig gConfig)
        {
            this.telemetryClient = telemetry;
            this.distCache = cache;
            this.serviceConfig = serviceConfig;
            this.config = gConfig;

            InitSearchClients();

            // Initialize the Ignore Fields list
            if (! string.IsNullOrWhiteSpace(config.IgnoreFieldsForEntityNode))
            {
                ignoreFields = config.IgnoreFieldsForEntityNode.Split(',').ToList();
            }
        }

        public async Task<GraphResponse> GenerateGraph(GraphRequest graphEntity)
        {
            LoggerHelper.Instance.LogVerbose($"Start: Invoked GenerateGraph method");

            return await Task.Run(() => new GraphResponse());

            //// Default value for number of children
            //if (graphEntity.NumberOfNeighbors == 0)
            //{
            //    graphEntity.NumberOfNeighbors = SearchServiceConfig.DefaultNumberOfNeighbors;
            //}

            //var graphResponse = new GraphResponse();

            //var NodeMap = new Dictionary<string, INodeInfo>();
            //var EdgeMap = new List<AbstractLinkedEdge>();

            //var queryEntity = new SearchRequest
            //{
            //    PageNumber = 1,
            //    PageSize = 10,
            //    QueryText = "*"
            //};

            //var searchResponse = new SearchResponse
            //{
            //    SearchId = Guid.NewGuid().ToString()
            //};

            //LoggerHelper.Instance.LogVerbose($"Run Azure Search query by main document id {graphEntity.DocumentId}");

            //AzureSearchServiceResponse res= await GetDocumentFromAzureSearch(SearchResponse, queryEntity, new List<string>() { "keyPhrases" }, $"document_id eq '{graphEntity.DocumentId}'");

            //var rootDocuments = res.awsResults; 

            //Graph graph = new Graph
            //{
            //    type = "document_document_graph"
            //};

            //if (rootDocuments != null && rootDocuments.Any())
            //{
            //    LoggerHelper.Instance.LogVerbose($"Process result of Azure Search Root Document id {graphEntity.DocumentId}");
            //    var rootDocument = (DocumentResultItem)rootDocuments.FirstOrDefault();
            //    var documentNodeInfo = new DocumentNodeInfo();
            //    documentNodeInfo.label = rootDocument.DocumentTitle.Text;
            //    var docMetadata = new DocumentNodeMetadata();
            //    docMetadata.uri = rootDocument.DocumentURI;
            //    docMetadata.excerpt = rootDocument.DocumentExcerpt?.Text;

            //    docMetadata.entities = AddDocumentAttributesToGraph(NodeMap, rootDocument);

            //    // Root Document Node 
            //    documentNodeInfo.metadata = docMetadata;
            //    NodeMap.Add(rootDocument.DocumentId, documentNodeInfo);

            //    string filterFieldValue = String.Empty;

            //    // FIND SIMILAR OR DIRECTLY LINKED DOCUMENTS 

            //    IList<IResultItem> directlyLinkedDocuments = new List<IResultItem>();

            //    List<string> filterFieldValueList = new List<string>();

            //    //
            //    // Find directly linked documents 
            //    //
            //    if (serviceConfig.FilterByLinksForGraph && !string.IsNullOrWhiteSpace(serviceConfig.PrimaryFilterFieldForGraph))
            //    {
            //        LoggerHelper.Instance.LogVerbose($"Find linked document by primary filter");

            //        // Fetch the links attribute
            //        filterFieldValueList = ExtractLinks(rootDocument);

            //        if (filterFieldValueList != null && filterFieldValueList.Any())
            //        {
            //            foreach (string val in filterFieldValueList.Distinct())
            //            {
            //                // string escaped_value = QueryHelper.EscapeQueryText(val.Trim());
            //                string escaped_value = val.Trim();

            //                filterFieldValue += $"document_id eq '{escaped_value}' , ";
            //            }

            //            if (!string.IsNullOrWhiteSpace(filterFieldValue))
            //            {
            //                filterFieldValue = filterFieldValue.Remove(filterFieldValue.Length - 2);
            //            }

            //            filterFieldValue = filterFieldValue.Replace(",", "or");

            //            // Directly linked documents
            //            AzureSearchServiceResponse res1  = await GetDocumentFromAzureSearch(SearchResponse, queryEntity, new List<string>() { "keyPhrases" }, $"({filterFieldValue} or links/any(g: search.in(g, '{graphEntity.DocumentId}'))) and document_id ne '{graphEntity.DocumentId}'");
            //            directlyLinkedDocuments = res1.awsResults;
            //        }
            //        else
            //        {
            //            // Directly linked documents
            //            AzureSearchServiceResponse res2 = await GetDocumentFromAzureSearch(SearchResponse, queryEntity, new List<string>() { "keyPhrases" }, $"(links/any(g: search.in(g, '{graphEntity.DocumentId}'))) and document_id ne '{graphEntity.DocumentId}'");
            //            directlyLinkedDocuments = res2.awsResults;
            //        }
            //    }

            //    //
            //    // Add directly links & similar Documents to the graph
            //    //
            //    int nodesCount = 0;
            //    LoggerHelper.Instance.LogVerbose(string.Format("Adding linked {0} document nodes to graph", directlyLinkedDocuments.Count));

            //    foreach (DocumentResultItem relatedDoc in directlyLinkedDocuments)
            //    {
            //        if ( AddLinkedDocumentsToGraph(NodeMap, EdgeMap, rootDocument, relatedDoc, filterFieldValueList) ) nodesCount++;
            //    }

            //    LoggerHelper.Instance.LogVerbose(string.Format("Added linked {0} document nodes to graph", nodesCount));

            //    if ( nodesCount < graphEntity.NumberOfNeighbors )
            //    {
            //        // 
            //        // FIND SIMILAR DOCUMENTS
            //        //
            //        queryEntity.QueryText = rootDocument.GetAzureDocumentKey();
            //        IList<IResultItem> similarLinkedDocuments = await GetSimilarDocuments(SearchResponse, queryEntity);

            //        if (similarLinkedDocuments.Count > 0)
            //        {
            //            decimal maxScore = ((DocumentResultItem)similarLinkedDocuments[0]).GetScore();
            //            decimal minScore = ((DocumentResultItem)similarLinkedDocuments[0]).GetScore();

            //            if (similarLinkedDocuments.Count > 1)
            //            {
            //                minScore = ((DocumentResultItem)similarLinkedDocuments[similarLinkedDocuments.Count - 1]).GetScore();
            //            }

            //            LoggerHelper.Instance.LogVerbose(string.Format("Adding similar {0} document nodes to graph", similarLinkedDocuments.Count));
            //            foreach (DocumentResultItem relatedDoc in similarLinkedDocuments)
            //            {
            //                AddSimilarDocumentsToGraph(NodeMap, EdgeMap, rootDocument, relatedDoc, 0, maxScore) ;
            //            }
            //        }
            //    }

            //    //
            //    // Load all keyPhrases entities into the graph
            //    //

            //    // Add the InferredLinkedEdgeMetadata from Entities Nodes
            //    //var entitiesNodes = NodeMap.Where(p => p.Value.type == "Entity").Select(x => x.Value as EntityNodeInfo).Where(x => x.metadata.subtype == "keyPhrases" && x.getDocuments().Count > 1).ToList();
            //    var entitiesNodes = NodeMap.Where(p => p.Value.type == "Entity").Select(x => x.Value as EntityNodeInfo).Where(x => x.metadata.subtype == "keyPhrases" && x.getDocuments().Count > 0).ToList();

            //    foreach (EntityNodeInfo entityNode in entitiesNodes)
            //    {
            //        if (entityNode.getDocuments() != null)
            //        {
            //            if (entityNode.getDocuments().Count > 1)
            //            {
            //                IList<string> docs = entityNode.getDocuments().ToList<string>();

            //                for (int i = 0; i < docs.Count; i++)
            //                {
            //                    for (int j = i + 1; j < docs.Count; j++)
            //                    {
            //                        string sourceid = docs[i];
            //                        string targetid = docs[j];

            //                        var existingEdge = (AbstractLinkedEdge)EdgeMap.FirstOrDefault(x => string.Equals(x.source, sourceid, StringComparison.OrdinalIgnoreCase) && string.Equals(x.target, targetid, StringComparison.OrdinalIgnoreCase));

            //                        DocumentNodeInfo source = (DocumentNodeInfo)NodeMap[sourceid];
            //                        DocumentNodeInfo target = (DocumentNodeInfo)NodeMap[targetid];

            //                        if (existingEdge == null)
            //                        {
            //                            var edgeMetadata = new LinkedEdgeMetadata(source, target, false);
            //                            edgeMetadata.description = $"Linked by {entityNode.label}.";

            //                            InferredLinkedEdge edge = new InferredLinkedEdge
            //                            {
            //                                source = docs[i],
            //                                target = docs[j],
            //                                metadata = edgeMetadata
            //                            };

            //                            existingEdge = edge;

            //                            EdgeMap.Add(edge);
            //                        }
            //                        else
            //                        {
            //                            existingEdge.metadata.description += $" Linked by {entityNode.label}.";
            //                        }

            //                        if (existingEdge.metadata.entitiesInCommon == null)
            //                        {
            //                            existingEdge.metadata.entitiesInCommon = new List<EntitiesInCommon>();
            //                        }

            //                        existingEdge.metadata.AddInferredLinksCount(entityNode.label);
            //                    }
            //                }
            //            }
            //        }
            //    }

            //    // GRAPH RESPONSE 
            //    graph.nodes = new Dictionary<string, INodeInfo>();
            //    graph.nodes.Add(rootDocument.DocumentId, documentNodeInfo);

            //    // Sort the graph edges based on weight and grab the corresponding nodes 
            //    EdgeMap.Sort();

            //    int countnodes = 0;

            //    foreach (var edge in EdgeMap)
            //    {
            //        if ( edge.metadata.IsEligibleEdge() )
            //        {
            //            if (countnodes < graphEntity.NumberOfNeighbors && edge.metadata.IsRooted() )
            //            {
            //                if (!graph.nodes.ContainsKey(edge.source))
            //                {
            //                    graph.nodes.Add(edge.source, NodeMap[edge.source]);
            //                    countnodes++;
            //                }

            //                if (!graph.nodes.ContainsKey(edge.target))
            //                {
            //                    graph.nodes.Add(edge.target, NodeMap[edge.target]);
            //                    countnodes++;
            //                }
            //            }

            //            if (graph.nodes.ContainsKey(edge.source) && graph.nodes.ContainsKey(edge.target))
            //            {
            //                graph.edges.Add(edge);
            //            }
            //        }
            //    }

            //}

            //graphResponse.graph = graph;

            //LoggerHelper.Instance.LogVerbose($"End: Invoked GenerateGraph method");
            //return graphResponse;
        }

        //private List<string> ExtractLinks(SearchDocument rootDocument)
        //{
        //    return rootDocument.DocumentAttributes.FirstOrDefault(x => string.Equals(x.Key, serviceConfig.PrimaryFilterFieldForGraph, StringComparison.OrdinalIgnoreCase))?.Value?.StringListValue;
        //}

        //private void AddFacetsNodesToGraph(Dictionary<string, INodeInfo> NodeMap, SearchResponse SearchResponse)
        //{
        //    foreach (var facet in SearchResponse.FacetResults)
        //    {
        //        if ( ignoreFields.Any(s => s.Contains(facet.DocumentAttributeKey))) { continue; }

        //        foreach (var pair in facet.DocumentAttributeValueCountPairs)
        //        {
        //            if (!NodeMap.ContainsKey(pair.DocumentAttributeValue.StringValue))
        //            {
        //                var entityMetadata = new EntityMetadata
        //                {
        //                    subtype = facet.DocumentAttributeKey
        //                };
        //                var entityNode = new EntityNodeInfo
        //                {
        //                    label = pair.DocumentAttributeValue.StringValue,
        //                    metadata = entityMetadata
        //                };
        //                NodeMap.Add(entityNode.label, entityNode);
        //            }
        //        }
        //    }
        //}

        //private List<Entity> AddDocumentAttributesToGraph(Dictionary<string, INodeInfo> NodeMap, SearchDocument document)
        //{
        //    List<Entity> entities = new List<Entity>(); 

        //    foreach (var docattr in document.DocumentAttributes)
        //    {
        //        if ( ignoreFields.Any(s => s.Contains(docattr.Key))) { continue; }

        //        if (string.Equals(docattr.Key, "keyPhrases", StringComparison.OrdinalIgnoreCase))
        //        {
        //            // Build a list of Attribute values
        //            List<string> values = new List<string>();
        //            if (docattr.Value.StringValue != null)
        //            {
        //                values.Add(docattr.Value.StringValue);
        //            }
        //            if (docattr.Value.StringListValue != null)
        //            {
        //                values.AddRange(docattr.Value.StringListValue);
        //            }

        //            // Document list of entities
        //            if (values.Count > 0)
        //            {
        //                entities.Add(new Entity() { count = values.Count, id = docattr.Key });
        //            }

        //            foreach (var value in values)
        //            {
        //                if (!NodeMap.ContainsKey(value))
        //                {
        //                    var entityMetadata = new EntityMetadata
        //                    {
        //                        subtype = docattr.Key
        //                    };
        //                    var entityNode = new EntityNodeInfo
        //                    {
        //                        label = value,
        //                        metadata = entityMetadata
        //                    };
        //                    entityNode.addDocumentRef(document.DocumentURI);
        //                    NodeMap.Add(entityNode.label, entityNode);
        //                }
        //                else
        //                {
        //                    EntityNodeInfo node = (EntityNodeInfo)NodeMap[value];
        //                    node.addDocumentRef(document.DocumentURI);
        //                }
        //            }
        //        }
        //    }

        //    return entities;
        //}

        //// Add a similar linked document to the graph
        //private bool AddLinkedDocumentsToGraph(Dictionary<string, INodeInfo> NodeMap, List<AbstractLinkedEdge> EdgeMap, SearchDocument rootDocument, SearchDocument childDocument, List<string> filterFieldValueList)
        //{
        //    // Add the Child Document Node
        //    var childDocumentNodeInfo = new DocumentNodeInfo();
        //    childDocumentNodeInfo.label = childDocument.DocumentTitle.Text;

        //    var docMetadata = new DocumentNodeMetadata();
        //    docMetadata.uri = childDocument.DocumentURI;
        //    docMetadata.excerpt = childDocument.DocumentExcerpt?.Text;
        //    docMetadata.entities = AddDocumentAttributesToGraph(NodeMap, childDocument);

        //    childDocumentNodeInfo.metadata = docMetadata;

        //    if (!NodeMap.ContainsKey(childDocument.DocumentId))
        //    {
        //        NodeMap.Add(childDocument.DocumentId, childDocumentNodeInfo);

        //        // Create the Direct Link edge
        //        DocumentNodeInfo rootNode = (DocumentNodeInfo)NodeMap[rootDocument.DocumentId];

        //        var edgeMetadata = new LinkedEdgeMetadata(rootNode, childDocumentNodeInfo, true);

        //        edgeMetadata.description = serviceConfig.FilterByLinksForGraph ? $"Direct Link." : $"Linked by {serviceConfig.FallbackFilterFieldForGraph}.";

        //        if (filterFieldValueList != null)
        //        {
        //            // Root => Documents
        //            edgeMetadata.linkCount = filterFieldValueList.Count(x => x.Equals(childDocument.DocumentId, StringComparison.OrdinalIgnoreCase));
        //            edgeMetadata.SetDirectLinksCount(filterFieldValueList.Count);

        //        }

        //        if (edgeMetadata.linkCount == 0)
        //        // Documents => root
        //        {
        //            List<string> pointingLinks = ExtractLinks(childDocument);

        //            if ( pointingLinks != null )
        //            {
        //                edgeMetadata.linkCount += pointingLinks.Count(x => x.Equals(rootDocument.DocumentId, StringComparison.OrdinalIgnoreCase));
        //                edgeMetadata.SetDirectLinksCount(pointingLinks.Count);
        //            }
        //        }

        //        DirectLinkedEdge edge = new DirectLinkedEdge
        //        {
        //            source = rootDocument.DocumentId,
        //            target = childDocument.DocumentId,
        //            metadata = edgeMetadata
        //        };

        //        EdgeMap.Add(edge);

        //        return true; 
        //    }

        //    return false; 
        //}

        //// Add a similar linked document to the graph
        //private bool AddSimilarDocumentsToGraph(Dictionary<string, INodeInfo> NodeMap, List<AbstractLinkedEdge> EdgeMap, SearchDocument rootDocument, SearchDocument childDocument, decimal min, decimal max)
        //{
        //    // Add the Child Document Node
        //    var childDocumentNodeInfo = new DocumentNodeInfo();
        //    childDocumentNodeInfo.label = childDocument.DocumentTitle.Text;

        //    var docMetadata = new DocumentNodeMetadata();
        //    docMetadata.uri = childDocument.DocumentURI;
        //    docMetadata.excerpt = childDocument.DocumentExcerpt?.Text;
        //    docMetadata.entities = AddDocumentAttributesToGraph(NodeMap, childDocument);

        //    childDocumentNodeInfo.metadata = docMetadata;

        //    if (!NodeMap.ContainsKey(childDocument.DocumentId))
        //    {
        //        NodeMap.Add(childDocument.DocumentId, childDocumentNodeInfo);

        //        // Create the Direct Link edge
        //        DocumentNodeInfo rootNode = (DocumentNodeInfo)NodeMap[rootDocument.DocumentId];

        //        var edgeMetadata = new LinkedEdgeMetadata(rootNode, childDocumentNodeInfo, true)
        //        {
        //            description = $"Similar Link."
        //        };
        //        // Normalize the ranking score of the Similar Documents
        //        edgeMetadata.SetSimilarLinksScore(childDocument.GetScore(),min,max);

        //        InferredLinkedEdge edge = new InferredLinkedEdge
        //        {
        //            source = rootDocument.DocumentId,
        //            target = childDocument.DocumentId,
        //            metadata = edgeMetadata
        //        };

        //        EdgeMap.Add(edge);

        //        return true; 
        //    }
        //    else
        //    {
        //        LoggerHelper.Instance.LogVerbose($"Already a direct Neighbor.");
        //    }

        //    return false;
        //}
    }
}
