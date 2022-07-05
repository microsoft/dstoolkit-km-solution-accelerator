# Azure Cognitive Search - Skills

This folder combines all custom skills the solution accelerator uses groups by programming language
- C#
- Java
- Python

To enrich your solution with more skills, please refer to the below github project 
https://github.com/Azure-Samples/azure-search-power-skills

To the exception of Java-Apache Tika, all Skills are based on Azure Functions platform providing a consistent programming, configuration and supported framework. 

Documentation on Azure Functions : 

## Deployment 

### Simple
By default the solution accelerator will use a Function App service to deploy the corresponding function in Azure. The exception is Java-Apache Tika which is deployed in Web App. Apache Tika is served by a Jetty engine.

All Application Settings - Environment Variables of the Function and Web Application services are located 
[..\configuration\config\appsettings](..\..\configuration\config\appsettings)

### Complex
In complex deployment, you may want to deploy all skills in Azure Kubernetes cluster. We provide the matching YAML deployment files in the configuration/config/yaml folder. 

The yaml contains the settings placeholder.  

## Programming Languages & Runtimes

Azure Functions runtime is set to 4 (latest). 

- C# skills target **.NET Core / C# 6**
- Java : Java 8 Oracle JRE 
- Python skills target Python 3.9

