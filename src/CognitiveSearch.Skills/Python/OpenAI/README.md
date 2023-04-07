# Open AI - Table of Functions/Skills 

This function application leverage the Azure Open AI service to highlight some integration scenarios. 

About Azure Open AI...

Azure OpenAI Service provides REST API access to OpenAI's powerful language models including the GPT-3, Codex and Embeddings model series. These models can be easily adapted to your specific task including but not limited to content generation, summarization, semantic search, and natural language to code translation. Users can access the service through REST APIs, Python SDK, or our web-based interface in the Azure OpenAI Studio.

[Azure Open AI](https://learn.microsoft.com/en-us/azure/cognitive-services/openai/overview)

[Azure Open AI Studio](https://oai.azure.com/portal/)

# Completion

This endpoint is used to call the Completion feature using the Azure Open AI service. To start the function you need to do a POST request to the endpoint. 

The structure of the request is the following:

```json
content-type: application/json;charset=utf-8

{
    "values": [
        {
            "recordId": "1",
            "data": {
            }
        },
        {
            "recordId": "2",
            "data": {
                "prompt": "A neutron star is the collapsed core of a massive supergiant star, which had a total mass of between 10 and 25 solar masses, possibly more if the star was especially metal-rich. Neutron stars are the smallest and densest stellar objects, excluding black holes and hypothetical white holes, quark stars, and strange stars. Neutron stars have a radius on the order of 10 kilometres (6.2 mi) and a mass of about 1.4 solar masses. They result from the supernova explosion of a massive star, combined with gravitational collapse, that compresses the core past white dwarf star density to that of atomic nuclei.\n\nAnswer the following question from the text above.\n\nQ: How are neutron stars created?\nA:",
                "temperature":0.7,
                "max_tokens":256,
                "stop":["\n"]
            }
        }
    ]
}
```

We convey 4 parameters to Open AI :
- prompt
- temperature
- max_tokens
- stop

The output is as follows :

```json
{
  "values": [
    {
      "recordId": "1",
      "data": {
        "response": {
          "id": "cmpl-6uJZ7ncxUskHvZKgARWDl0cGK4U1B",
          "object": "text_completion",
          "created": 1678879597,
          "model": "text-davinci-003",
          "choices": [
            {
              "text": " Neutron stars are created by the supernova explosion of a massive star, combined with gravitational collapse, that compresses the core past white dwarf star density to that of atomic nuclei.",
              "index": 0,
              "finish_reason": "stop",
              "logprobs": null
            }
          ],
          "usage": {
            "completion_tokens": 38,
            "prompt_tokens": 156,
            "total_tokens": 194
          }
        }
      }
    }
  ]
}
```
