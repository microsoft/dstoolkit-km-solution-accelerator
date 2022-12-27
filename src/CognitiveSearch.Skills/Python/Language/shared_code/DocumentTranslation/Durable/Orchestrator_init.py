# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

import azure.functions as func
import azure.durable_functions as df

def orchestrator_function(context: df.DurableOrchestrationContext):
    result1 = yield context.call_activity('DocumentTranslationActivity', context.get_input())
    return result1

main = df.Orchestrator.create(orchestrator_function)
