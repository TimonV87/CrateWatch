using DbUp;
using Npgsql;

namespace CrateWatch.Db;

public static class DatabaseMigrator
{
    private const string BaselineScript = "CrateWatch.Db.Migration.V1__init.sql";

    public static void Migrate(string connectionString, ILogger logger)
    {
        var upgrader = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(typeof(DatabaseMigrator).Assembly, name => name.EndsWith(".sql"))
            .LogToNowhere()
            .Build();

        // A database created by the Java backend's Flyway already contains the V1 schema but has no
        // DbUp journal yet. Record V1 as applied instead of failing on "relation already exists".
        if (ExistsInDatabase(connectionString, "public.users") && !ExistsInDatabase(connectionString, "public.schemaversions"))
        {
            logger.LogInformation("Existing schema found without a DbUp journal; baselining {Script}", BaselineScript);
            upgrader.MarkAsExecuted(BaselineScript);
        }

        var result = upgrader.PerformUpgrade();
        if (!result.Successful)
        {
            throw new InvalidOperationException("Database migration failed", result.Error);
        }

        foreach (var script in result.Scripts)
        {
            logger.LogInformation("Applied migration {Script}", script.Name);
        }
    }

    private static bool ExistsInDatabase(string connectionString, string qualifiedName)
    {
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();
        using var command = new NpgsqlCommand("SELECT to_regclass(@name) IS NOT NULL", connection);
        command.Parameters.AddWithValue("name", qualifiedName);
        return (bool)command.ExecuteScalar()!;
    }
}
