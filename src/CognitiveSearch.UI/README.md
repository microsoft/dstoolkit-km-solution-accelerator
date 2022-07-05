# CognitiveSearch.UI

This folder contains the UI front-end and supporting back-end API & Services.

## 

UI layer (Front-End) uses a .NET 6.0 Core MVC platform.
API & Services layers are written in .NET 6.0 Core

The layers allows you to create your own UI on top of the API & Services back-end. 

All layers are grouped under a single Visual Studio solution. 

# UI Layer

On top of MVC, we use the following technologies 

bootstrap 5 
Typeahead
JDatatables

# API Layer 

Each API Controller has a speficic ingress request model.  

Each request model extend the related service ingress request model.  

# Services 

Each service has 

Interface 
Configuration
Service class

Services Injection

