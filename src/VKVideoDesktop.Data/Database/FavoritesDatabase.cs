using Microsoft.Data.Sqlite;
using VKVideoDesktop.Core.Interfaces;
using VKVideoDesktop.Core.Models;

namespace VKVideoDesktop.Data.Database;

public sealed class FavoritesDatabase : IFavoritesService
{
    private readonly string _connectionString;

    public FavoritesDatabase() : this(GetDefaultConnectionString()) { }

    public FavoritesDatabase(string connectionString)
    {
        _connectionString = connectionString;
        InitializeDatabase();
    }

    private static string GetDefaultConnectionString()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(appData, "VKVideoDesktop");
        Directory.CreateDirectory(dir);
        var dbPath = Path.Combine(dir, "app.db");
        return $"Data Source={dbPath}";
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        SchemaMigration.EnsureMigrationsTable(connection);
        SchemaMigration.Apply(connection, 1, "Initial Favorites schema", new[]
        {
            @"
            CREATE TABLE IF NOT EXISTS Favorites (
                Id TEXT PRIMARY KEY,
                VideoId TEXT NOT NULL UNIQUE,
                Title TEXT NOT NULL,
                Author TEXT NOT NULL,
                ThumbnailUrl TEXT,
                Duration INTEGER,
                AddedAt TEXT NOT NULL
            )"
        });
    }

    public async Task<IReadOnlyList<FavoriteEntry>> GetAllAsync()
    {
        var entries = new List<FavoriteEntry>();
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM Favorites ORDER BY AddedAt DESC";
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            entries.Add(new FavoriteEntry
            {
                Id = reader.GetString(reader.GetOrdinal("Id")),
                VideoId = reader.GetString(reader.GetOrdinal("VideoId")),
                Title = reader.GetString(reader.GetOrdinal("Title")),
                Author = reader.GetString(reader.GetOrdinal("Author")),
                ThumbnailUrl = reader.IsDBNull(reader.GetOrdinal("ThumbnailUrl"))
                    ? string.Empty : reader.GetString(reader.GetOrdinal("ThumbnailUrl")),
                Duration = TimeSpan.FromTicks(reader.GetInt64(reader.GetOrdinal("Duration"))),
                AddedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("AddedAt")))
            });
        }
        return entries;
    }

    public async Task<bool> IsFavoriteAsync(string videoId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Favorites WHERE VideoId = @VideoId";
        cmd.Parameters.AddWithValue("@VideoId", videoId);
        var count = (long)(await cmd.ExecuteScalarAsync())!;
        return count > 0;
    }

    public async Task AddAsync(FavoriteEntry entry)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT OR IGNORE INTO Favorites (Id, VideoId, Title, Author, ThumbnailUrl, Duration, AddedAt)
            VALUES (@Id, @VideoId, @Title, @Author, @ThumbnailUrl, @Duration, @AddedAt)";
        cmd.Parameters.AddWithValue("@Id", entry.Id);
        cmd.Parameters.AddWithValue("@VideoId", entry.VideoId);
        cmd.Parameters.AddWithValue("@Title", entry.Title);
        cmd.Parameters.AddWithValue("@Author", entry.Author);
        cmd.Parameters.AddWithValue("@ThumbnailUrl", (object?)entry.ThumbnailUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Duration", entry.Duration.Ticks);
        cmd.Parameters.AddWithValue("@AddedAt", entry.AddedAt.ToString("O"));
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task RemoveAsync(string videoId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM Favorites WHERE VideoId = @VideoId";
        cmd.Parameters.AddWithValue("@VideoId", videoId);
        await cmd.ExecuteNonQueryAsync();
    }
}
