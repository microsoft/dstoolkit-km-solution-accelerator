![banner](docs/media/banner.png)

__Knowledge Mining solution accelerator__

This repository contains all the code for deploying and end-to-end Knowledge Mining solution based on Azure Cognitive Search.

It is built on top of standards Azure services like Functions, Web App Services, Congitive Services & Cognitive Search. 

It is built on the MLOps Accelerator and provides end to end training and deployment pipelines allowing quick and easy setup of CI/CD pipelines for your projects.

For detailed documentation please refer to the docs section of the repo containing the solution wiki.

# Before you start 

In order to successfully setup your solution you will need to have access to and or provisioned the following:

- Access to an Azure subscription (required)
- Access to an Azure DevOps subscription (optional)

An Owner or Contributor role is assumed on the Azure subscription or the targeted Resource Group. 

# Getting Started

Please refer to the [README](deployment/README.md) to deploy this solution accelerator. 

The directions provided in all guides assume you have a fundamental working knowledge of the Azure portal, Azure Functions, Azure Cognitive Search, Functions, Storage and Azure Cognitives Services. 

For additional training and support, please see:

* [Knowledge Mining Bootcamp](https://github.com/MicrosoftLearning/LearnAI-KnowledgeMiningBootcamp)
* [AI in Cognitive Search documentation](https://docs.microsoft.com/azure/search/cognitive-search-resources-documentation)

# Knowledge Mining overview

Knowledge mining (KM) is an emerging discipline in artificial intelligence (AI) that uses a combination of intelligent services to quickly learn from vast amounts of information. It allows organizations to deeply understand and easily explore information, uncover hidden insights, and find relationships and patterns at scale.

[Knowledge Mining in Azure](https://azure.microsoft.com/en-us/solutions/knowledge-mining)

# What is this solution accelerator ? 

This KM solution accelerator aims to provide you with a workable end-to-end Knowledge Mining solution composed of : 
- Ingestion
    - Data ingestion from Azure Data Lake
- Enrichment
    - Data enrichment with Azure Applied AI and Cognitive Services
- Exploration
    - Keyword and Semantic search
    - Support for multiples search indexes
    - Content security model (permissions)
    - Modular User Interface 

With this cloud-based accelerator you will get an end-to-end solution with the tools to deploy, extend, operate & monitor it.

In that respect, the solution provides 
- Azure Web App Authentication support 
- High configurability (json)
- Extensibility 
- Operations (PowerShell-based)
- Azure Pipelines for CI/CD 
- Deployment framework (manual or through CI/CD)

# Why a knowledge mining solution accelerator? 

This Knowledge Mining solution accelerator is inspired from another accelerator [Knowledge Mining Solution Accelerator](https://github.com/Azure-Samples/azure-search-knowledge-mining). 

Based on our fields experience, we built features/skills to address common unstructured data challenges focusing on the usability and data explore experience. 

Below is a non-exhaustive list of key highlights:

* **Embedded images indexation** 
    - Images embedded in documents are indexed as documents not just for keywords search recall.
    - PDF pages are extracted as images (configurable).
    - A custom version of Apache Tika is used for images extraction.
    - Overcome the limit of [1000 normalized images](https://docs.microsoft.com/en-us/azure/search/cognitive-search-concept-image-scenarios#get-normalized-images)

* **Image normalization** : 
    - handling oversized images for OCR completeness
    - support for TIFF format
    - thumbnails creation for UI support

* **Metadata**
    - Using Apache Tika we give you access to all metadata present in each document or image. A common scenario are Images with geo-location metadata i.e. EXIF GPS coordinates. 

* **HTML Conversion**
    - Having an HTML representation of a document could ease some NLP work. 
    - Table of contents is a common structure which we expose in the HTML representation of a PDF. 

* **Tables extraction**: tabular information are common in unstructured data. The solution will extract, index and project tables to a dedicated knowledge store.   

* **Export to Excel**: popular ask when exploring unstructured data. 

* **Configurable UI**: building a UI is time consuming, we wanted to bring great UI configurability so you could bring to life new KM solutions in a timely manner.

# What Knowledge Mining scenarios this accelerator targets ?

This solution accelerator spirit is of a [Content Research](https://docs.microsoft.com/en-us/azure/architecture/solution-ideas/articles/content-research) KM scenario. 

![](docs/architecture/knowledge-mining-content-research.png)

Nevertheless, since its architecture is open, you could use it as a foundation for more specialized KM scenarios.

**This solution accelerator is not targeted to any domain although its extensibility would give you the tools to make it domain specific.**

You may think of productization of such accelerator for your organization.  

# Who is the target audience ?

This solution accelerator targets whoever is in need of  

- Proof Of Concept to showcase Knowledge Mining to your stakeholders 
- Deploy an end-to-end KM solution for immediate Production use
- Learn how to build a KM solution on Azure
- Playground for evaluating Azure Machine Learning, Cognitive & Applied AI Services 

# Data Science Toolkit Integration

This solution accelerator purpose is also to ease the integration of Data Science modules into your knowledge mining solution. 

The Data Science Toolkit team has built accelerators for your data science workload. 

| Solution | Description |
|--------------|---|
|[Verseagility](https://github.com/microsoft/verseagility)|Verseagility is a Python-based toolkit to ramp up your custom natural language processing (NLP) task, allowing you to bring your own data, use your preferred frameworks and bring models into production. It is a central component of the Microsoft Data Science Toolkit.|
| [MLOps Base](https://github.com/microsoft/dstoolkit-mlops-base) | This repository contains the basic repository structure for machine learning projects based on Azure technologies (Azure ML and Azure DevOps). The folder names and files are chosen based on personal experience. You can find the principles and ideas behind the structure, which we recommend to follow when customizing your own project and MLOps process. Also, we expect users to be familiar with azure machine learning concepts and how to use the technology.| 
|[MLOps for DataBricks](https://github.com/microsoft/dstoolkit-ml-ops-for-databricks)| This repository contains the Databricks development framework for delivering any Data Engineering projects, and machine learning projects based on the Azure Technologies.| 
|[Classification Solution Accelerator](https://github.com/microsoft/dstoolkit-classification-solution-accelerator)| This repository contains the basic repository structure for delivering classification solutions for machine learning (ML) projects based on Azure technologies (Azure ML and Azure DevOps).|
|[Object Detection Solution Accelerator](https://github.com/microsoft/dstoolkit-objectdetection-tensorflow-azureml)|This repository contains all the code for training TensorFlow object detection models within Azure Machine Learning (AML) with setups for training on Azure compute, experiment monitoring and endpoint deployment as a webservice. It is built on the MLOps Accelerator and provides end to end training and deployment pipelines allowing quick and easy setup of CI/CD pipelines for your projects.|
|||

# Documentation

You may refer to the solution accelerator documentation as follows: 

| Topic  | Description | Documentation Link | 
|----|----|----|
| Pre-Requisites | What do you need to deploy & operate the solution | [README](docs/pre-reqs/README.md)| 
| Architecture | How the solution is architected|[README](docs/architecture/README.md)| 
| Deployment | How to deploy this solution accelerator |[README](docs/deployment/README.md)| 
| Configuration | All you need to know about the solution accelerator configuration |[README](docs/configuration/README.md)| 
| Data Science | Integration with Data Science |[README](docs/data-science/README.md)| 
| Deployment | Ho to get started by deploying the solution |[README](docs/deployment/README.md)| 
| Monitoring | How to monitor the solution |[README](docs/monitoring/README.md)| 
| Search | How search is configured and managed |[README](docs/search/README.md)| 
| Search & Explore (UI) | User Interface to Search & Explore |[README](docs/ui/README.md)| 
||||

# Repository Structure

The respository structure of this accelerator is as follows 

--------
- **azure-pipelines** - Azure devops pipelines to set up your CI/CD
- **[configuration](configuration/README.md)** - solution configuration 
- **data** - sample data to validate solution deployment.
    - **documents** : sample documents for your KM solution 
- **[deployment](deployment/README.md)** - Configuration & scripts for deployment & operations
    - **config** : contains the entire solution base configuration
    - **modules** : PowerShell modules
    - **scripts** : deployment scripts
    - **init_env.ps1** : Environment initialization script 
- **[docs](docs/README.md)** - contains solution documentation wiki in .md format. Designed to be imported as an Azure DevOps wiki.
- **overlay** - Source code
- **[src](src/README.md)** - Source code
    - **CognitiveSearch.Skills** Custom skills
    - **CognitiveSearch.UI** User Interface .NET Core MVC
    - **Data Science** - placeholder to add your data science modules. 
--------

# How to use this accelerator?

Clone or download this repository and then navigate to the Deployment folder, following the steps outlined in the [deployment](deployment/README.md) guide. 

When you complete all of the steps, you'll have a working end-to-end knowledge mining solution that combines data sources ingestion with data enrichment skills and a web app powered by Azure Cognitive Search.

# Credits

This solution is inspired from the original work of the 

- Contributors of [Knowledge Mining Solution Accelerator](https://github.com/Azure-Samples/azure-search-knowledge-mining/graphs/contributors)
- Contributors of [Azure Search Power Skills ](https://github.com/Azure-Samples/azure-search-power-skills/graphs/contributors)

Core contributors to this solution accelerator are 
- [Nicolas Uthurriague](https://github.com/puthurr)
- [Edoardo Quasso](https://github.com/EdoQuasso) for the Azure Cognitive Functions (Python)

# Special Thanks 

The data science toolkit sponsorship team

- [Karsten Strøbæk](https://github.com/strobaek)
- [Willie Ahlers](https://github.com/WillieAhlers1)
- [Kimberly O'Donoghue]()

For the great conversation on Knowledge Mining and Unstructured Data
- [Sreedhar Mallangi](https://github.com/smallangi)
- [Timm Walz](https://github.com/nonstoptimm)

# Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

# Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft 
trademarks or logos is subject to and must follow 
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.

