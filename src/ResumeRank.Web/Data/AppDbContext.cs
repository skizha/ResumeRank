using Microsoft.EntityFrameworkCore;
using ResumeRank.Web.Models;

namespace ResumeRank.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Resume> Resumes => Set<Resume>();
    public DbSet<RankingResult> RankingResults => Set<RankingResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Resume>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.JobId);
        });

        modelBuilder.Entity<RankingResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.JobId);
            entity.HasOne(e => e.Resume)
                  .WithMany()
                  .HasForeignKey(e => e.ResumeId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
