using Microsoft.Data.Sqlite;

namespace VKVideoDesktop.Data.Database;

public static class SchemaMigration
{
    public static void EnsureMigrationsTable(SqliteConnection connection)
    {
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS _SchemaMigrations (
                Version INTEGER PRIMARY KEY,
                Name TEXT NOT NULL,
                AppliedAt TEXT NOT NULL
            )";
        cmd.ExecuteNonQuery();
    }

    public static bool IsApplied(SqliteConnection connection, int version)
    {
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM _SchemaMigrations WHERE Version = @Version";
        cmd.Parameters.AddWithValue("@Version", version);
        var count = (long)(cmd.ExecuteScalar() ?? 0);
        return count > 0;
    }

    public static void Apply(SqliteConnection connection, int version, string name, string[] sqlStatements)
    {
        using var transaction = connection.BeginTransaction(deferred: false);
        try
        {
            if (IsApplied(connection, version))
            {
                transaction.Rollback();
                return;
            }

            foreach (var sql in sqlStatements)
            {
                var cmd = connection.CreateCommand();
                cmd.CommandText = sql;
                cmd.Transaction = transaction;
                cmd.ExecuteNonQuery();
            }

            var insertCmd = connection.CreateCommand();
            insertCmd.Transaction = transaction;
            insertCmd.CommandText = @"
                INSERT INTO _SchemaMigrations (Version, Name, AppliedAt)
                VALUES (@Version, @Name, @AppliedAt)";
            insertCmd.Parameters.AddWithValue("@Version", version);
            insertCmd.Parameters.AddWithValue("@Name", name);
            insertCmd.Parameters.AddWithValue("@AppliedAt", DateTime.UtcNow.ToString("O"));
            insertCmd.ExecuteNonQuery();

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
