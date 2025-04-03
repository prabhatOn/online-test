using Microsoft.EntityFrameworkCore;
using OnlineAssessment.Web.Models;

namespace OnlineAssessment.Web.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

        public DbSet<User> Users { get; set; }
        public DbSet<Organization> Organizations { get; set; }
        public DbSet<Test> Tests { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<AnswerOption> AnswerOptions { get; set; }
        public DbSet<TestCase> TestCases { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ✅ Store UserRole Enum as a string in the database
            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<string>();

            base.OnModelCreating(modelBuilder);
        }
    }
}
