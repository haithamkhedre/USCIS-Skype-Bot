using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace USCISBot.Models
{
    public class Knowledge : TableEntity
    {
        public Knowledge(string question , string answer)
        {
            this.PartitionKey = question;
            this.RowKey = answer;
        }

        public Knowledge()
        {

        }

        public string Answer { get; set; }

        public string UserName { get; set; }

        public string UserId { get; set; }

        public decimal Score { get; set; }

        public string Category { get; set; }

        public string Nurons { get; set; }

        public bool CanModify { get; set; }
    }
        
}