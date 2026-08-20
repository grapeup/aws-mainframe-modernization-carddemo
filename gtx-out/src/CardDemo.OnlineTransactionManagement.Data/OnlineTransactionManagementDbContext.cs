#nullable enable
using Microsoft.EntityFrameworkCore;
using CardDemo.OnlineTransactionManagement.Domain;

namespace CardDemo.OnlineTransactionManagement.Data;

public class OnlineTransactionManagementDbContext(DbContextOptions<OnlineTransactionManagementDbContext> options)
    : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<CardCrossReference> CardCrossReferences => Set<CardCrossReference>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Account>(e =>
        {
            e.ToTable("accounts");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ActiveStatus).HasColumnName("active_status");
            e.Property(x => x.CurrentBalance).HasColumnName("current_balance");
            e.Property(x => x.CreditLimit).HasColumnName("credit_limit");
            e.Property(x => x.CashCreditLimit).HasColumnName("cash_credit_limit");
            e.Property(x => x.OpenDate).HasColumnName("open_date");
            e.Property(x => x.ExpirationDate).HasColumnName("expiration_date");
            e.Property(x => x.ReissueDate).HasColumnName("reissue_date");
            e.Property(x => x.CurrentCycleCredit).HasColumnName("current_cycle_credit");
            e.Property(x => x.CurrentCycleDebit).HasColumnName("current_cycle_debit");
            e.Property(x => x.AddressZip).HasColumnName("address_zip");
            e.Property(x => x.GroupId).HasColumnName("group_id");
        });
        b.Entity<CardCrossReference>(e =>
        {
            e.ToTable("card_cross_references");
            e.HasKey(x => x.CardNumber);
            e.Property(x => x.CardNumber).HasColumnName("card_number");
            e.Property(x => x.CustomerId).HasColumnName("customer_id");
            e.Property(x => x.AccountId).HasColumnName("account_id");
        });
        b.Entity<Transaction>(e =>
        {
            e.ToTable("transactions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TypeCode).HasColumnName("type_code");
            e.Property(x => x.CategoryCode).HasColumnName("category_code");
            e.Property(x => x.Source).HasColumnName("source");
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.Amount).HasColumnName("amount");
            e.Property(x => x.OriginatedAt).HasColumnName("originated_at");
            e.Property(x => x.ProcessedAt).HasColumnName("processed_at");
            e.Property(x => x.CardNumber).HasColumnName("card_number");
            e.OwnsOne(x => x.Merchant, o =>
            {
                o.Property(x => x.MerchantId).HasColumnName("merchant_id");
                o.Property(x => x.Name).HasColumnName("merchant_name");
                o.Property(x => x.City).HasColumnName("merchant_city");
                o.Property(x => x.ZipCode).HasColumnName("merchant_zip");
            });
        });

        base.OnModelCreating(b);
    }
}