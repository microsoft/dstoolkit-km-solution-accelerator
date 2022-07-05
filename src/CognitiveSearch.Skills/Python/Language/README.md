# Table of Functions/Skills 

- [Entity Linking](#entity-linking) 
- [Entity Recognition](#entity-recognition)
- [Key Phrase Extraction](#key-phrase-extraction)
- [Language Detection](#language-detection)
- [Summarization](#summarization)
- [Translator](#translator)

In the Entity Linking, Entity Recognition, Keyphrase Extraction and Language Detection functions a header tag called "type" is defined. 

It is optional and it can assume 2 values "small" or "big", if it is not specified the default value is "big". It is needed to analyze two different type of texts: short and documents. 

For RSS text we know that we have a small amount of words to analyze, so the entire text is sent in the request to the Azure Service. In case of document instead we first split the text in chunks and we send each chunk in a different request. So we use "small" for analyze RSS and "big" for documents.

# Entity Linking

This endpoint is used to call the Link entities using the Text Analytics Azure service. To start the function you need to do a POST request to the endpoint. 

The structure of the request is the following:

```https
Content-Type: application/json
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

The output:
- ```entities```    An array of complex types that contains the following fields:
- ```name```    (The actual entity name as it appears in the text)
- ```id```
- ```language```    (The language of the text as determined by the skill)
- ```url``` (The linked url to this entity)
- ```bingId```  (The bingId for this linked entity)
- ```dataSource``` (The data source associated with the url)
- ```matches``` (An array of complex types that contains: text, offset, length and confidenceScore)

# Entity Recognition

This endpoint is used to call the Entity Recognition using the Text Analytics Azure service. To start the function you need to do a POST request to the endpoint.
The Entity Recognition skill extracts entities of different types from text. These entities fall under 14 distinct categories, ranging from people and organizations to URLs and phone numbers.

The structure of the request is the following:

```https
Content-Type: application/json
categories: (Optional) Array of categories that should be extracted. Possible category types: "Person", "Location", "Organization", "Quantity", "DateTime", "URL", "Email", "personType", "Event", "Product", "Skill", "Address", "phoneNumber", "ipAddress". If no category is provided, all types are returned.
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

The output:
* ```persons```	An array of strings where each string represents the name of a person.
* ```locations```	An array of strings where each string represents a location.
* ```organizations```	An array of strings where each string represents an organization.
* ```quantities```	An array of strings where each string represents a quantity.
* ```dateTimes```	An array of strings where each string represents a DateTime (as it appears in the text) value.
* ```urls```	An array of strings where each string represents a URL
* ```emails```	An array of strings where each string represents an email
* ```personTypes```	An array of strings where each string represents a PersonType
* ```events```	An array of strings where each string represents an event
* ```products```	An array of strings where each string represents a product
* ```skills```	An array of strings where each string represents a skill
* ```addresses```	An array of strings where each string represents an address
* ```phoneNumbers```	An array of strings where each string represents a telephone number
* ```ipAddresses```	An array of strings where each string represents an IP Address
* ```namedEntities```	An array of complex types that contains the following fields:
* ```category```
* ```subcategory```
* ```confidenceScore``` (Higher value means it's more to be a real entity)
* ```length``` (The length(number of characters) of this entity)
* ```offset``` (The location where it was found in the text)
* ```text``` (The actual entity name as it appears in the text)

# Key Phrase Extraction

This endpoint is used to call the Key Phrase Extraction using the Text Analytics Azure service. To start the function you need to do a POST request to the endpoint.
The Key Phrase Extraction skill evaluates unstructured text, and for each record, returns a list of key phrases.

The structure of the request is the following:

```https
Content-Type: application/json
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

# Language Detection

This endpoint is used to call the Language Detection using the Text Analytics Azure service. To start the function you need to do a POST request to the endpoint. The Language Detection skill detects the language of input text and reports a single language code for every document submitted on the request. The language code is paired with a score indicating the strength of the analysis.

The structure of the request is the following:

```https
Content-Type: application/json
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

# Summarization

This endpoint is used to call the Summarization using the Text Analytics Azure service. To start the function you need to do a POST request to the endpoint. It is a feature that produces a summary by extracting sentences that collectively represent the most important or relevant information within the original content.

The structure of the request is the following:

```https
Content-Type: application/json

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

# Translator

This endpoint is used to call the Translator using the Text Analytics Azure service. To start the function you need to do a POST request to the endpoint. The Text Translation skill evaluates text and, for each record, returns the text translated to the specified target language.

The structure of the request is the following:

```https
Content-Type: application/json
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

