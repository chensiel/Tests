using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using EFCore.BulkExtensions;
using Newtonsoft.Json;
using System.IO;

namespace Tests
{
    public class Test1
    {
        public class Trade
        {
            [Key]
            public string TradeId { get; set; }
            public ICollection<Order> OrderList { get; set; }
        }
        public class Order
        {
            [Key]
            public string OrderId { get; set; }
            public string WayCom { get; set; }
            public string WayBill { get; set; }
            public Trade Trade { get; set; }
        }
        public class Shipping
        {
            [Key]
            public string ShippingId { get; set; }
            public string WayCom { get; set; }
            public string WayBill { get; set; }
            public string ShippingLotNo { get; set; }
        }

        public class AppDbContext : DbContext
        {
            public DbSet<Trade> Trade { get; set; }
            public DbSet<Order> Order { get; set; }
            public DbSet<Shipping> Shipping { get; set; }

            public AppDbContext()
            {
            }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                if (!optionsBuilder.IsConfigured)
                {
                    optionsBuilder.UseSqlite("Data Source=test.db;");
                }
                base.OnConfiguring(optionsBuilder);
            }
        }

        public void Test()
        {
            var db = new AppDbContext();
            db.Database.EnsureCreated();
            db.Database.ExecuteSqlRaw("delete from Shipping");
            db.Database.ExecuteSqlRaw("delete from \"Order\"");
            db.Database.ExecuteSqlRaw("delete from Trade");

            var tradeList = JsonConvert.DeserializeObject<Trade[]>(File.ReadAllText("trade.json"));
            var shippingList = JsonConvert.DeserializeObject<Shipping[]>(File.ReadAllText("shipping.json"));

            db.Trade.AddRange(tradeList);
            db.Shipping.AddRange(shippingList);
            db.SaveChanges();

            var query = from q1 in db.Order
                        join j1 in db.Shipping on new { q1.WayCom, q1.WayBill } equals new { j1.WayCom, j1.WayBill }
                        select new
                        {
                            j1.ShippingLotNo,
                            q1.Trade.TradeId
                        } into q2
                        group q2 by new { q2.ShippingLotNo, q2.TradeId } into g1
                        select new { g1.Key.ShippingLotNo, g1.Key.TradeId } into q3
                        group q3 by new { q3.ShippingLotNo } into g2
                        select new { g2.Key.ShippingLotNo, Count = g2.Count() };
            var sql = query.ToParametrizedSql().Item1;
            var items = query.ToArray();
        }
    }
}
