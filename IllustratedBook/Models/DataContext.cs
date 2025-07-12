using Microsoft.EntityFrameworkCore;

namespace IllustratedBook.Models
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options)
            : base(options) {}

        public DbSet<Book> Books { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Section> Sections { get; set; }
        public DbSet<Image> Images { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<Book>()
                .HasOne(b => b.Author)
                .WithMany()
                .HasForeignKey("AuthorId");

            modelBuilder.Entity<Section>()
                .HasOne(s => s.Book)
                .WithMany()
                .HasForeignKey(s => s.BookId);

            // Configure self-referencing relationship for Section (parent-child)
            modelBuilder.Entity<Section>()
                .HasOne<Section>()
                .WithMany()
                .HasForeignKey(s => s.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Image relationships with NO ACTION to avoid cascade issues
            modelBuilder.Entity<Image>()
                .HasOne(i => i.Book)
                .WithMany()
                .HasForeignKey(i => i.BookId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Image>()
                .HasOne(i => i.Chapter)
                .WithMany()
                .HasForeignKey(i => i.ChapterId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
} 