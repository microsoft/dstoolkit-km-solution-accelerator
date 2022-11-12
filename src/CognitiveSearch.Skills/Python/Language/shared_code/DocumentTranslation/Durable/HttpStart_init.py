# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

import logging
import json
import azure.functions as func
import azure.durable_functions as df

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
            # inputRecord = { 
            #     # "headers" : req.headers,
            #     "record" : value
            # }
            instance_id = await client.start_new(req.route_params["functionName"], None, value)

            logging.info(f"Started orchestration with ID = '{instance_id}'.")

            output_record = (
            {
                "recordId": value['recordId'],
                "data": { "message": instance_id }
            })

            if output_record != None:
                results["values"].append(output_record)

        return func.HttpResponse(json.dumps(results, ensure_ascii=False), mimetype="application/json")
    else:
        return func.HttpResponse(
             "Invalid body",
             status_code=400
        )

    # return client.create_check_status_response(req, instance_id)
