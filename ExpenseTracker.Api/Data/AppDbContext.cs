using ExpenseTracker.Api.Entities;
using ExpenseTracker.Api.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Data;

// AppDbContext is EF Core's main entry point for queries, saves, and model configuration.
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>(); // Set is a built-in method from DbContext, returns DbSet<User> dynamically.

    public DbSet<Category> Categories => Set<Category>(); 

    public DbSet<Expense> Expenses => Set<Expense>();

    public DbSet<Budget> Budgets => Set<Budget>();

    public DbSet<FinancialMessage> FinancialMessages => Set<FinancialMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // User email is unique and required because it is the login identity.
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Email).HasMaxLength(256).IsRequired();
            entity.HasIndex(user => user.Email).IsUnique();
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(category => category.Id);
            entity.Property(category => category.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(category => new { category.UserId, category.Name }).IsUnique();
            entity.Property(category => category.Type).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<Expense>(entity =>
        {
            entity.HasKey(expense => expense.Id);
            entity.Property(expense => expense.Description).HasMaxLength(200).IsRequired();
            entity.Property(expense => expense.Amount).HasPrecision(18, 2);
            entity.Property(expense => expense.Currency).HasMaxLength(10).HasDefaultValue("ILS").IsRequired();
            entity.Property(expense => expense.Notes).HasMaxLength(1000);
            entity.Property(expense => expense.Merchant).HasMaxLength(200);
            entity.Property(expense => expense.CategorySource).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(expense => new { expense.UserId, expense.ExpenseDate });
            entity.HasIndex(expense => new { expense.UserId, expense.CategoryId });
            // Currency is system-managed, so the database enforces ILS as well.
            entity.ToTable(table => table.HasCheckConstraint("CK_Expenses_Currency_ILS", "\"Currency\" = 'ILS'")); // enforce 
                                                                                                    // ILS to the whole column.
            entity.HasOne(expense => expense.User)
                .WithMany(user => user.Expenses)
                .HasForeignKey(expense => expense.UserId)
                .OnDelete(DeleteBehavior.Cascade); // if user is deleted, all its expenses is deleted automatically.
            entity.HasOne(expense => expense.Category)
                .WithMany(category => category.Expenses)
                .HasForeignKey(expense => expense.CategoryId)
                .OnDelete(DeleteBehavior.Restrict); // if category is deleted, the expenses will not be deleted, 
                    // but their CategoryId will be set to null (if nullable) or throw an error (if not nullable). 
                    // In this case, we choose to throw an error to prevent orphaned expenses without a category.
        });

        modelBuilder.Entity<Budget>(entity =>
        {
            entity.HasKey(budget => budget.Id);
            entity.Property(budget => budget.Amount).HasPrecision(18, 2);
            entity.HasIndex(budget => new { budget.UserId, budget.CategoryId }).IsUnique();
            entity.HasOne(budget => budget.User)
                .WithMany(user => user.Budgets)
                .HasForeignKey(budget => budget.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(budget => budget.Category)
                .WithMany(category => category.Budgets)
                .HasForeignKey(budget => budget.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FinancialMessage>(entity =>
        {
            entity.HasKey(message => message.Id);
            entity.Property(message => message.Title).HasMaxLength(150).IsRequired();
            entity.Property(message => message.Message).HasMaxLength(1000).IsRequired();
            entity.Property(message => message.Type).HasConversion<string>().HasMaxLength(30);
            entity.Property(message => message.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(message => message.Severity).HasConversion<string>().HasMaxLength(20);
            entity.Property(message => message.ContextJson).HasMaxLength(4000);
            entity.HasIndex(message => new { message.UserId, message.Status, message.CreatedAtUtc });
            entity.HasIndex(message => new { message.UserId, message.Type, message.CreatedAtUtc });
            entity.HasOne(message => message.User)
                .WithMany(user => user.FinancialMessages)
                .HasForeignKey(message => message.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Built-in categories are seeded into the model so every environment starts with the same base set.
        var seedTimestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var seedCategories = SystemCategoryCatalog.Names.Select((name, index) => new Category
        {
            Id = Guid.Parse($"00000000-0000-0000-0000-{index + 1:000000000000}"),
            Name = name,
            Type = CategoryType.System,
            UserId = null,
            CreatedAtUtc = seedTimestamp,
            UpdatedAtUtc = seedTimestamp
        });

        modelBuilder.Entity<Category>().HasData(seedCategories);
    }
}
