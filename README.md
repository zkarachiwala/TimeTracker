# TimeTracker
TimeTracker

### Use secret to store DB Password
https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-7.0&tabs=linux

### Use a sub query
https://stackoverflow.com/questions/71354466/performing-a-subquery-in-entity-framework-core
Use the Entity.SubEntity.Any method

### Many to many relationships
https://learn.microsoft.com/en-us/ef/core/modeling/relationships/one-to-many

### Understanding cascading delete
To prevent deletion of 'AppUser' from deleting TimeEntries, make the AppUser navigation property on TimeEntry nullable
https://learn.microsoft.com/en-us/ef/core/saving/cascade-delete

## Azure AD Support
### Hook in Azure AD
https://adrianhall.github.io/asp.net/2022/09/01/blazor-wasm-aad-auth-part-1/
https://learn.microsoft.com/en-us/aspnet/core/blazor/security/webassembly/standalone-with-azure-active-directory?view=aspnetcore-7.0

### Use App Roles with Azure AD
https://code-maze.com/using-app-roles-with-azure-active-directory-and-blazor-webassembly-hosted-apps/

## Requires secrets.json file
```
{
  "DbPassword": "{database password goes here - MAC OS requirement}",
  "DbUser": "{database user goes here - MAC OS requirement}",
  "AzureAD" : {
    "Domain" : "{Your AD domain}",
    "TenantId" : "{Your AD tenant ID}",
    "ClientId" : "{Your client id for your server app}"
  },
  "ClientConfiguration" : {
        "ClientId" : "{You client id for your client app}"
  }

}
```