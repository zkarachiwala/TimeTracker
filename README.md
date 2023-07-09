# TimeTracker
TimeTracker

### Use secret to store DB Password
https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-7.0&tabs=linux

## Use a sub query
https://stackoverflow.com/questions/71354466/performing-a-subquery-in-entity-framework-core
Use the Entity.SubEntity.Any method

## Many to many relationships
https://learn.microsoft.com/en-us/ef/core/modeling/relationships/one-to-many

## Understanding cascading delete
To prevent deletion of 'AppUser' from deleting TimeEntries, make the AppUser navigation property on TimeEntry nullable
https://learn.microsoft.com/en-us/ef/core/saving/cascade-delete
