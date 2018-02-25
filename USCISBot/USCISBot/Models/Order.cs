using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace USCISBot.Models
{
    public class Order : TableEntity
    {
        public Order(string userId)
        {
            this.PartitionKey = "ORDERSUSA";
            this.RowKey = userId;
        }

        public Order() { }

        public string ShippingAddress { get; set; }

        public string ShippingOption { get; set; }

        public string Title { get; set; }

        public decimal Total { get; set; }

        public string OrderId { get; set; }

        public string TransactionId { get; set; }

    }
}