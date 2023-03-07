# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

import logging
import azure.functions as func
import json

from . import DocumentGrapher

#
# Document Grapher function entrypoint
#
def main(req: func.HttpRequest, context: func.Context) -> func.HttpResponse:
    logging.info(f'{context.function_name} HTTP trigger function processed a request.')
    if hasattr(context, 'retry_context'):
        logging.info(f'Current retry count: {context.retry_context.retry_count}')
        
        if context.retry_context.retry_count == context.retry_context.max_retry_count:
            logging.info(
                f"Max retries of {context.retry_context.max_retry_count} for "
                f"function {context.function_name} has been reached")

    try:
        body = json.dumps(req.get_json())
    except ValueError:
        return func.HttpResponse(
             "Invalid body",
             status_code=400
        )
    
    # If there is nowhere to put the translated documents just skip.
    if body:
        result = compose_response(req.headers, body)
        return func.HttpResponse(result, mimetype="application/json")
    else:
        return func.HttpResponse(
             "Invalid body",
             status_code=400
        )

def compose_response(headers, json_data):
    request = json.loads(json_data)
    # Prepare the response
    results = {}
    results["values"] = []    
    if 'values' in request: 
        values = request['values']

        for value in values:
            output_record = DocumentGrapher.transform_value(value)
            if output_record != None:
                results["values"].append(output_record)

    return json.dumps(results, ensure_ascii=False)
