# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

import logging
import socket
import os
import azure.functions as func

def main(req: func.HttpRequest) -> func.HttpResponse:
    logging.info('Python HTTP trigger function processed a request.')

    # for k, v in sorted(os.environ.items()):
    #     logging.info(f' {k} : {v}')

    return func.HttpResponse(f"Alive and kicking !")