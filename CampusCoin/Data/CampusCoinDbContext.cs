using CampusCoin.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CampusCoin.Infrastructure.Data;

public class CampusCoinDbContext : DbContext
{
    public CampusCoinDbContext(
        DbContextOptions<CampusCoinDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<Insight> Insights => Set<Insight>();
    public DbSet<SavingTip> SavingTips => Set<SavingTip>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Bookmark> Bookmarks => Set<Bookmark>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<TipTemplate> TipTemplates => Set<TipTemplate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(x => x.UserId);

            entity.Property(x => x.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Email)
                .HasMaxLength(150)
                .IsRequired();

            entity.HasIndex(x => x.Email)
                .IsUnique();

            entity.Property(x => x.PasswordHash)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(x => x.MonthlyAllowance)
                .HasPrecision(18, 2);

            entity.Property(x => x.SavingsGoal)
                .HasPrecision(18, 2);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(x => x.CategoryId);

            entity.Property(x => x.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasOne(x => x.User)
                .WithMany(x => x.Categories)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(x => x.TransactionId);

            entity.Property(x => x.Amount)
                .HasPrecision(18, 2);

            entity.HasOne(x => x.User)
                .WithMany(x => x.Transactions)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Category)
                .WithMany(x => x.Transactions)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Budget>(entity =>
        {
            entity.HasKey(x => x.BudgetId);

            entity.Property(x => x.LimitAmount)
                .HasPrecision(18, 2);

            entity.HasIndex(x =>
                new { x.UserId, x.CategoryId, x.Month })
                .IsUnique();

            entity.HasOne(x => x.User)
                .WithMany(x => x.Budgets)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Category)
                .WithMany(x => x.Budgets)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Insight>(entity =>
        {
            entity.HasKey(x => x.InsightId);

            entity.HasOne(x => x.User)
                .WithMany(x => x.Insights)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SavingTip>(entity =>
        {
            entity.HasKey(x => x.SavingTipId);

            entity.Property(x => x.PotentialSaving)
                .HasPrecision(18, 2);

            entity.HasOne(x => x.User)
                .WithMany(x => x.SavingTips)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.Category)
                .WithMany(x => x.SavingTips)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(x => x.NotificationId);

            entity.HasOne(x => x.User)
                .WithMany(x => x.Notifications)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Bookmark>(entity =>
        {
            entity.HasKey(x => x.BookmarkId);

            entity.HasOne(x => x.User)
                .WithMany(x => x.Bookmarks)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.Insight)
                .WithMany(x => x.Bookmarks)
                .HasForeignKey(x => x.InsightId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.SavingTip)
                .WithMany(x => x.Bookmarks)
                .HasForeignKey(x => x.SavingTipId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Note>(entity =>
        {
            entity.HasKey(x => x.NoteId);

            entity.HasOne(x => x.User)
                .WithMany(x => x.Notes)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(x => x.Bookmark)
                .WithMany(x => x.Notes)
                .HasForeignKey(x => x.BookmarkId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TipTemplate>(entity =>
        {
            entity.HasKey(x => x.TipTemplateId);

            entity.Property(x => x.Title)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.Description)
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(x => x.IsActive)
                .IsRequired();
        });
    }
}
