using Azure.Data.Tables;
using NUnit.Framework;
using Rebus.Sagas;
using Rebus.Tests.Contracts.Sagas;

namespace Rebus.AzureServiceBus.Tests
{
    [TestFixture, Category("TableStorage")]
    public class TableStorageSagaStorageBasicLoadAndSaveAndFindOperations : BasicLoadAndSaveAndFindOperations<TableStorageSagaStorageFactory> { }

    [TestFixture, Category("TableStorage")]
    public class TableStorageSagaStorageConcurrencyHandling : ConcurrencyHandling<TableStorageSagaStorageFactory> { }

    [TestFixture, Category("TableStorage")]
    public class TableStorageSagaStorageSagaIntegrationTests : SagaIntegrationTests<TableStorageSagaStorageFactory> { }

    public class TableStorageSagaStorageFactory : ISagaStorageFactory
    {
        const string DataTableName = "RebusSagaData";
        static readonly string ConnectionString = TsTestConfig.ConnectionString;

        public TableStorageSagaStorageFactory()
        {
            CleanUp();
        }

        public ISagaStorage GetSagaStorage()
        {
            var tableClient = new TableClient(ConnectionString, DataTableName);
            var storage = new TableStorageSagaStorage(tableClient);

            return storage;
        }

        public void CleanUp()
        {
            try
            {
                var tableClient = new TableClient(ConnectionString, DataTableName);
                tableClient.CreateIfNotExists();
                var result = tableClient.Query<TableEntity>();
                foreach (var item in result)
                {
                    tableClient.DeleteEntity(item.PartitionKey, item.RowKey);
                }
            }
            catch { }
        }
    }
}
