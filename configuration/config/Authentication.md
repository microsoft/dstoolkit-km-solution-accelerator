# Azure Web App Service Authentication configuration 

Our solution accelerator User Interface authentication uses the built-in Azure authentication and authorization layer formely know as EasyAuth.

https://docs.microsoft.com/en-us/azure/app-service/overview-authentication-authorization

# Configure the Enterprise Registration

The necessary configuration steps are described [here](https://docs.microsoft.com/en-us/azure/app-service/configure-authentication-provider-aad)

## Authentication section

The platform would be Web with the below Redirect URIs 

- "https://{{config.name}}ui.azurewebsites.net"
- "https://{{config.name}}ui.azurewebsites.net/.auth/login/aad/callback",

__Check the ID Tokens__

## API Permissions

All permissions would be Delegated. Minimum permission required is  

- Microsoft Graph / User.Read (Sign In and read user profile)

You might want to add more permissions as you see fit. Microsoft Graph has extensive set of APIs for collaboration. 

## Expose an API section

Application ID URI would show (typically)

- api://{{config.clientId}}

__Scopes defined by this API__ should list the user_impersonation scope 

- api://{{config.clientId}}/user_impersonation

## Manifest section - Emit Security Groups Claims 

To secure content, our solution accelerator would look up for the Security Groups (SGs) the user is member of so that a user would only see the content he is allowed to see. 

By default, SGs membership is not emitted in the ID token by Azure AD. 

To emit Security Groups claims, follow the [official documentation](https://docs.microsoft.com/en-us/azure/active-directory/hybrid/how-to-connect-fed-group-claims#configure-the-azure-ad-application-registration-for-group-attributes)

In a nutshell,
```json
"groupMembershipClaims": "SecurityGroup"
```

# Update the environment configuration clientId 

As your Enterprise Application is created, amend your environment deployment configuration to adjust the client id parameter.  

# Your application details in your deployment config 

| Key | Value |
|---|---|
|domain        | {{config.domain}}|
|tenantId       | {{config.tenantId}}|
|clientId        | {{config.clientId}}|
|subscriptionId   | {{config.subscriptionId}} |

# Web App Setting related to Azure EasyAuth

In the configuration/config/webapps/webappui.json, you will find the below entries for the Azure AAD Authentication integration. 

```json
  {
    "name": "AzureAd:Instance",
    "value": "https://login.microsoftonline.com",
    "slotSetting": false
  },
  {
    "name": "AzureAd:Domain",
    "value": "{{config.domain}}",
    "slotSetting": false
  },
  {
    "name": "AzureAd:TenantId",
    "value": "{{config.tenantId}}",
    "slotSetting": false
  },
  {
    "name": "AzureAd:ClientId",
    "value": "{{config.clientId}}",
    "slotSetting": false
  }
```
The client secret app settings is not deployed as part of our solution accelerator.


# Publishing your webapp settings

```ps
./init_env.ps1 -Name <env>
Publish-WebAppsSettings -WindowsOnly
```
The -WindowsOnly tag will target the Windows-based Web Application which here is our WebApp UI. 

# Validate the EasyAuth configuration

Once the UI Web application settings are pushed thus the web application restarted, you will be challenged for consent. 

As you consented the application to read your profile upon the first connection, you can validate your security token by accessing 

- https://{{config.name}}ui.azurewebsites.net/.auth/me

To decode the security JWT token you may use [jwt.io](https://jwt.io). It will highlight among other things your security groups membership. 

# Non-Azure EasyAuth Authentication (non default)

In the UI webapp settings, change AzureEasyAuthIntegration to false.

Add the below settings 
```json
  {
    "name": "AzureAd:CallbackPath",
    "value": "/signin-oidc",
    "slotSetting": false
  },
  {
    "name": "AzureAd:ClientSecret",
    "value": "<YOUR AZURE APP SECRET HERE>",
    "slotSetting": false
  }
```

Restart the UI web app.

Your UI application will now authenticate the users by itself.
