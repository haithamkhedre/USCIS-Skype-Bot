using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Web;

namespace USCISBot.Models
{
    public class User : TableEntity
    {

        public User(string partitionKey, string rowKey)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;
        }

        public User() { }

        public string Email { get; set; }

        public string PhoneNumber { get; set; }

        public string Cases { get; set; }
        
    }
}