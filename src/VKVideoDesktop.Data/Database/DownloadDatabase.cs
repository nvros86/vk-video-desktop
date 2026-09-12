using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using VKVideoDesktop.Core.Enums;
using VKVideoDesktop.Core.Interfaces;
using VKVideoDesktop.Core.Models;

namespace VKVideoDesktop.Data.Database;

public sealed class DownloadDatabase : IDownloadRepository
{
    private readonly string _connectionString;
    private readonly ILogger<DownloadDatabase> _logger;

    public DownloadDatabase(ILogger<DownloadDatabase> logger) : this(GetDefaultConnectionString(), logger) { }

    public DownloadDatabase(string connectionString, ILogger<DownloadDatabase> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
        InitializeDatabase();
    }

    private static string GetDefaultConnectionString()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(appData, "VKVideoDesktop");
        Directory.CreateDirectory(dir);
        var dbPath = Path.Combine(dir, "downloads.db");
        return $"Data Source={dbPath}";
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        SchemaMigration.EnsureMigrationsTable(connection);
        SchemaMigration.Apply(connection, 1, "Initial Downloads schema", new[]
        {
            @"
            CREATE TABLE IF NOT EXISTS Downloads (
                Id TEXT PRIMARY KEY,
                VideoId TEXT NOT NULL,
                Title TEXT NOT NULL,
                ThumbnailUrl TEXT,
                SourceUrl TEXT,
                DestinationPath TEXT NOT NULL,
                TemporaryPath TEXT,
                Quality TEXT,
                Format TEXT,
                Status INTEGER NOT NULL,
                TotalBytes INTEGER,
                DownloadedBytes INTEGER,
                Speed REAL,
                RetryCount INTEGER,
                ErrorMessage TEXT,
                CreatedAt TEXT,
                StartedAt TEXT,
                CompletedAt TEXT
            )"
        });
    }

    public async Task<IReadOnlyList<DownloadTask>> GetAllAsync()
    {
        var tasks = new List<DownloadTask>();

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Downloads ORDER BY CreatedAt DESC";

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tasks.Add(MapToTask(reader));
        }

        return tasks;
    }

    public async Task<DownloadTask?> GetByIdAsync(string id)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Downloads WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", id);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapToTask(reader);
        }

        return null;
    }

    public async Task SaveAsync(DownloadTask task)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT OR REPLACE INTO Downloads
            (Id, VideoId, Title, ThumbnailUrl, SourceUrl, DestinationPath, TemporaryPath,
             Quality, Format, Status, TotalBytes, DownloadedBytes, Speed, RetryCount,
             ErrorMessage, CreatedAt, StartedAt, CompletedAt)
            VALUES
            (@Id, @VideoId, @Title, @ThumbnailUrl, @SourceUrl, @DestinationPath, @TemporaryPath,
             @Quality, @Format, @Status, @TotalBytes, @DownloadedBytes, @Speed, @RetryCount,
             @ErrorMessage, @CreatedAt, @StartedAt, @CompletedAt)";

        command.Parameters.AddWithValue("@Id", task.Id);
        command.Parameters.AddWithValue("@VideoId", task.VideoId);
        command.Parameters.AddWithValue("@Title", task.Title);
        command.Parameters.AddWithValue("@ThumbnailUrl", (object?)task.ThumbnailUrl ?? DBNull.Value);
        command.Parameters.AddWithValue("@SourceUrl", (object?)task.SourceUrl ?? DBNull.Value);
        command.Parameters.AddWithValue("@DestinationPath", task.DestinationPath);
        command.Parameters.AddWithValue("@TemporaryPath", task.TemporaryPath);
        command.Parameters.AddWithValue("@Quality", (object?)task.Quality ?? DBNull.Value);
        command.Parameters.AddWithValue("@Format", (object?)task.Format ?? DBNull.Value);
        command.Parameters.AddWithValue("@Status", (int)task.Status);
        command.Parameters.AddWithValue("@TotalBytes", (object?)task.TotalBytes ?? DBNull.Value);
        command.Parameters.AddWithValue("@DownloadedBytes", task.DownloadedBytes);
        command.Parameters.AddWithValue("@Speed", task.SpeedBytesPerSecond);
        command.Parameters.AddWithValue("@RetryCount", task.RetryCount);
        command.Parameters.AddWithValue("@ErrorMessage", (object?)task.ErrorMessage ?? DBNull.Value);
        command.Parameters.AddWithValue("@CreatedAt", task.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("@StartedAt", (object?)task.StartedAt?.ToString("O") ?? DBNull.Value);
        command.Parameters.AddWithValue("@CompletedAt", (object?)task.CompletedAt?.ToString("O") ?? DBNull.Value);

        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(string id)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Downloads WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", id);
        await command.ExecuteNonQueryAsync();
    }

    public async Task UpdateStatusAsync(string id, DownloadStatus status)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "UPDATE Downloads SET Status = @Status WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@Status", (int)status);
        await command.ExecuteNonQueryAsync();
    }

    public async Task UpdateProgressAsync(string id, long downloadedBytes, double progress)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "UPDATE Downloads SET DownloadedBytes = @Bytes WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@Bytes", downloadedBytes);
        await command.ExecuteNonQueryAsync();
    }

    private static DownloadTask MapToTask(SqliteDataReader reader)
    {
        return new DownloadTask
        {
            Id = reader.GetString(reader.GetOrdinal("Id")),
            VideoId = reader.GetString(reader.GetOrdinal("VideoId")),
            Title = reader.GetString(reader.GetOrdinal("Title")),
            ThumbnailUrl = reader.IsDBNull(reader.GetOrdinal("ThumbnailUrl"))
                ? string.Empty : reader.GetString(reader.GetOrdinal("ThumbnailUrl")),
            SourceUrl = reader.IsDBNull(reader.GetOrdinal("SourceUrl"))
                ? string.Empty : reader.GetString(reader.GetOrdinal("SourceUrl")),
            DestinationPath = reader.GetString(reader.GetOrdinal("DestinationPath")),
            TemporaryPath = reader.IsDBNull(reader.GetOrdinal("TemporaryPath"))
                ? string.Empty : reader.GetString(reader.GetOrdinal("TemporaryPath")),
            Quality = reader.IsDBNull(reader.GetOrdinal("Quality"))
                ? null : reader.GetString(reader.GetOrdinal("Quality")),
            Format = reader.IsDBNull(reader.GetOrdinal("Format"))
                ? null : reader.GetString(reader.GetOrdinal("Format")),
            Status = (DownloadStatus)reader.GetInt32(reader.GetOrdinal("Status")),
            TotalBytes = reader.IsDBNull(reader.GetOrdinal("TotalBytes"))
                ? null : reader.GetInt64(reader.GetOrdinal("TotalBytes")),
            DownloadedBytes = reader.IsDBNull(reader.GetOrdinal("DownloadedBytes"))
                ? 0 : reader.GetInt64(reader.GetOrdinal("DownloadedBytes")),
            SpeedBytesPerSecond = reader.IsDBNull(reader.GetOrdinal("Speed"))
                ? 0 : reader.GetDouble(reader.GetOrdinal("Speed")),
            RetryCount = reader.IsDBNull(reader.GetOrdinal("RetryCount"))
                ? 0 : reader.GetInt32(reader.GetOrdinal("RetryCount")),
            ErrorMessage = reader.IsDBNull(reader.GetOrdinal("ErrorMessage"))
                ? null : reader.GetString(reader.GetOrdinal("ErrorMessage")),
            CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("CreatedAt"))),
            StartedAt = reader.IsDBNull(reader.GetOrdinal("StartedAt"))
                ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("StartedAt"))),
            CompletedAt = reader.IsDBNull(reader.GetOrdinal("CompletedAt"))
                ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("CompletedAt")))
        };
    }
}
