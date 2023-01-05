# Table of Functions/Skills 

- [Entity Linking](#entity-linking) 
- [Named Entity Recognition](#named-entity-recognition)
- [PII Detection](#pii-detection)
- [Key Phrase Extraction](#key-phrase-extraction)
- [Language Detection](#language-detection)
- [Summarization](#summarization)
- [Translator](#text-translation)
- [Document Translation](#document-translation)

In the Entity Linking, Named Entity Recognition, PII Detection, Keyphrase Extraction and Language Detection functions, a header tag called "type" is defined.

It is optional and it can assume 2 values "small" or "big", if it is not specified the default value is "big". It is needed to analyze two different type of texts: short and documents. 

For small text workload we know that we have a small amount of words to analyze, so the entire text is sent in the request to the Azure Service. In case of document instead we first split the text in chunks and we send each chunk in a different request. So we use "small" for analyze small text and "big" for full documents.

The Language function uses two Azure Cognitive Services : 

- [Language](https://learn.microsoft.com/en-us/azure/cognitive-services/language-service/overview)
- [Translation](https://learn.microsoft.com/en-us/azure/cognitive-services/translator/translator-overview)

# Entity Linking

This endpoint is used to call the Link entities using the Language Azure service. To start the function you need to do a POST request to the endpoint. 

The structure of the request is the following:

```https
content-type: application/json;charset=utf-8
minimumPrecision: (Optional) A value between 0 and 1. If the confidence score (in the entities output) is lower than this value, the entity is not returned. The default is 0.
defaultLanguageCode: (Optional) Language code of the input text. If the default language code is not specified, English (en) will be used as the default language code.
type: (Optional) "small" or "big"

{
    "values": [
        {
            "recordId": "1",
            "data": {
                "languageCode": A string indicating the language of the records. If this parameter is not specified, the default language code will be used to analyze the records,
                "text": The text to analyze.
            }
        },
        {
            "recordId": "2",
            "data": {
                "languageCode": A string indicating the language of the records. If this parameter is not specified, the default language code will be used to analyze the records.,
                "text": The text to analyze.
            }
        }
    ]
}
```

The output is as follows :
- ```entities```    An array of complex types that contains the following fields:
    - ```name```    (The actual entity name as it appears in the text)
    - ```id```
    - ```language```    (The language of the text as determined by the skill)
    - ```url``` (The linked url to this entity)
    - ```bingId```  (The bingId for this linked entity)
    - ```dataSource``` (The data source associated with the url)
    - ```matches``` (An array of complex types that contains: text, offset, length and confidenceScore)

[Azure Cognitive Language - Entity Linking](https://learn.microsoft.com/en-us/azure/cognitive-services/language-service/entity-linking/overview)

# Named Entity Recognition

This **entityrecognition** endpoint is used to call the Entity Recognition using the Language Azure service. To start the function you need to do a POST request to the endpoint.
The Entity Recognition skill extracts entities of different types from text. These entities fall under 14 distinct categories, ranging from people and organizations to URLs and phone numbers.

The structure of the request is the following:

```https
content-type: application/json;charset=utf-8
categories: (Optional) Array of categories that should be extracted. Possible category types: "Person", "Location", "Organization", "Quantity", "DateTime", "URL", "Email", "personType", "Event", "Product", "Skill", "Address", "phoneNumber", "ipAddress". If no category is provided, all types are returned. List of [supported categories](https://learn.microsoft.com/en-us/azure/cognitive-services/language-service/named-entity-recognition/concepts/named-entity-categories)

minimumPrecision: (Optional) A value between 0 and 1. If the confidence score (in the namedEntities output) is lower than this value, the entity is not returned. The default is 0.
defaultLanguageCode: (Optional) Language code of the input text. If the default language code is not specified, English (en) will be used as the default language code.
type: (Optional) "small" or "big"

{
    "values": [
        {
            "recordId": "1",
            "data": {
                "languageCode": A string indicating the language of the records. If this parameter is not specified, the default language code will be used to analyze the records,
                "text": The text to analyze
            }
        },
        {
            "recordId": "2",
            "data": {
                "languageCode": A string indicating the language of the records. If this parameter is not specified, the default language code will be used to analyze the records,
                "text": The text to analyze
            }
        }
    ]
}
```

The output is as follows :
* ```Person```	An array of strings where each string represents the name of a person.
* ```Location```	An array of strings where each string represents a location.
* ```Organization```	An array of strings where each string represents an organization.
* ```Quantity```	An array of strings where each string represents a quantity.
* ```DateTime```	An array of strings where each string represents a DateTime (as it appears in the text) value.
* ```URL```	An array of strings where each string represents a URL
* ```Email```	An array of strings where each string represents an email
* ```PersonType```	An array of strings where each string represents a PersonType
* ```Event```	An array of strings where each string represents an event
* ```Product```	An array of strings where each string represents a product
* ```Skill```	An array of strings where each string represents a skill
* ```Address```	An array of strings where each string represents an address
* ```PhoneNumber```	An array of strings where each string represents a telephone number
* ```IPAddress```	An array of strings where each string represents an IP Address
* ```namedEntities```	An array of complex types that contains the following fields:
    * ```category```
    * ```subcategory```
    * ```confidenceScore``` (Higher value means it's more to be a real entity)
    * ```length``` (The length(number of characters) of this entity)
    * ```offset``` (The location where it was found in the text)
    * ```text``` (The actual entity name as it appears in the text)

Note that the output reflect the categories defined for the Named Entity Recognition. It may evolve in the future with new categories. The updated list of categories and corresponding sub-categories can be found [here](https://learn.microsoft.com/en-us/azure/cognitive-services/language-service/named-entity-recognition/concepts/named-entity-categories)

[Azure Cognitive Language - Named Entity Recognition (NER)](https://learn.microsoft.com/en-us/azure/cognitive-services/language-service/named-entity-recognition/overview)

# PII Detection

This endpoint **PIIDetection** is used to call the PII Detection using the Language Azure service. To start the function you need to do a POST request to the endpoint.

The PII Detection skill extracts entities of different types from text. These entities fall under categories, ranging from people and organizations to URLs and phone numbers.

The structure of the request is the following:

```https
content-type: application/json;charset=utf-8
categories: (Optional) Array of categories that should be extracted. If no category is provided, all types are returned.
minimumPrecision: (Optional) A value between 0 and 1. If the confidence score (in the namedEntities output) is lower than this value, the entity is not returned. The default is 0.
defaultLanguageCode: (Optional) Language code of the input text. If the default language code is not specified, English (en) will be used as the default language code.
type: (Optional) "small" or "big"

{
    "values": [
        {
            "recordId": "1",
            "data": {
                "languageCode": A string indicating the language of the records. If this parameter is not specified, the default language code will be used to analyze the records,
                "text": The text to analyze
            }
        },
        {
            "recordId": "2",
            "data": {
                "languageCode": A string indicating the language of the records. If this parameter is not specified, the default language code will be used to analyze the records,
                "text": The text to analyze
            }
        }
    ]
}
```

The output is as follows :

* ```namedEntities```	An array of complex types that contains the following fields:
    *  ```category```
    * ```subcategory```
    * ```confidenceScore``` (Higher value means it's more to be a real entity)
    * ```length``` (The length(number of characters) of this entity)
    * ```offset``` (The location where it was found in the text)
    * ```text``` (The actual entity name as it appears in the text)

The updated list of categories and corresponding sub-categories can be found [here](https://learn.microsoft.com/en-us/azure/cognitive-services/language-service/personally-identifiable-information/concepts/entity-categories)

[Azure Cognitive Language - PII Detection](https://learn.microsoft.com/en-us/azure/cognitive-services/language-service/personally-identifiable-information/overview)

# Key Phrase Extraction

This endpoint is used to call the Key Phrase Extraction using the Language Azure service. To start the function you need to do a POST request to the endpoint.
The Key Phrase Extraction skill evaluates unstructured text, and for each record, returns a list of key phrases.

The structure of the request is the following:

```https
content-type: application/json;charset=utf-8
maxKeyPhraseCount: (Optional) The maximum number of key phrases to produce.
defaultLanguageCode: (Optional) The language code to apply to documents that don't specify language explicitly. If the default language code is not specified, English (en) will be used as the default language code.
type: (Optional) "small" or "big"

{
    "values": [
        {
            "recordId": "1",
            "data": {
                "languageCode": A string indicating the language of the records. If this parameter is not specified, the default language code will be used to analyze the records,
                "text": The text to be analyzed
            }
        },
        {
            "recordId": "2",
            "data": {
                "languageCode": A string indicating the language of the records. If this parameter is not specified, the default language code will be used to analyze the records,
                "text": The text to be analyzed
            }
        }
    ]
}
```

The output:
* ```keyPhrases```	A list of key phrases extracted from the input text. The key phrases are returned in order of importance.

[Azure Cognitive Language - Key Phrases extraction](https://learn.microsoft.com/en-us/azure/cognitive-services/language-service/key-phrase-extraction/overview)


# Language Detection

This endpoint is used to call the Language Detection using the Language Azure service. To start the function you need to do a POST request to the endpoint. The Language Detection skill detects the language of input text and reports a single language code for every document submitted on the request. The language code is paired with a score indicating the strength of the analysis.

The structure of the request is the following:

```https
content-type: application/json;charset=utf-8
defaultCountryHint: (Optional) An ISO 3166-1 alpha-2 two letter country code can be provided to use as a hint to the language detection model if it cannot disambiguate the language. Specifically, the defaultCountryHint parameter is used with documents that don't specify the countryHint input explicitly.
type: (Optional) "small" or "big"

{
    "values": [
        {
            "recordId": "1",
            "data": {
                "text": The text to be analyzed.
            }
        }
    ]
}
```

The output:
* ```languageCode```	The ISO 6391 language code for the language identified. For example, "en".
* ```languageName```	The name of language. For example "English".
* ```score```	A value between 0 and 1. The likelihood that language is correctly identified. The score may be lower than 1 if the sentence has mixed languages.

[Azure Cognitive Language - Language Detection](https://learn.microsoft.com/en-us/azure/cognitive-services/language-service/language-detection/overview)

# Summarization

This **summarization** endpoint is used to call the Summarization using the Language Azure service. To start the function you need to do a POST request to the endpoint. It is a feature that produces a summary by extracting sentences that collectively represent the most important or relevant information within the original content.

The structure of the request is the following:

```https
content-type: application/json;charset=utf-8

{
    "values": [
        {
            "recordId": "1",
            "data": {
                "text": The text to summarize,
                "language": A string indicating the language of the records
            }
        },
        {
            "recordId": "2",
            "data": {
                "text": The text to summarize,
                "language": A string indicating the language of the records
        }
    ]
}
```

The output:
* ```text```    Text extracted from the original document
* ```rankScore```   An indicator of how relevant a sentence is determined to be, to the main idea of a document
* ```offset```  Starting point in which the sentences appear in the input document
* ```length```  Number of character in text extracted

[Azure Cognitive Language - Summarization](https://learn.microsoft.com/en-us/azure/cognitive-services/language-service/summarization/overview?tabs=document-summarization)

# Text Translation

This endpoint **translator** translates textual content using the Translator Azure service. To start the function you need to do a POST request to the endpoint. The Text Translation skill evaluates text and, for each record, returns the text translated to the specified target language.

The structure of the request is the following:

```https
content-type: application/json;charset=utf-8
defaultToLanguageCode:  (Required) The language code to translate documents into for documents that don't specify the to language explicitly.
defaultFromLanguageCode:    (Optional) The language code to translate documents from for documents that don't specify the from language explicitly. If the defaultFromLanguageCode is not specified, the automatic language detection provided by the Translator Text API will be used to determine the from language.
suggestedFrom:  (Optional) The language code to translate documents from when neither the fromLanguageCode input nor the defaultFromLanguageCode parameter are provided, and the automatic language detection is unsuccessful. If the suggestedFrom language is not specified, English (en) will be used as the suggestedFrom language.

{
    "values": [
        {
            "recordId": "1",
            "data": {
                "text": The text to be translated,
                "toLanguageCode": A string indicating the language the text should be translated to. If this input is not specified, the defaultToLanguageCode will be used to translate the text,
                "fromLanguageCode": A string indicating the current language of the text. If this parameter is not specified, the defaultFromLanguageCode (or automatic language detection if the defaultFromLanguageCode is not provided) will be used to translate the text
            }
        }
    ]
}
```

The output:
- ```translatedText```	The string result of the text translation from the translatedFromLanguageCode to the translatedToLanguageCode.
- ```translatedToLanguageCode```	A string indicating the language code the text was translated to. Useful if you are translating to multiple languages and want to be able to keep track of which text is which language.
- ```translatedFromLanguageCode```	A string indicating the language code the text was translated from. Useful if you opted for the automatic language detection option as this output will give you the result of that detection.

[Azure Cognitive Services Translator documentation](https://learn.microsoft.com/en-us/azure/cognitive-services/translator/translator-overview) 

# Document Translation

We do provide 2 endpoints for Document Translation
- Durable function (default) endpoint named **DocumentTranslationHttpStart**
- Non-Durable function endpoint named **DocumentTranslation**
- Non-Durable function endpoint named **DocumentTranslationOps** for Translation Operations status.

Both endpoints accept the same request structure and poll for translation results. 

Document Translation endpoints translate an entire document using the Translator Azure service. To start the function you need to do a POST request to the endpoint. The Text Translation skill evaluates text and, for each record, returns the text translated to the specified target language.

The structure of the request is the following:

```https
content-type: application/json;charset=utf-8
defaultToLanguageCode:  (Required) The language code to translate documents into for documents that don't specify the to language explicitly.
defaultFromLanguageCode:    (Optional) The language code to translate documents from for documents that don't specify the from language explicitly. If the defaultFromLanguageCode is not specified, the automatic language detection provided by the Translator Text API will be used to determine the from language.
suggestedFrom:  (Optional) The language code to translate documents from when neither the fromLanguageCode input nor the defaultFromLanguageCode parameter are provided, and the automatic language detection is unsuccessful. If the suggestedFrom language is not specified, English (en) will be used as the suggestedFrom language.

{
    "values": [
        {
            "recordId": "1",
            "data": {
                "document_index_key": The document index key,
                "document_id": The document id,
                "document_filename": The document filename,
                "document_url": The document url,
                "fromLanguageCode": A string indicating the current language of the text. If this parameter is not specified, the defaultFromLanguageCode (or automatic language detection if the defaultFromLanguageCode is not provided) will be used to translate the text,
                "fileExtension": The file extension of the document. Required to define if the document is supported for translation.
                "contentType": Ensure the content type is set adequately on the generated/resulting blob in the translation container.
            }
        }
    ]
}
```

The output:
- ```translation_opid```	The string representing the Translation operation identifier. This id can be used to validate a translation operation or check on its status.
- ```translatedToLanguageCode```	A string indicating the language code the text was translated to. Useful if you are translating to multiple languages and want to be able to keep track of which text is which language.
- ```translatedFromLanguageCode```	A string indicating the language code the text was translated from. Useful if you opted for the automatic language detection option as this output will give you the result of that detection.
- ```document_translated```	A boolean indicating if the document was sent to Translation or not.
- ```document_translatable```	A boolean indicating if the document was a potential candidate for translation but something went wrong.

[Azure Cognitive Services Translator documentation](https://learn.microsoft.com/en-us/azure/cognitive-services/translator/translator-overview) 
