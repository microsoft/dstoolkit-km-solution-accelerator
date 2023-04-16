# Security 

## .NET Core Security concepts

[.NET Core Security](https://learn.microsoft.com/en-us/aspnet/core/security/?view=aspnetcore-6.0)

ASP.NET Core enables developers to configure and manage security. The following list provides links to security topics:

- Authentication
- Authorization
- Data protection
- HTTPS enforcement
- Safe storage of app secrets in development
- XSRF/CSRF prevention
- Cross Origin Resource Sharing (CORS)
- Cross-Site Scripting (XSS) attacks

## Authentication

[.NET Core Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/?view=aspnetcore-6.0)

## Authorization

[.NET Core Authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/introduction?view=aspnetcore-6.0)

[.NET Core Authorization policies](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-6.0)


## Content security model (permissions)

### On the Document processing side

To secure your content served by ACS, you need to implement your own content security model. Luckly our accelerator provides one model for you to use or extend. 

Two index fields drives our model

- **restricted** - boolean to indicate if a document is visible by anyone or not. Default is false if no permissions are set.
- **permissions** - list of AAD Security Groups to secure the access to restricted document. 

To assign permissions upon indexation we used a json mapping **security.json** located in the Metadata Assign function. 

```json
[
  {
    "source": [ "site1", "folder1" ],
    "target": [ "securitygroup1","securitygroup2" ]
  },
  {
    "source": [ "folder2" , "restricted"],
    "target": [ "securitygroup2" ]
  },
  {
    "source": [ "folder3", "restricted" ],
    "target": [ "securitygroup3" ]
  }
]
```

The source describes the metadata url path segments to match all (AND operator). The target describes the list of AAD SG GUIDs to assign.

### Example 1

You document metadata url is of the form https://myhost/**site1**/documents/**folder1**.

Since the url contains site1 and folder1, it will assign securitygroup1 & securitygroup2 to the permissions field and set Restricted to true.

### Example 2

You document metadata url is of the form https://myhost/**site1**/documents/folder2.

Since the url matches only site1 no permissions will be assigned.

and so on...

### On the Query side 

When a user issued a search query, the backend will add an extra search filter to add all security groups the user belongs to. 

It implies the Bearer token contains that information. See the **Authentication** documentation on the same.

In a nutshell a user will see
- All unrestricted content (restricted eq false)
- All restricted content of one of its security groups match a document corresponding permissions (restricted eq true and SG match)

This security model is given as-is, you might want to implement your own model based on dymamic rules. 
