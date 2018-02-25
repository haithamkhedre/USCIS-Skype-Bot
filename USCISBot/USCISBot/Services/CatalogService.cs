namespace USCISBot.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Models;

    public class CatalogService
    {
        private static readonly IEnumerable<CatalogItem> FakeCatalogRepository = new List<CatalogItem>
        {
            new CatalogItem
            {
                Currency = "USD",
                Description = "USCIS Non Official Bot Lifetime Subscription , you will never regret ! ,  will check daily for you",
                Id = Guid.NewGuid(),
                ImageUrl = "http://uscisbot2017.azurewebsites.net/images/lt.png",
                Price = 0.5,
                Title = "USCIS Non Official Bot - Lifetime Subscription "
            }
        };

        public Task<CatalogItem> GetItemByIdAsync(Guid itemId)
        {
            return Task.FromResult(FakeCatalogRepository.FirstOrDefault(o => o.Id.Equals(itemId)));
        }

        public Task<CatalogItem> GetRandomItemAsync()
        {
            // getting a random item - currently we have only one choice :p
            return Task.FromResult(FakeCatalogRepository.First());
        }
    }
}