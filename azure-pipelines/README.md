# Azure pipeline folder

As indicated in the name, this directory contains all the azure pipeline _yml_ files to create the CI/CD pipelines.

We provide two pipelines for convenience: 

|Name|Description|
|--|--|
|**infra-pipeline.yml**|Pipeline to deploy core azure services required by this accelerator|
|**solution-pipeline.yml**|Pipeline to build, publish & deploy the solution software|

## Variables

The pipeline files MUST NOT define any variables rather rely on a library group. For example. dataset name, model name, etc, must not be defined in the pipeline files. On the hand, if you need variable to define your environments (dev, test, prod, etc) or anything else not related to the DE/DS/ML part like specific credentials, you may define in the yml files.

In the provide example, we use a library group named contoso-group for non dev branch. 

```yml
variables:
  - name: tag
    value: '$(Build.BuildId)'
  - name: buildConfiguration
    value: Release
  - name: vmImageName
    value: 'windows-latest'
  - ${{ if eq(variables['build.SourceBranchName'], 'dev') }}:
    - group: contoso-group
```
