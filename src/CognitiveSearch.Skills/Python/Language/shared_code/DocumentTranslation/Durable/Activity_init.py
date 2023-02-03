# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

import azure.functions as func
import azure.durable_functions as df

from .. import DocumentTranslation

def main(record) -> str:
    
    # Call Document Translation and wait...
    output_record = DocumentTranslation.transform_value(record)

    return output_record
