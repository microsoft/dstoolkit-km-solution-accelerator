import logging
import os
import json

from neo4j import GraphDatabase

if 'NEO4J_ENABLED' in os.environ:
    if os.environ['NEO4J_ENABLED']:
        endpoint = os.environ["NEO4J_ENDPOINT"]
        user = os.environ["NEO4J_USERNAME"]
        password = os.environ["NEO4J_PASSWORD"]
        neo4jdriver = GraphDatabase.driver(endpoint, auth=(user, password))
    else:
        neo4jdriver = None
else: 
    neo4jdriver = None

def transform_value(record):
    try:
        recordId = record['recordId']
    except AssertionError  as error:
        return None

    # Validate the inputs
    try:
        document = {}
        document['recordId'] = recordId
        document['data'] = {}

        assert ('data' in record), "'data' field is required."
        data = record['data']

        if not neo4jdriver:
            logging.info(f'Neo4j driver is not initialized. Skipping.')
        else:
            logging.info(f'Record {recordId} Document Grapher starting...')

            with neo4jdriver.session() as session:
                # Is the document exist? Let's use Cypher MERGE
                docid,category = session.execute_write(merge_document_node_tx, data)
                logging.info(f'Node {docid} merged')

                if docid and 'document_entities' in data:
                    if 'document_entities' in data:
                        entities = data['document_entities']
                        # For each linked entity
                        if 'linked_entities' in entities:
                            linked_entities, sources = merge_linked_entities(entities['linked_entities'])
                            for entity in linked_entities.values():
                                try:
                                    mergedEntity = session.execute_write(merge_entity_node_tx, entity)
                                    logging.info(f'Linked Entity {mergedEntity} merged')
                                    if mergedEntity:
                                        # Merge the LINKED edge
                                        mergedEdge = session.execute_write(merge_document_entity_edge_tx, category, data, entity)
                                        logging.info(f'Linked Entity-Document {mergedEdge} merged')
                                except:
                                    logging.warn(f'Linked Entity {entity} graph failure.')

                            # Store the Linked Entities sources in the graph
                            for source in sources.keys():
                                try:
                                    mergedEntitySource = session.execute_write(merge_entity_source_tx, source)
                                    logging.info(f'Linked Entity Source {mergedEntitySource} merged')
                                    if mergedEntitySource:
                                        for entity in sources[source]:
                                            # Merge the SOURCE edge
                                            mergedEdge = session.execute_write(merge_document_entity_source_edge_tx, source, entity)
                                            logging.info(f'Linked Entity Source -> Entity {mergedEdge} merged')
                                except:
                                    logging.warn(f'Linked Entity Source {source} graph failure.')

                        # For each NER
                        if 'named_entities' in entities:
                            named_entities = merge_ner_entities(entities['named_entities'])
                            for entity in named_entities.values():
                                try:
                                    mergedEntity = session.execute_write(merge_ner_node_tx, entity)
                                    logging.info(f'Entity {mergedEntity} merged')
                                    if mergedEntity:
                                        # Merge the REFERENCES edge
                                        mergedEdge = session.execute_write(merge_document_ner_edge_tx, category, data, entity)
                                        logging.info(f'Entity-Document {mergedEdge} merged')
                                except:
                                    logging.warn(f'Entity {entity} graph failure.')

                    # # For each Authors
                    # if 'authors' in data:
                    #     authors = data['authors']

                    document['data']['message']='Nodes & Edges updated in Neo4j.'

    except KeyError as error:
        return (
            {
            "recordId": recordId,
            "errors": [ { "message": "KeyError:" + error.args[0] }   ]       
            })
    except AssertionError as error:
        return (
            {
            "recordId": recordId,
            "errors": [ { "message": "AssertionError:" + error.args[0] }   ]       
            })
    except SystemError as error:
        return (
            {
            "recordId": recordId,
            "errors": [ { "message": "SystemError:" + error.args[0] }   ]       
            })
    except AttributeError as error:
        return (
            {
            "recordId": recordId,
            "errors": [ { "message": "AttributeError:" + error.args[0] }   ]       
            })

    return (document)

# Document / Image / Slide / Page / Translation

# def is_document(data):

#     metadata = data['document_metadata']

# # page_number
# # restricted 
# # document_embedded
# # document_converted 
# # document_translated
# # document_translatable
# # content_group
# # content_source
# # creation_Date
# # last_modified_date
# # source_processing_date

#     if not metadata['document_embedded'] and metadata
#     return True

#     # if 'document_parent' in data:
#         # Create the parent link

def is_image(data):
    metadata = data['document_metadata']
    if not bool(metadata['document_embedded']) and metadata['content_group']=='Image':
        return True
    else:
        return False

def is_slide(data):
    metadata = data['document_metadata']
    if bool(metadata['document_converted']) and bool(metadata['document_embedded']) and metadata['content_group']=='Image':
        if 'document_parent' in data:
            content_group=data['document_parent']['content_group']
            if content_group == "PowerPoint":
                return True
            else:
                return False
        else:
            return False
    else:
        return False

def is_page(data):
    metadata = data['document_metadata']
    if bool(metadata['document_converted']) and bool(metadata['document_embedded']) and metadata['content_group']=='Image':
        if 'document_parent' in data:
            content_group=data['document_parent']['content_group']
            if content_group == "PDF":
                return True
            else:
                return False
        else:
            return False
    else:
        return False

def get_metadata(metadata, key, default=''):
    if key in metadata:
        return metadata[key]
    else:
        return default

def merge_document_node_tx(tx, document):

    metadata = document['document_metadata']

    # Capture all metadata
    dockey=document['document_index_key']
    title=document['document_metadata']['title']

    page_number=get_metadata(metadata,'page_number',0)
    last_modified=get_metadata(metadata,'last_modified')

    parent_node=False

    if is_image(document):
        category='Image'
        CIPHER_QUERY=f'MERGE (n:Image {{key: "{dockey}", name: "{title}", last_modified:"{last_modified}"}}) RETURN n.key AS result'
    elif is_slide(document):
        category='Slide'
        CIPHER_QUERY=f'MERGE (n:Slide {{key: "{dockey}", name: "{title}", number:{page_number}, last_modified:"{last_modified}"}}) RETURN n.key AS result'
        parent_node = True
    elif is_page(document):
        category='Page'
        CIPHER_QUERY=f'MERGE (n:Page {{key: "{dockey}", name: "{title}", number:{page_number}, last_modified:"{last_modified}"}}) RETURN n.key AS result'
        parent_node = True
    else:
        category='Document'
        CIPHER_QUERY=f'MERGE (n:Document {{key: "{dockey}", name: "{title}", last_modified:"{last_modified}"}}) RETURN n.key AS result'

    result = tx.run(CIPHER_QUERY)
    noderecord = result.single()

    if parent_node:
        # Parent relationship
        if 'document_parent' in document:
            parentkey=document['document_parent']['key']
            CIPHER_QUERY=f'MATCH (parent:Document {{key: "{parentkey}"}}), (doc:NODETYPE {{key: "{dockey}"}}) MERGE (parent)<-[r:PARENT]-(doc) RETURN type(r) AS result'
            result = tx.run(CIPHER_QUERY.replace("NODETYPE",category))
            parent_record = result.single()
        
    return noderecord["result"], category

# Named Entities (NER)

def get_nerent_id (entity):
    entid = None
    if 'text' in entity:
        entid = entity['text']
    return entid

def merge_ner_entities(entities):
    merged={}
    for entity in entities:
        entid = get_nerent_id(entity)
        if not entid in merged:
            merged[entid]=entity
            merged[entid]['count']=1
        else:
            # TODO something clever to store multiple instances of an Entity...Aggregation data
            merged[entid]['count']+=1

    return merged

def merge_ner_node_tx(tx, entity):
    # "category": "Organization",
    # "subcategory": null,
    # "length": 19,
    # "offset": 0,
    # "confidenceScore": 1.0,
    # "text": "Contoso Corporation"
    name=entity['text']
    # score=entity['confidenceScore']

    CIPHER_QUERY=f'MERGE (n:CATEGORY {{name: "{name}", key:"{name}"}}) RETURN n.key AS result'

    # query = ("MERGE (n:CATEGORY {name: '$name', key:'$name', confidenceScore:$score}) RETURN n.key AS result").replace("CATEGORY",entity['category'])
    # result = tx.run(query, name=entity['text'], score=entity['confidenceScore'])
    result = tx.run(CIPHER_QUERY.replace("CATEGORY",entity['category']))
    record = result.single()
    return record["result"]

def merge_document_ner_edge_tx(tx, category, document, entity):
    # MATCH
    #   (charlie:Person {name: 'Charlie Sheen'}),
    #   (wallStreet:Movie {title: 'Wall Street'})
    # MERGE (charlie)-[r:ACTED_IN]->(wallStreet)
    # RETURN charlie.name, type(r), wallStreet.title
    docid=document['document_index_key']
    entid=entity['text']
    entcat=entity['category']
    # score=entity['confidenceScore']
    count=entity['count']

    # Query
    CIPHER_QUERY=f'MATCH (ent:CATEGORY {{key:"{entid}"}}), (doc:NODETYPE {{key:"{docid}"}}) MERGE (doc)-[r:REFERENCES {{count:{count}}}]->(ent) RETURN type(r) AS result'
    result = tx.run(CIPHER_QUERY.replace("CATEGORY",entcat).replace("NODETYPE",category))
    record = result.single()
    return record["result"]

# Linked Entities

def get_linkent_id (entity):
    entid = None
    if 'id' in entity:
        entid = entity['id']
    if not entid and 'name' in entity:
        entid = entity['name']
    return entid

def merge_linked_entities(entities):
    merged={}
    sources={}
    for entity in entities:
        entid = get_linkent_id(entity)
        if entid in merged:
            if 'matches' in merged[entid]:
                # TODO - keep a mesh of the offsets
                merged[entid]['matches']+=entity['matches']
            else:
                merged[entid]['matches']=entity['matches']
        else: 
            merged[entid]=entity

        if 'dataSource' in entity:
            sourceid=entity['dataSource']
            if sourceid in sources:
                sources[sourceid]+=[{'entid':entid, 'url': entity['url']}]
            else:
                sources[sourceid]=[{'entid':entid, 'url': entity['url']}]

    return merged, sources

def merge_entity_node_tx(tx, entity):
    entid = get_linkent_id(entity)
    name=entity['name']
    CIPHER_QUERY=f'MERGE (n:Entity {{key:"{entid}", name: "{name}"}}) RETURN n.key AS result'
    result = tx.run(CIPHER_QUERY)
    record = result.single()
    return record["result"]

def merge_document_entity_edge_tx(tx, category, document, entity):
    # Variables
    docid=document['document_index_key']
    entid = get_linkent_id(entity)
    strength=len(entity['matches'])
    # Query
    CIPHER_QUERY=f'MATCH (ent:Entity {{key: "{entid}"}}), (doc:NODETYPE {{key: "{docid}"}}) MERGE (doc)-[r:LINKED {{strength:{strength}}}]->(ent) RETURN type(r) AS result'
    result = tx.run(CIPHER_QUERY.replace("NODETYPE",category))
    record = result.single()
    return record["result"]

def merge_entity_source_tx(tx, name):
    CIPHER_QUERY=f'MERGE (n:EntitySource {{name:"{name}"}}) RETURN n.name AS result'
    result = tx.run(CIPHER_QUERY)
    record = result.single()
    return record["result"]

def merge_document_entity_source_edge_tx (tx, key, value):
    entid=value['entid']
    url=value['url']
    CIPHER_QUERY=f'MATCH (ent:Entity {{key: "{entid}"}}), (src:EntitySource {{name: "{key}"}}) MERGE (ent)-[r:SOURCED {{url:"{url}"}}]->(src) RETURN type(r) AS result'
    result = tx.run(CIPHER_QUERY)
    record = result.single()
    return record["result"]
