using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Spec_orders_poc.Orders;

namespace Spec_orders_poc.Tests.Infrastructure;

public class OrdersApiFactory : WebApplicationFactory<Program>
{
    public const string CustomerC1 = "C1";
    public const string CustomerEmpty = "C-EMPTY";
    public const string CustomerBig = "C-BIG";
    public const string Customer10K = "C-10K";

    private readonly string _databaseName = $"OrdersPocTests_{Guid.NewGuid():N}";

    public string ConnectionString =>
        $"Server=(localdb)\\MSSQLLocalDB;Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<OrdersDbContext>>();
            services.AddDbContext<OrdersDbContext>(o => o.UseSqlServer(ConnectionString));

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    options.DefaultForbidScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName, _ => { });
        });

        CreateAndSeedDatabase();
    }

    private void CreateAndSeedDatabase()
    {
        var options = new DbContextOptionsBuilder<OrdersDbContext>().UseSqlServer(ConnectionString).Options;
        using var db = new OrdersDbContext(options);
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();

        db.Customers.AddRange(
            new Customer { CustomerId = CustomerC1 },
            new Customer { CustomerId = CustomerEmpty },
            new Customer { CustomerId = CustomerBig },
            new Customer { CustomerId = Customer10K });
        db.SaveChanges();

        long nextId = 1;
        var statuses = Enum.GetValues<OrderStatus>();

        // C1: 45 orders over all statuses, one at 2026-05-31T23:59:00Z
        var c1 = new List<Order>();
        for (var i = 0; i < 44; i++)
        {
            c1.Add(new Order
            {
                OrderId = nextId++,
                CustomerId = CustomerC1,
                OrderDate = new DateTime(2026, 5, 1, 8, 0, 0, DateTimeKind.Utc).AddHours(i * 12),
                Total = 10m + i,
                Status = statuses[i % statuses.Length]
            });
        }
        c1.Add(new Order
        {
            OrderId = nextId++,
            CustomerId = CustomerC1,
            OrderDate = new DateTime(2026, 5, 31, 23, 59, 0, DateTimeKind.Utc),
            Total = 99.99m,
            Status = OrderStatus.Shipped
        });
        db.Orders.AddRange(c1);
        db.SaveChanges();

        nextId = SeedBulk(db, CustomerBig, 10_001, nextId);
        SeedBulk(db, Customer10K, 10_000, nextId);
    }

    private static long SeedBulk(OrdersDbContext db, string customerId, int count, long nextId)
    {
        var statuses = Enum.GetValues<OrderStatus>();
        var baseDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        for (var offset = 0; offset < count; offset += 1000)
        {
            var batch = new List<Order>();
            for (var i = offset; i < Math.Min(offset + 1000, count); i++)
            {
                batch.Add(new Order
                {
                    OrderId = nextId++,
                    CustomerId = customerId,
                    OrderDate = baseDate.AddMinutes(i),
                    Total = 5m + (i % 500),
                    Status = statuses[i % statuses.Length]
                });
            }
            db.Orders.AddRange(batch);
            db.SaveChanges();
            db.ChangeTracker.Clear();
        }

        return nextId;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            var options = new DbContextOptionsBuilder<OrdersDbContext>().UseSqlServer(ConnectionString).Options;
            using var db = new OrdersDbContext(options);
            db.Database.EnsureDeleted();
        }
    }
}
