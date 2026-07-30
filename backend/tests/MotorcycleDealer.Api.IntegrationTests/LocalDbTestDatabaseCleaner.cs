using System.Collections.Concurrent;
using Microsoft.Data.SqlClient;

namespace MotorcycleDealer.Api.IntegrationTests;

internal static class LocalDbTestDatabaseCleaner
{
    private const string MasterConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=15";

    private const string CanonicalDatabaseName = "MotorcycleDealerDb";

    private static readonly string[] EphemeralTestDatabasePrefixes =
    [
        "MotorcycleDealerDb_Forensic_",
        "MotorcycleDealerDb_P4Rbac_",
        "MotorcycleDealerDb_P4Test_",
        "MotorcycleDealerDb_P5Show_",
        "MotorcycleDealerDb_P5Showroom_",
        "MotorcycleDealerDb_P5Book_",
        "MotorcycleDealerDb_P9_",
        "MotorcycleDealerDb_E2E_"
    ];

    private static readonly ConcurrentDictionary<string, byte> ActiveFixtureDatabases =
        new(StringComparer.OrdinalIgnoreCase);

    public static void RegisterActiveDatabase(string databaseName)
    {
        if (!string.IsNullOrWhiteSpace(databaseName) &&
            !string.Equals(databaseName, CanonicalDatabaseName, StringComparison.OrdinalIgnoreCase))
        {
            ActiveFixtureDatabases.TryAdd(databaseName, 0);
        }
    }

    public static async Task DropFixtureAndOrphanTestDatabasesAsync(string fixtureDatabaseName)
    {
        try
        {
            await using var conn = new SqlConnection(MasterConnectionString);
            await conn.OpenAsync();

            if (IsSafeEphemeralTestDatabaseName(fixtureDatabaseName))
            {
                await TryDropSingleDatabaseAsync(conn, fixtureDatabaseName);
            }

            ActiveFixtureDatabases.TryRemove(fixtureDatabaseName, out _);

            var orphanCandidates = new List<string>();
            await using (var listCmd = conn.CreateCommand())
            {
                listCmd.CommandText = """
                    SELECT [name]
                    FROM [sys].[databases]
                    WHERE [name] <> N'MotorcycleDealerDb'
                      AND (
                          [name] LIKE N'MotorcycleDealerDb\_Forensic\_%' ESCAPE N'\'
                       OR [name] LIKE N'MotorcycleDealerDb\_P4Rbac\_%' ESCAPE N'\'
                       OR [name] LIKE N'MotorcycleDealerDb\_P4Test\_%' ESCAPE N'\'
                       OR [name] LIKE N'MotorcycleDealerDb\_P5Show\_%' ESCAPE N'\'
                       OR [name] LIKE N'MotorcycleDealerDb\_P5Showroom\_%' ESCAPE N'\'
                       OR [name] LIKE N'MotorcycleDealerDb\_P5Book\_%' ESCAPE N'\'
                       OR [name] LIKE N'MotorcycleDealerDb\_P9\_%' ESCAPE N'\'
                       OR [name] LIKE N'MotorcycleDealerDb\_E2E\_%' ESCAPE N'\'
                      );
                    """;

                await using var reader = await listCmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var candidate = reader.GetString(0);
                    if (IsSafeEphemeralTestDatabaseName(candidate) &&
                        !ActiveFixtureDatabases.ContainsKey(candidate))
                    {
                        orphanCandidates.Add(candidate);
                    }
                }
            }

            foreach (var orphanDb in orphanCandidates)
            {
                if (!ActiveFixtureDatabases.ContainsKey(orphanDb))
                {
                    await TryDropSingleDatabaseAsync(conn, orphanDb);
                }
            }
        }
        catch
        {
            ActiveFixtureDatabases.TryRemove(fixtureDatabaseName, out _);
        }
    }

    private static bool IsSafeEphemeralTestDatabaseName(string? databaseName)
    {
        if (string.IsNullOrWhiteSpace(databaseName) ||
            string.Equals(databaseName, CanonicalDatabaseName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return EphemeralTestDatabasePrefixes.Any(prefix =>
            databaseName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task TryDropSingleDatabaseAsync(SqlConnection masterConnection, string databaseName)
    {
        if (!IsSafeEphemeralTestDatabaseName(databaseName))
        {
            return;
        }

        try
        {
            var escapedIdentifier = databaseName.Replace("]", "]]", StringComparison.Ordinal);
            await using var dropCmd = masterConnection.CreateCommand();
            dropCmd.CommandTimeout = 30;
            dropCmd.CommandText = $"""
                IF DB_ID(@dbName) IS NOT NULL
                BEGIN
                    ALTER DATABASE [{escapedIdentifier}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE [{escapedIdentifier}];
                END
                """;
            dropCmd.Parameters.AddWithValue("@dbName", databaseName);
            await dropCmd.ExecuteNonQueryAsync();
        }
        catch
        {
            // Ignore transient race if another fixture swept an orphan database concurrently.
        }
    }
}
