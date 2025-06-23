# Core Access Control Admin Backend API

## Prerequisites

1. [.NET Core 3.1 Sdk](https://www.microsoft.com/net/download)
1. [MS SQL Server 2019](https://www.microsoft.com/en-us/sql-server/sql-server-downloads), you can install on docker if using mac or linux

## Database Setup

1. Create Azure Cosmos DB account
1. [Optionally] Run the console app under 'test_data/', which will create Cosmos DB, Container, and seed sample test data

## Configuration

Following items are configurable from `appsettings.json`

| key                                  | description                                                          |
| ------------------------------------ | -------------------------------------------------------------------- |
| JWT.SecurityKey                      | the JWT security token                                               |
| JWT.Issuer                           | the JWT issuer                                                       |
| JWT.Audience                         | the JWT audience                                                     |
| JWT.ExpirationTimeInMinutes          | the JWT token expiry duration in minute                              |
| ConnectionStrings.Cosmos             | the connection string for Cosmos database                            |
| Cosmos.DbName                        | the Cosmos database name                                             |
| Cosmos.ApplicationContainerName      | the Cosmos Container name for application                            |
| Cosmos.LocationsContainerName        | the Cosmos Container name for locations                              |
| Cosmos.CampusPartitionKey            | the Cosmos Partition Key for Campuss                                 |
| Cosmos.RegionPartitionKey            | the Cosmos Partition Key for Regions                                 |
| Cosmos.ChildLocationPartitionKey     | the Cosmos Partition Key for Child Locations                         |
| Cosmos.AssociatePartitionKey         | the Cosmos Partition Key for Contact Associates                      |
| Cosmos.CampusRolePartitionKey        | the Cosmos Partition Key for Campus Roles                            |
| Cosmos.CalendarPartitionKey          | the Cosmos Partition Key for Calendar (location events)              |

## Run locally

- [Set up the database](#database-setup)
- [Configure the application](#configuration)
- You can configure on **appSettings.\{environment\}.json** file, which will be not pushed to git repository.
- Run to start the API
  ```bash
  dotnet run -p src/Hestia.LocationsMDM.WebApi
  ```
  To watch changes and live reload use
  ```bash
  dotnet watch -p src/Hestia.LocationsMDM.WebApi run
  ```

### Run Tests

- Configure [appsettings.json](tests/Hestia.LocationsMDM.WebApi.Test/appsettings.test.json) see the configuration properties [here](#configuration) for detail
- Build the test solution
  ```bash
  dotnet build tests/Hestia.LocationsMDM.WebApi.Test/
  ```
- Run test without coverage

  ```bash
  dotnet test
  ```

  To generate code coverage report run below

  ```bash
  coverlet tests/Hestia.LocationsMDM.WebApi.Test/bin/Debug/netcoreapp3.1/Hestia.LocationsMDM.WebApi.Test.dll --target "dotnet" --targetargs "test --no-build" --output "./coverage-reports/"  --threshold 85 -f cobertura -f lcov
  ```

  > Make sure you have coverlet installed globally as `dotnet tool install --global coverlet.console`

  Then run below generate HTML report,

  ```bash
  reportgenerator "-reports:coverage-reports/coverage.cobertura.xml" "-targetdir:coverage-reports/html" -reporttypes:HTML
  ```

  > Make sure you have reportgenerator installed globally as `dotnet tool install --global dotnet-reportgenerator-globaltool`

  Now, open `/coverage-reports/html/index.html` in your browser

## Deploy on Azure

To deploy application on Azure, check following articles depending on your needs:
- [Publish with Visual Studio](https://docs.microsoft.com/en-us/aspnet/core/tutorials/publish-to-azure-webapp-using-vs?view=aspnetcore-3.1#deploy-the-app-to-azure)
- [Publish with Visual Studio for Mac](https://docs.microsoft.com/en-us/visualstudio/mac/publish-app-svc?toc=%2Faspnet%2Fcore%2Ftoc.json&bc=%2Faspnet%2Fcore%2Fbreadcrumb%2Ftoc.json&view=vsmac-2019&viewFallbackFrom=aspnetcore-3.1)
- [Publish with the CLI](https://docs.microsoft.com/en-us/azure/app-service/app-service-web-tutorial-dotnetcore-sqldb?toc=%2Faspnet%2Fcore%2Ftoc.json&bc=%2Faspnet%2Fcore%2Fbreadcrumb%2Ftoc.json&view=vsmac-2019)
- [Publish with Visual Studio and Git](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/azure-apps/azure-continuous-deployment?view=aspnetcore-3.1#create-a-web-app-in-the-azure-portal)

## Docker Deployment

- Deployment with Docker is done by executing `docker-compose up` which will setup and deploy Web API.

## Verification

- Configure and deploy the app as described above
- Initialize Cosmos DB with sample data. Under `/test_data` folder execute following command to create DB and seed it with sample data
  ```bash
  dotnet run
  ```
- Import postman collection and environment from `/doc/postman` folder
- Execute scripts one by one to verify app functionality

## Contribution

Please check [Developer Guide](./CONTRIBUTIONS.md)
