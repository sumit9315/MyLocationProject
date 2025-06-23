using System;
using System.IO;
using System.Linq;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace Hestia.LocationsMDM.WebApi.Test
{
    public static class TestDataManager
    {
        const string TestDataDir = "SampleData";
        private const string PartitionKey = "partition_key";
        private static CosmosClient _cosmosClient;

        public static void SetUp(IConfiguration configuration)
        {
            var connString = configuration.GetConnectionString("Cosmos");
            _cosmosClient = new CosmosClient(connString);

            string dbName = configuration["Cosmos:DbName"];
            string containerName = configuration["Cosmos:LocationsContainerName"];

            var db = CreateDatabase(dbName);
            var container = CreateContainer(db, containerName);

            // ClearContainerData(container);
            AddContainerData(container);
        }

        private static void AddContainerData(Container container)
        {
            var files = Directory.GetFiles(TestDataDir, "*.json", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                string jsonContent = File.ReadAllText(file);
                var jObj = JObject.Parse(jsonContent);
                if (!jObj.ContainsKey("id"))
                {
                    jObj["id"] = Guid.NewGuid().ToString();
                }

                var obj = jObj.ToObject<object>();
                var result = container.CreateItemAsync(obj).Result;
            }
        }

        private static void ClearContainerData(Container container)
        {
            var items = container.GetItemLinqQueryable<dynamic>(allowSynchronousQueryExecution: true)
                .ToList();

            foreach (var item in items)
            {
                container.DeleteItemAsync<dynamic>(item.id.ToString(), new PartitionKey(item.partition_key?.ToString() ?? "")).Wait();
            }
        }

        private static Database CreateDatabase(string dbName)
        {
            var db = _cosmosClient.CreateDatabaseIfNotExistsAsync(dbName, ThroughputProperties.CreateManualThroughput(400)).Result.Database;
            return db;
        }

        private static Container CreateContainer(Database db, string containerName)
        {
            DeleteContainer(db, containerName);
            var container = db.CreateContainerAsync(new ContainerProperties
            {
                Id = containerName,
                PartitionKeyPath = $"/{PartitionKey}"
            }).Result.Container;

            return container;
        }

        private static void DeleteContainer(Database db, string containerName)
        {
            var container = db.GetContainer(containerName);
            try
            {
                container.DeleteContainerAsync().Wait();
            }
            catch
            {
                // ignore
            }
        }
    }
}
