// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Knowledge.Services.Graph.Custom
{
    using System;
    using System.Collections.Generic;

    public class GraphResponse
    {
        public string apiVersion { get; set; }
        public Graph graph { get; set; }
    }

    public class Graph
    {
        public Graph()
        {
            this.nodes = new Dictionary<string, INodeInfo>();
            this.edges = new List<AbstractLinkedEdge>();
        }
        public bool directed { get; set; }
        public string type { get; set; }
        public Dictionary<string, INodeInfo> nodes { get; set; }

        public List<AbstractLinkedEdge> edges { get; set; }
    }

    public class DocumentNodeInfo : INodeInfo
    {
        public string label { get; set; }
        public string type => "Document";
        public DocumentNodeMetadata metadata { get; set; }
    }

    public class EntityNodeInfo : INodeInfo
    {
        public string label { get; set; }
        public string type => "Entity";
        public EntityMetadata metadata { get; set; }

        private HashSet<string> docids;

        public EntityNodeInfo()
        {
            if (docids == null)
            {
                docids = new HashSet<string>();
            }
        }

        public void addDocumentRef(string docid)
        {
            if (docids == null)
            {
                docids = new HashSet<string>();
            }
            docids.Add(docid);
        }
        public HashSet<string> getDocuments()
        {
            return docids;
        }
    }

    public class DocumentNodeMetadata
    {
        public DocumentNodeMetadata()
        {
            this.entities = new List<Entity>();
        }

        public string uri { get; set; }
        public string excerpt { get; set; }
        public IList<Entity> entities { get; set; }
    }

    public class Entity
    {
        public string id { get; set; }
        public int count { get; set; }
    }

    public class EntityMetadata
    {
        public string subtype { get; set; }
        public string link { get; set; }
    }

    public class AbstractLinkedEdge : IEdge, IComparable
    {
        public string source { get; set; }
        public string target { get; set; }
        public bool directed { get; set; }

        public string type { get; set; }
        public LinkedEdgeMetadata metadata { get; set; }
        int IComparable.CompareTo(object obj)
        {
            AbstractLinkedEdge c = (AbstractLinkedEdge)obj;

            if (this.metadata.weight < c.metadata.weight)
                return 1;
            if (this.metadata.weight > c.metadata.weight)
                return -1;
            else
                return 0;
        }
    }

    public class DirectLinkedEdge : AbstractLinkedEdge
    {
        public DirectLinkedEdge()
        {
            this.type = "directLink";
        }
    }

    public class InferredLinkedEdge : AbstractLinkedEdge
    {
        public InferredLinkedEdge()
        {
            this.type = "inferredLink";
        }
    }

    public class LinkedEdgeMetadata
    {
        public decimal weight { get; set; }

        public string description { get; set; }

        public int linkCount { get; set; }

        // Weight Variables
        private bool rootedEdge;

        private decimal directLinksCount = 0;
        private decimal similarLinksScore = 0;
        private decimal inferredLinksCount = 0;

        private decimal sourceEntityCount = 0;
        private decimal targetEntityCount = 0;

        public IList<EntitiesInCommon> entitiesInCommon { get; set; }

        // private int 

        public LinkedEdgeMetadata()
        {
            this.entitiesInCommon = new List<EntitiesInCommon>();
        }

        public LinkedEdgeMetadata(DocumentNodeInfo src, DocumentNodeInfo tgt, bool attachtoRoot)
        {
            this.entitiesInCommon = new List<EntitiesInCommon>();

            this.sourceEntityCount = src.metadata.entities.Count > 0 ? src.metadata.entities[0].count : 1;
            this.targetEntityCount = tgt.metadata.entities.Count > 0 ? tgt.metadata.entities[0].count : 1;

            this.rootedEdge = attachtoRoot; 

            this.CalculateWeight();
        }

        public void SetDirectLinksCount(int value)
        {
            this.directLinksCount = value;
            this.CalculateWeight();
        }
        public void SetSimilarLinksScore(decimal value, decimal min, decimal max)
        {
            if ( min == max)
            {
                this.similarLinksScore = 1;
            }
            else
            {
                this.similarLinksScore = (value-min)/(max-min);
            }

            this.CalculateWeight();
        }

        public void AddInferredLinksCount(string label)
        {
            this.inferredLinksCount += 1;
            this.entitiesInCommon.Add(new EntitiesInCommon { id = label, relevance = 1 });
            this.CalculateWeight();
        }

        private void CalculateWeight()
        {
            // When both links are present for
            // document the weights of direct
            // (wd) and inferred (wi) will be
            // combined with:
            // wd new = (3wd + 2similar + 1wi) / 6
            this.weight = 0M;

            if (this.linkCount > 0 )
            {
                this.weight += (1000M + (linkCount / directLinksCount));
            }

            if (this.similarLinksScore > 0)
            {
                this.weight += (300M + (similarLinksScore));
            }

            // 
            if (this.inferredLinksCount > 0 && this.rootedEdge )
            {
                this.weight += 150M ;
            }

            if (this.inferredLinksCount > 0)
            {
                this.weight += (50M + (2M * inferredLinksCount / (sourceEntityCount + targetEntityCount)));
            }

            this.weight /= 1500M;
        }

        public bool IsEligibleEdge()
        {
            return (this.linkCount > 0) || (this.similarLinksScore > 0 && this.entitiesInCommon.Count > 0) || (this.inferredLinksCount > 0);
        }
        public bool IsRooted()
        {
            return this.rootedEdge;
        }
    }

    public class EntitiesInCommon
    {

        public string id { get; set; }
        public int relevance { get; set; }
    }


    public interface INodeInfo
    {
        public string label { get; set; }
        public string type { get; }
    }

    public interface IEdge
    {
        public string source { get; set; }
        public string target { get; set; }
        public bool directed { get; set; }
        public string type { get; }
    }


}
