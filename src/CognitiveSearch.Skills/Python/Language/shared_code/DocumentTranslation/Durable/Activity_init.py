# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

import azure.functions as func
import azure.durable_functions as df
import json
from .. import DocumentTranslation

def main(record: str) -> str:
    
    # Call Document Translation and wait...
    output_record = DocumentTranslation.transform_value(record, poll=True)

    return output_record
