# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

import logging
import json
import azure.functions as func
import azure.durable_functions as df
from .. import DocumentTranslation

async def main(req: func.HttpRequest, starter: str) -> func.HttpResponse:
    client = df.DurableOrchestrationClient(starter)

    try:
        body = json.dumps(req.get_json())
    except ValueError:
        return func.HttpResponse(
             "Invalid body",
             status_code=400
        )
    
    # If there is nowhere to put the translated documents just skip.
    if body:
        values = json.loads(body)['values']
        
        # Prepare the Output before the loop
        results = {}
        results["values"] = []

        for value in values:

            output_record = DocumentTranslation.transform_value(value)

            # Based on the simulation, if the document is candidate, we trigger an orchestration...
            if output_record['data']['document_translatable']:
                instance_id = await client.start_new("DocumentTranslationOrchestrator", None, value)
                logging.info(f"Started orchestration with ID = '{instance_id}'.")
                output_record['data']['message']=instance_id

            if output_record != None:
                results["values"].append(output_record)

        return func.HttpResponse(json.dumps(results, ensure_ascii=False), mimetype="application/json")
    else:
        return func.HttpResponse(
             "Invalid body",
             status_code=400
        )
