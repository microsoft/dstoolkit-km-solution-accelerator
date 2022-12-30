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

# Document Translation

DOCUMENT_TRANSLATION_SUPPORTED_LANGUAGES = ["af", "sq", "am", "ar", "hy", "as", "az", "bn", "ba", "bs", "bg","yue", "ca",
"zh", "zh_chs", "zh_cht", "lzh", "zh-Hans", "zh-Hant", 
"hr", "cs", "da", "prs", "dv", "nl", "en","et", "fj", "fil", "fi",
"fr", "fr-ca", "ka", "de", "el", "gu", "ht", "he", "hi", "mww",
"hu", "is", "id", "iu", "ga", "it", "ja", "kn", "kk", "km", "tlh-Latn", "tlh-Piqd", "ko",
"ku", "kmr", "ky", "lo", "lv", "lt", "mk", "mg", "ms", "ml", "mt", "mi", "mr", "mn-Cyrl",
"mn-Mong", "my", "ne", "nb", "or", "ps", "fa", "pl", "pt", "pt-pt", "pa", "otq", "ro", "ru",
"sm", "sr-Cyrl", "sr-Latn", "sk", "sl", "es", "sw", "sv", "ty", "ta", "tt", "te", "th", "bo",
"ti", "to", "tr", "tk", "uk", "ur", "ug", "uz", "vi", "cy", "yua"]

DOCUMENT_TRANSLATION_SUPPORTED_EXTENSIONS = [".pdf", ".csv", ".html",".htm", ".xlf", ".markdown",".mdown",".mkdn",".md",".mkd",".mdwn",".mdtxt",".mdtext",".rmd",
".mthml",".mht",".xls",".xlsx",".msg",".ppt",".pptx",".doc",".docx",".odt",".odp",".ods",".rtf",".tsv",".txt"]


# https://learn.microsoft.com/en-us/azure/cognitive-services/language-service/personally-identifiable-information/language-support?tabs=documents
PII_DETECTION_SUPPORTED_LANGUAGES = ["de","en","es","fr","it","ja","ko","pt-BR","pt-PT","zh-Hans"]
