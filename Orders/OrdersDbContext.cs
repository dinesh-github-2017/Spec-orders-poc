using Microsoft.EntityFrameworkCore;

namespace Spec_orders_poc.Orders;

public class OrdersDbContext : DbContext
{
    public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : base(options)
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(e =>
        {
            e.ToTable("Customers", "dbo");
            e.HasKey(c => c.CustomerId);
            e.Property(c => c.CustomerId).HasColumnName("CustomerId").HasMaxLength(20);
        });

        modelBuilder.Entity<Order>(e =>
        {
            e.ToTable("Orders", "dbo");
            e.HasKey(o => o.OrderId);
            e.Property(o => o.OrderId).HasColumnName("OrderId").ValueGeneratedNever();
            e.Property(o => o.CustomerId).HasColumnName("CustomerId").HasMaxLength(20);
            e.Property(o => o.OrderDate).HasColumnName("OrderDateUtc").HasColumnType("datetime2(0)");
            e.Property(o => o.Total).HasColumnName("Total").HasColumnType("decimal(18,2)");
            e.Property(o => o.Status).HasColumnName("Status").HasMaxLength(20).HasConversion<string>();
            e.HasOne<Customer>().WithMany().HasForeignKey(o => o.CustomerId);
        });
    }
}
