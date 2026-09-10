using Microsoft.Data.Sqlite;
using VKVideoDesktop.Core.Interfaces;
using VKVideoDesktop.Core.Models;

namespace VKVideoDesktop.Data.Database;

public sealed class HistoryDatabase : IHistoryService
{
    private readonly string _connectionString;

    public HistoryDatabase()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(appData, "VKVideoDesktop");
        Directory.CreateDirectory(dir);
        var dbPath = Path.Combine(dir, "app.db");
        _connectionString = $"Data Source={dbPath}";
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS History (
                Id TEXT PRIMARY KEY,
                VideoId TEXT NOT NULL UNIQUE,
                Title TEXT NOT NULL,
                Author TEXT NOT NULL,
                ThumbnailUrl TEXT,
                Duration INTEGER,
                LastPosition INTEGER,
                LastViewed TEXT NOT NULL
            )";
        cmd.ExecuteNonQuery();
    }

    public async Task<IReadOnlyList<HistoryEntry>> GetAllAsync()
    {
        var entries = new List<HistoryEntry>();
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM History ORDER BY LastViewed DESC";
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            entries.Add(new HistoryEntry
            {
                Id = reader.GetString(reader.GetOrdinal("Id")),
                VideoId = reader.GetString(reader.GetOrdinal("VideoId")),
                Title = reader.GetString(reader.GetOrdinal("Title")),
                Author = reader.GetString(reader.GetOrdinal("Author")),
                ThumbnailUrl = reader.IsDBNull(reader.GetOrdinal("ThumbnailUrl"))
                    ? string.Empty : reader.GetString(reader.GetOrdinal("ThumbnailUrl")),
                Duration = TimeSpan.FromTicks(reader.GetInt64(reader.GetOrdinal("Duration"))),
                LastPosition = TimeSpan.FromTicks(reader.GetInt64(reader.GetOrdinal("LastPosition"))),
                LastViewed = DateTime.Parse(reader.GetString(reader.GetOrdinal("LastViewed")))
            });
        }
        return entries;
    }

    public async Task<HistoryEntry?> GetByVideoIdAsync(string videoId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM History WHERE VideoId = @VideoId";
        cmd.Parameters.AddWithValue("@VideoId", videoId);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new HistoryEntry
            {
                Id = reader.GetString(reader.GetOrdinal("Id")),
                VideoId = reader.GetString(reader.GetOrdinal("VideoId")),
                Title = reader.GetString(reader.GetOrdinal("Title")),
                Author = reader.GetString(reader.GetOrdinal("Author")),
                ThumbnailUrl = reader.IsDBNull(reader.GetOrdinal("ThumbnailUrl"))
                    ? string.Empty : reader.GetString(reader.GetOrdinal("ThumbnailUrl")),
                Duration = TimeSpan.FromTicks(reader.GetInt64(reader.GetOrdinal("Duration"))),
                LastPosition = TimeSpan.FromTicks(reader.GetInt64(reader.GetOrdinal("LastPosition"))),
                LastViewed = DateTime.Parse(reader.GetString(reader.GetOrdinal("LastViewed")))
            };
        }
        return null;
    }

    public async Task SaveOrUpdateAsync(HistoryEntry entry)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO History (Id, VideoId, Title, Author, ThumbnailUrl, Duration, LastPosition, LastViewed)
            VALUES (@Id, @VideoId, @Title, @Author, @ThumbnailUrl, @Duration, @LastPosition, @LastViewed)";
        cmd.Parameters.AddWithValue("@Id", entry.Id);
        cmd.Parameters.AddWithValue("@VideoId", entry.VideoId);
        cmd.Parameters.AddWithValue("@Title", entry.Title);
        cmd.Parameters.AddWithValue("@Author", entry.Author);
        cmd.Parameters.AddWithValue("@ThumbnailUrl", (object?)entry.ThumbnailUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Duration", entry.Duration.Ticks);
        cmd.Parameters.AddWithValue("@LastPosition", entry.LastPosition.Ticks);
        cmd.Parameters.AddWithValue("@LastViewed", entry.LastViewed.ToString("O"));
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(string entryId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM History WHERE Id = @Id";
        cmd.Parameters.AddWithValue("@Id", entryId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task ClearAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM History";
        await cmd.ExecuteNonQueryAsync();
    }
}
