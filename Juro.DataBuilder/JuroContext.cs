using Juro.DataBuilder.Models;
using Microsoft.EntityFrameworkCore;

namespace Juro.DataBuilder;

public class JuroContext : DbContext
{
    public DbSet<ManamiAnimeItem> AnimeItems { get; set; }

    public DbSet<AnimeSeason> AnimeSeasons { get; set; }

    public DbSet<AnimeModel> Gogoanime { get; set; }

    public DbSet<AnimeModel> AnimePahe { get; set; }

    public DbSet<AnimeModel> Kaido { get; set; }

    public DbSet<AnimeModel> Aniwave { get; set; }

    public DbSet<AnimeModel> OtakuDesu { get; set; }

    public string DbPath { get; }

    public JuroContext()
    {
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        DbPath = Path.Join(path, "juro.db");
    }

    public JuroContext(string dbPath)
    {
        DbPath = dbPath;
    }

    // The following configures EF to create a Sqlite database file in the
    // special "local" folder for your platform.
    protected override void OnConfiguring(DbContextOptionsBuilder options) =>
        options.UseSqlite($"Data Source={DbPath}");
}
