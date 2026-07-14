using BookQuery.Service.Models;
using Microsoft.EntityFrameworkCore;

namespace BookQuery.Service.Context
{
    public class BookContext : DbContext
    {
        public BookContext(DbContextOptions<BookContext> options) : base(options)
        {
        }

        public DbSet<Book> Books => Set<Book>();
        public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Book>(entity =>
            {
                entity.ToTable(nameof(Book));
                entity.Property(b => b.Id).ValueGeneratedNever();
                entity.Property(b => b.Title).HasMaxLength(50);
                entity.Property(b => b.Description).HasMaxLength(100);
                entity.Property(b => b.Author).HasMaxLength(50);
                entity.Property(b => b.IsDeleted).HasDefaultValue(false);
                entity.HasQueryFilter(m => !m.IsDeleted);
            });

            modelBuilder.Entity<ProcessedMessage>(entity =>
            {
                entity.ToTable("ProcessedMessage");
                entity.HasKey(x => x.MessageId);
            });
        }

        public override int SaveChanges()
        {
            UpdateSoftDeleteStatuses();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            UpdateSoftDeleteStatuses();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void UpdateSoftDeleteStatuses()
        {
            foreach (var entry in ChangeTracker.Entries<Book>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.IsDeleted = false;
                        break;
                    case EntityState.Deleted:
                        entry.State = EntityState.Modified;
                        entry.Entity.IsDeleted = true;
                        break;
                }
            }
        }
    }
}
