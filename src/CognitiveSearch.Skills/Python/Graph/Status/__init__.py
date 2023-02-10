# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

import logging
import socket
import os
import azure.functions as func

from neo4j import GraphDatabase

def main(req: func.HttpRequest) -> func.HttpResponse:
    logging.info('Python HTTP trigger function processed a request.')

    neo4jdriver = None

    if 'NEO4J_ENABLED' in os.environ:
        enabled = os.getenv("NEO4J_ENABLED", 'False').lower() in ('true', '1', 't')
        if enabled:
            endpoint = os.environ["NEO4J_ENDPOINT"]
            user = os.environ["NEO4J_USERNAME"]
            password = os.environ["NEO4J_PASSWORD"]
            try:
                neo4jdriver = GraphDatabase.driver(endpoint, auth=(user, password))
            except:
                neo4jdriver = None

    if neo4jdriver:
        with neo4jdriver.session() as session:
            message = f'Neo4j connection is alive !'
    else:
        message = f'Neo4j connection is not initialized...'

    return func.HttpResponse(message)