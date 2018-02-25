using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace USCISBot.Models
{
    public class StorageManager
    {
        public static StorageManager Instance { get; } = new StorageManager();

        private static CloudTable usersTable;

        private static CloudTable ordersTable;

        private static CloudTable knowledgeTable;


        public StorageManager()
        {
            Install();
        }

        private static void Install()
        {
            // Retrieve the storage account from the connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("StorageConnectionString"));

            // Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            // Retrieve a reference to the table.
            usersTable = tableClient.GetTableReference("users");

            // Create the table if it doesn't exist.
            usersTable.CreateIfNotExists();

            // Retrieve a reference to the table.
            ordersTable = tableClient.GetTableReference("orders");

            // Create the table if it doesn't exist.
            ordersTable.CreateIfNotExists();

            // Retrieve a reference to the table.
            knowledgeTable = tableClient.GetTableReference("knowledge");

            // Create the table if it doesn't exist.
            knowledgeTable.CreateIfNotExists();

        }

        public void Add(User user) 
        {
            // Create the TableOperation object that inserts the customer entity.
            TableOperation insertOperation = TableOperation.Insert(user);

            // Execute the insert operation.
            usersTable.Execute(insertOperation);
        }

        public void Add(Order order)
        {
            // Create the TableOperation object that inserts the customer entity.
            TableOperation insertOperation = TableOperation.Insert(order);

            // Execute the insert operation.
            ordersTable.Execute(insertOperation);
        }

        public void Update(User user)
        {
            // Create the Replace TableOperation.
            TableOperation updateOperation = TableOperation.Replace(user);

            // Execute the operation.
            usersTable.Execute(updateOperation);
        }

        public User Get(string partitionKey , string rowKey)
        {
            User result= null;
            // Create a retrieve operation that takes a customer entity.
            TableOperation retrieveOperation = TableOperation.Retrieve<User>(partitionKey, rowKey);

            // Execute the retrieve operation.
            TableResult retrievedResult = usersTable.Execute(retrieveOperation);

            // Print the phone number of the result.
            if (retrievedResult.Result != null)
            {
                result = (User)retrievedResult.Result;
            }

            return result;
        }

        public Order Get(string userId)
        {
            Order result = null;
            // Create a retrieve operation that takes a customer entity.
            TableOperation retrieveOperation = TableOperation.Retrieve<Order>("ORDERSUSA",userId);

            // Execute the retrieve operation.
            TableResult retrievedResult = ordersTable.Execute(retrieveOperation);

            // Print the phone number of the result.
            if (retrievedResult.Result != null)
            {
                result = (Order)retrievedResult.Result;
            }

            return result;
        }

        public void Add(Knowledge knowledge)
        {
            if (GetKnowledge(knowledge.PartitionKey) == null)
            {
                // Create the TableOperation object that inserts the customer entity.
                TableOperation insertOperation = TableOperation.Insert(knowledge);

                // Execute the insert operation.
                knowledgeTable.Execute(insertOperation);
            }
        }

        public void Update(Knowledge knowledge)
        {
            // Create the Replace TableOperation.
            TableOperation updateOperation = TableOperation.Replace(knowledge);

            // Execute the operation.
            knowledgeTable.Execute(updateOperation);
        }

        public Knowledge GetKnowledge(string question)
        {
            // Construct the query operation for all customer entities where PartitionKey="Smith".
            TableQuery<Knowledge> query = new TableQuery<Knowledge>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, question));

            // Print the fields for each customer.
            return knowledgeTable.ExecuteQuery(query).FirstOrDefault();
           
        }

        public List<Knowledge> GetKnowledge()
        {
            return knowledgeTable.ExecuteQuery(new TableQuery<Knowledge>()).ToList();
        }
    }
}