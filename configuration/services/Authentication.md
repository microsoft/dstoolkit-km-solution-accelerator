# Azure Web App Service Authentication configuration 

Our solution accelerator User Interface authentication uses the built-in Azure authentication and authorization layer formely know as EasyAuth.

https://docs.microsoft.com/en-us/azure/app-service/overview-authentication-authorization

# Scenario : UI & API in the same App Service

## Configure the Enterprise Registration

The necessary configuration steps are described [here](https://docs.microsoft.com/en-us/azure/app-service/configure-authentication-provider-aad)

### Authentication section

The platform would be Web with the below Redirect URIs 

- "https://{{config.name}}ui.azurewebsites.net"
- "https://{{config.name}}ui.azurewebsites.net/.auth/login/aad/callback",

__Under Implicit grant and hybrid flows, select ID tokens.__

### API Permissions

All permissions would be Delegated. Minimum permission required is  

- Microsoft Graph / User.Read (Sign In and read user profile)

You might want to add more permissions as you see fit. Microsoft Graph has extensive set of APIs for collaboration. 

### Expose an API section

Application ID URI would show (typically)

- api://{{config.clientId}}

__Scopes defined by this API__ should list the user_impersonation scope 

- api://{{config.clientId}}/user_impersonation

### Manifest section - Emit Security Groups Claims 

To secure content, our solution accelerator would look up for the Security Groups (SGs) the user is member of so that a user would only see the content he is allowed to see. 

By default, SGs membership is not emitted in the ID token by Azure AD. 

To emit Security Groups claims, follow the [official documentation](https://docs.microsoft.com/en-us/azure/active-directory/hybrid/how-to-connect-fed-group-claims#configure-the-azure-ad-application-registration-for-group-attributes)

In a nutshell,
```json
"groupMembershipClaims": "SecurityGroup"
```

Refer to Content Security model documentation in the Security page for more explanation on why Security Groups are needed. 

## Update the environment configuration clientId 

As your Enterprise Application is created, amend your environment deployment configuration to adjust the client id parameter.  

## Your application details in your deployment config 

| Key | Value |
|---|---|
|domain        | {{config.domain}}|
|tenantId       | {{config.tenantId}}|
|clientId        | {{config.clientId}}|
|subscriptionId   | {{config.subscriptionId}} |

## Web App Setting related to Azure EasyAuth

In the configuration/services/webapps/webui.json, you will find the below entries for the Azure AAD Authentication integration. 

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


## Publishing your webapp settings

```ps
./init_env.ps1 -Name <env>
Publish-WebAppsSettings -WindowsOnly
```
The -WindowsOnly tag will target the Windows-based Web Application which here is our WebApp UI. 

## Validate the EasyAuth configuration

Once the UI Web application settings are pushed thus the web application restarted, you will be challenged for consent. 

As you consented the application to read your profile upon the first connection, you can validate your security token by accessing 

- https://{{config.name}}ui.azurewebsites.net/.auth/me

To decode the security JWT token you may use [jwt.io](https://jwt.io). It will highlight among other things your security groups membership. 

## Non-Azure EasyAuth Authentication (non default)

[Set the Web-App to sign users in](https://learn.microsoft.com/en-us/azure/active-directory/develop/scenario-web-app-sign-user-overview?tabs=aspnetcore)

In the UI webapp settings, change Authentication:AzureEasyAuthIntegration to false.

Add the below settings 
```json
  {
    "name": "AzureAd:CallbackPath",
    "value": "/signin-oidc",
    "slotSetting": false
  },
  {
    "name": "AzureAd:SignedOutCallbackPath",
    "value": "/signout-oidc",
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

## Anonymous Access

In the UI webapp settings, change Authentication:AzureEasyAuthIntegration to false and Authentication:AllowAnonymous to true. 

# Scenario 2 : UI & Web API in different Web App Services

## Backend Web API 

Register a new Application to protect the Web API backend.

The registration steps are described (here)[https://learn.microsoft.com/en-us/azure/active-directory/develop/web-api-quickstart?pivots=devlang-aspnet-core#step-1-register-the-application]

* For Name, enter a name for the application. For example, enter {{config.name}}-WebAPI. Users of the app will see this name, and can be changed later.
* Select Register.
* Under Manage, select Expose an API > Add a scope. For Application ID URI, accept the default by selecting Save and continue, and then enter the following details:
  *  Scope name: **access_as_user**
  *  Who can consent?: Admins and users
  *  Admin consent display name: Access {{config.name}} Web API
  *  Admin consent description: Allows the app to access {{config.name}} Web API as the signed-in user.
  *  User consent display name: Access {{config.name}} Web API
  *  User consent description: Allow the application to access {{config.name}} Web API on your behalf.
  *  State: Enabled
* Select Add scope to complete the scope addition

In addition to the above configuration, our solution requires to have access to the Security Groups membership of users. SGs are used to protect the indexed data enforcing a security model for searching content. 

- Under Manage, select Token Configuration
- Add Groups Claim
- For the group types, select Security Groups.
- In the Customize token properties by type section, for each token type, validate the Group ID radio button is on.
- Save

The Web API application will now emit a claim **groups** listing all SG memberships of a user. Use the **jwt.ms** website to valide your access token.

Example
```json
  "groups": [
    "fac683ea-8adf-4d98-ac54-86b2437fc4c8",
    "f2c45514-020a-420d-9e7c-dc1e1a22935f",
    "0c8dd8f8-1634-48b7-8515-83707ac5e3ab"
  ]
```

**Documentation resources**

- https://learn.microsoft.com/en-us/azure/active-directory/develop/scenario-protected-web-api-app-configuration?tabs=aspnetcore
- https://learn.microsoft.com/en-us/azure/active-directory/develop/web-api-quickstart?pivots=devlang-aspnet-core

Your backend API settings would require the client id and tenant id to validate the incoming Bearer token. 
```json
"ClientId": "Enter_the_Application_Id_here",
"TenantId": "Enter_the_Tenant_Info_Here"
```

For our solution, note the {{config.name}}-WebAPI client id and set it in your deployment config **webAPIClientId** setting.

```json
    "webAPIClientId": "00000000-0000-0000-0000-000000000000",
```

The Expose an API / Authorized client applications section will be assign once the UI application is provisioned (see next). 

## Front-End UI (consuming our backend API)

If you have a SPA Application you would require a dedicated enterprise application to sign-in users.

https://learn.microsoft.com/en-us/azure/active-directory/develop/scenario-spa-overview

The original token upon authentication is an **ID token**. To invoke downstream APIs you would need an **access token**. 

### Acquire an Access token for our downstream backend "{{config.name}} Web API"

https://learn.microsoft.com/en-us/azure/active-directory/develop/scenario-protected-web-api-app-configuration?tabs=aspnetcore#bearer-token

**C#**
```C#
  var scopes = new[] {$"api://.../access_as_user"};
  var result = await app.AcquireToken(scopes)
                        .ExecuteAsync();

  httpClient = new HttpClient();
  httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);

  // Call the web API.
  HttpResponseMessage response = await _httpClient.GetAsync(apiUri);
```

**JavaScript**
```JavaScript
  const customApiToken = await msalInstance.acquireTokenSilent({
      scopes: [ "api://.../access_as_user" ]
  });
```
Once you have acquired an access token, you may invoke the downstream api with Authorization Bearer header. 


## Further protect Web API by adding a trusted client application

Using the UI client id, add it as Authorized Client Application of the Web API application registration. 

https://learn.microsoft.com/en-us/azure/active-directory/hybrid/connect/how-to-connect-fed-group-claims


