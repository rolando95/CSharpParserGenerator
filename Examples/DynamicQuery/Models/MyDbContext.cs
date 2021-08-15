using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DynamicQuery.Models;
using Microsoft.EntityFrameworkCore;

namespace DynamicQuery.DBContext
{
    public class MyDbContext : DbContext
    {
        public DbSet<Person> People { get; set; }
        public DbSet<Address> Addresses { get; set; }

        public MyDbContext(DbContextOptions options) : base(options)
        {
            if (!People.Any()) LoadDataInMemory();
        }

        public void LoadDataInMemory()
        {
            string file = System.IO.File.ReadAllText("dataSeed.json");
            var people = JsonSerializer.Deserialize<List<Person>>(file, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            People.AddRange(people);

            SaveChanges();
        }
    }
}