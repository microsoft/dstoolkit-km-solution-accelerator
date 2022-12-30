# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.


ENTITY_CATEGORIES = ["Person", "Location", "Organization", "Quantity", "DateTime", "URL", "Email", "PersonType", "Event", "Product", "Skill", "Address", "Phone Number", "IP Address"]

    # entity_recognition_mapping = {
    #         "Person": {"name": "persons", "matched":[]}, 
    #         "Location": {"name": "locations", "matched":[]}, 
    #         "Organization": {"name": "organizations", "matched":[]},
    #         "Quantity": {"name": "quantities", "matched":[]}, 
    #         "DateTime": {"name": "dateTimes", "matched":[]}, 
    #         "URL": {"name": "urls", "matched":[]}, 
    #         "Email": {"name": "emails", "matched":[]}, 
    #         "PersonType": {"name": "personTypes", "matched":[]}, 
    #         "Event": {"name": "events", "matched":[]}, 
    #         "Product": {"name": "products", "matched":[]}, 
    #         "Skill": {"name": "skills", "matched":[]}, 
    #         "Address": {"name": "addresses", "matched":[]}, 
    #         "Phone Number": {"name": "phoneNumbers", "matched":[]}, 
    #         "IP Address": {"name": "ipAddresses", "matched":[]}
    # }

# https://learn.microsoft.com/en-us/azure/cognitive-services/language-service/personally-identifiable-information/language-support?tabs=documents
PII_DETECTION_SUPPORTED_LANGUAGE = ["de","en","es","fr","it","ja","ko","pt-BR","pt-PT","zh-Hans"]
