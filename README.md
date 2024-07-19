# MySqlExecutorApi
This app act as simple MySQL client that can run as typical REST API. The sole reason for this project is, i don't want to install MySQL client on some application server, but want to run some MySQL command on it no matter the database server is located.]

## Dependencies
- MySqlConnector
- MySqlConnector.DependencyInjection
- Swashbuckle.AspNetCore

## Main component dependency
- The `MySqlDataSource` that based on `DbDataSource` is registered as singleton lifetime here. It will managed all the connection pool for this app. The `MySqlConnection` is registered as transient lifetime, that will always closed & clear up after all usage.
- I've created the `DbRepo` that act as repository for all data manipulation action. It is registered as scoped lifetime.

## Main endpoints
- GET: /api/command/get-db-status
- GET: /api/command/list-all-table
- GET: /api/command/get-table-info
- POST: /api/command/execute-write-command
- POST: /api/command/execute-read-command

## Running app
- Make sure your machine already have .NET 8 installed
- Download the repo
- Restore all dependency
- Enter `MySqlExecutorApi` (same level with Project.cs)
  - In `appsettings.json`, set the port this app will run & the also the database credential
  - Just run the project using `dotnet run`
