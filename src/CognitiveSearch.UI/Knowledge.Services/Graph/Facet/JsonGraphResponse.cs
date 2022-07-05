// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Knowledge.Services.Graph.Facet
{
    //https://jsongraphformat.info/v2.0/json-graph-schema.json

    public class JsonGraphResponse
    {
        public JsonGraph graph { get; set; }

        public JsonGraphResponse()
        {
            this.graph = new JsonGraph();
        }
    }

    public class JsonGraph
    {
        public string id { get; set; }
        public string label { get; set; }
        public bool directed { get; set; }
        public string type { get; set; }

        public IDictionary<string,object> metadata { get; set; }

        public IDictionary<string, JsonGraphNode> nodes;

        public IList<JsonGraphEdge> edges;

        public JsonGraph()
        {
            this.nodes = new Dictionary<string, JsonGraphNode>();

            this.edges = new List<JsonGraphEdge>();

            this.metadata = new Dictionary<string, object>(); 
        }
    }

    public class JsonGraphNode
    {
        public string label { get; set; }

        public IDictionary<string,object> metadata { get; set; }

        public JsonGraphNode()
        {
            metadata = new Dictionary<string, object>();
        }

        //TODO do better here
        public void SetId(int i)
        {
            this.metadata.Add("id", i);
        }
        public int GetId()
        {
            return (int)this.metadata["id"];
        }

        public string GetNodeType()
        {
            return (string)this.metadata["type"];
        }

        public int GetNodeLevel()
        {
            return (int)metadata["level"];
        }
        public string GetNodeSubType()
        {
            return (string)this.metadata["subtype"];
        }
    }

    public class JsonGraphEdge
    {
        public string id { get; set; }
        public string source { get; set; }
        public string target { get; set; }
        public string relation { get; set; }
        public bool directed { get; set; }
        public string label { get; set; }

        public IDictionary<string, object> metadata { get; set; }

        public JsonGraphEdge()
        {
            metadata = new Dictionary<string, object>();
        }
    }
}
