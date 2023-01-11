# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

import azure.functions as func
import azure.durable_functions as df

from .. import DocumentGrapher

def main(record) -> str:

    # Call Document Grapher and wait...
    output_record = DocumentGrapher.transform_value(record)

    return output_record
