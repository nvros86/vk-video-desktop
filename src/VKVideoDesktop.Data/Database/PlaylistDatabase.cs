using System.Text.Json;
using Microsoft.Data.Sqlite;
using VKVideoDesktop.Core.Interfaces;
using VKVideoDesktop.Core.Models;

namespace VKVideoDesktop.Data.Database;

public sealed class PlaylistDatabase : IPlaylistService
{
    private readonly string _connectionString;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public PlaylistDatabase() : this(GetDefaultConnectionString()) { }

    public PlaylistDatabase(string connectionString)
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
        SchemaMigration.Apply(connection, 1, "Initial Playlists schema", new[]
        {
            @"
            CREATE TABLE IF NOT EXISTS Playlists (
                Id TEXT PRIMARY KEY,
                Title TEXT NOT NULL,
                Description TEXT,
                ThumbnailUrl TEXT,
                VideoCount INTEGER,
                VideosJson TEXT,
                CreatedAt TEXT NOT NULL
            )"
        });
    }

    public async Task<IReadOnlyList<Playlist>> GetAllAsync()
    {
        var playlists = new List<Playlist>();
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM Playlists ORDER BY CreatedAt DESC";
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            playlists.Add(MapToPlaylist(reader));
        }
        return playlists;
    }

    public async Task<Playlist?> GetByIdAsync(string playlistId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM Playlists WHERE Id = @Id";
        cmd.Parameters.AddWithValue("@Id", playlistId);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return MapToPlaylist(reader);
        return null;
    }

    public async Task<Playlist> CreateAsync(string title, string? description)
    {
        await _lock.WaitAsync();
        try
        {
            var playlist = new Playlist
            {
                Id = Guid.NewGuid().ToString(),
                Title = title,
                Description = description,
                CreatedAt = DateTime.UtcNow
            };

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Playlists (Id, Title, Description, VideoCount, VideosJson, CreatedAt)
                VALUES (@Id, @Title, @Description, 0, '[]', @CreatedAt)";
            cmd.Parameters.AddWithValue("@Id", playlist.Id);
            cmd.Parameters.AddWithValue("@Title", playlist.Title);
            cmd.Parameters.AddWithValue("@Description", (object?)playlist.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedAt", playlist.CreatedAt.ToString("O"));
            await cmd.ExecuteNonQueryAsync();
            return playlist;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task RenameAsync(string playlistId, string newTitle)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE Playlists SET Title = @Title WHERE Id = @Id";
        cmd.Parameters.AddWithValue("@Id", playlistId);
        cmd.Parameters.AddWithValue("@Title", newTitle);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(string playlistId)
    {
        await _lock.WaitAsync();
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM Playlists WHERE Id = @Id";
            cmd.Parameters.AddWithValue("@Id", playlistId);
            await cmd.ExecuteNonQueryAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task AddVideoAsync(string playlistId, Video video)
    {
        await _lock.WaitAsync();
        try
        {
            var playlist = await GetByIdAsync(playlistId);
            if (playlist == null) return;

            var videos = new List<Video>(playlist.Videos) { video };
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var cmd = connection.CreateCommand();
            cmd.CommandText = "UPDATE Playlists SET VideosJson = @VideosJson, VideoCount = @Count WHERE Id = @Id";
            cmd.Parameters.AddWithValue("@Id", playlistId);
            cmd.Parameters.AddWithValue("@VideosJson", JsonSerializer.Serialize(videos));
            cmd.Parameters.AddWithValue("@Count", videos.Count);
            await cmd.ExecuteNonQueryAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task RemoveVideoAsync(string playlistId, string videoId)
    {
        await _lock.WaitAsync();
        try
        {
            var playlist = await GetByIdAsync(playlistId);
            if (playlist == null) return;

            var videos = new List<Video>(playlist.Videos);
            videos.RemoveAll(v => v.Id == videoId);
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var cmd = connection.CreateCommand();
            cmd.CommandText = "UPDATE Playlists SET VideosJson = @VideosJson, VideoCount = @Count WHERE Id = @Id";
            cmd.Parameters.AddWithValue("@Id", playlistId);
            cmd.Parameters.AddWithValue("@VideosJson", JsonSerializer.Serialize(videos));
            cmd.Parameters.AddWithValue("@Count", videos.Count);
            await cmd.ExecuteNonQueryAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ReorderVideoAsync(string playlistId, string videoId, int newIndex)
    {
        await _lock.WaitAsync();
        try
        {
            var playlist = await GetByIdAsync(playlistId);
            if (playlist == null) return;

            var videos = new List<Video>(playlist.Videos);
            var video = videos.FirstOrDefault(v => v.Id == videoId);
            if (video == null) return;

            videos.Remove(video);
            newIndex = Math.Clamp(newIndex, 0, videos.Count);
            videos.Insert(newIndex, video);

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var cmd = connection.CreateCommand();
            cmd.CommandText = "UPDATE Playlists SET VideosJson = @VideosJson, VideoCount = @Count WHERE Id = @Id";
            cmd.Parameters.AddWithValue("@Id", playlistId);
            cmd.Parameters.AddWithValue("@VideosJson", JsonSerializer.Serialize(videos));
            cmd.Parameters.AddWithValue("@Count", videos.Count);
            await cmd.ExecuteNonQueryAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    private static Playlist MapToPlaylist(SqliteDataReader reader)
    {
        var videosJson = reader.IsDBNull(reader.GetOrdinal("VideosJson"))
            ? "[]" : reader.GetString(reader.GetOrdinal("VideosJson"));
        var videos = JsonSerializer.Deserialize<List<Video>>(videosJson) ?? new List<Video>();

        return new Playlist
        {
            Id = reader.GetString(reader.GetOrdinal("Id")),
            Title = reader.GetString(reader.GetOrdinal("Title")),
            Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                ? null : reader.GetString(reader.GetOrdinal("Description")),
            ThumbnailUrl = reader.IsDBNull(reader.GetOrdinal("ThumbnailUrl"))
                ? null : reader.GetString(reader.GetOrdinal("ThumbnailUrl")),
            VideoCount = reader.GetInt32(reader.GetOrdinal("VideoCount")),
            Videos = videos,
            CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("CreatedAt")))
        };
    }
}
