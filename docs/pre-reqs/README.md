![banner](../media/banner.png)

# Pre-Requisites

## Local or Remote 

To deploy & publish the solution accelerator from a local or remote machine with direct access to the target Azure subscription, you would need the following technologies : 

- [.NET Core 6.0](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
- [PowerShell Core 7](https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell?view=powershell-7.1)
- [AZ CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
- [Azure Functions Core tools v4.x](https://github.com/Azure/azure-functions-core-tools)

## Azure Cloud Shell installation

Alternatively, you can use Azure Cloud Shell for deploying and/or operating your solution. Use a dedicated file share to upload/store the solution accelerator code.

Az CLI and PowerShell 7 are already installed in Azure Cloud Shell by default. Azure Functions and Dotnet Core are also installed although at the time of writing with older versions.

https://docs.microsoft.com/en-us/azure/cloud-shell/features

https://docs.microsoft.com/en-us/azure/cloud-shell/features#tools

Check on the function core runtime 

```ps
func --version
```
Check on the dotnet core runtime 

```ps
dotnet --version
```

**If the function and dotnet versions are showing 4.x and 6.x respectively, you're good to go. If not, please follow the below steps.**

### Install Azure Functions 4 on Azure Cloud Shell (local user)

Local user only (-g removed)
npm i azure-functions-core-tools@4 --unsafe-perm true

### Install .NET Core 6 (local user)

- Donwload the Linux install script 
https://docs.microsoft.com/en-us/dotnet/core/install/linux-scripted-manual#scripted-install

- Upload it to your Azure Cloud Shell home

- Make the script executable 
```ps
chmod +755 dotnet-install.sh
```
- Run the .NET Core 6.0 
```ps
./dotnet-install.sh -c 6.0
```

### PowerShell profile for your local user

Create a PowerShell profile
https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_profiles?view=powershell-7.2

Add the below instruction in your profile file
$Env:PATH = '~/node_modules/azure-functions-core-tools/bin:~/.dotnet:'+$Env:PATH

Restart your Cloud Shell session

### Validate your installation

```ps
dotnet --version
func --version
```
