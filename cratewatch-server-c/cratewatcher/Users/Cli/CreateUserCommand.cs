using CrateWatch.Users.Domain;
using CrateWatch.Users.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CrateWatch.Users.Cli;

/// <summary>Usage: <c>dotnet cratewatcher.dll create-user &lt;username&gt;</c>. Accounts are only created this way; there is no registration endpoint.</summary>
public static class CreateUserCommand
{
    private const int MaxUsernameLength = 100;
    private const int MinPasswordLength = 8;

    public static async Task<int> RunAsync(IServiceProvider services, string username)
    {
        if (string.IsNullOrWhiteSpace(username) || username.Length > MaxUsernameLength)
        {
            return Fail($"Username must be 1-{MaxUsernameLength} characters.");
        }

        var password = ReadSecret($"Password for {username} (min {MinPasswordLength} characters): ");
        if (password.Length < MinPasswordLength)
        {
            return Fail($"Password must be at least {MinPasswordLength} characters.");
        }
        if (ReadSecret("Repeat password: ") != password)
        {
            return Fail("Passwords do not match.");
        }

        using var scope = services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserRepository>();
        try
        {
            await users.SaveAsync(new User
            {
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 10),
            });
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return Fail($"User '{username}' already exists.");
        }

        Console.WriteLine($"User '{username}' created.");
        return 0;
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 1;
    }

    // Hidden input on a terminal; plain lines when stdin is piped (e.g. printf 'pw\npw\n' | ... create-user tom).
    private static string ReadSecret(string prompt)
    {
        Console.Write(prompt);
        if (Console.IsInputRedirected)
        {
            return Console.ReadLine() ?? "";
        }

        var secret = new System.Text.StringBuilder();
        ConsoleKeyInfo key;
        while ((key = Console.ReadKey(intercept: true)).Key != ConsoleKey.Enter)
        {
            if (key.Key == ConsoleKey.Backspace)
            {
                if (secret.Length > 0)
                {
                    secret.Length--;
                }
            }
            else if (!char.IsControl(key.KeyChar))
            {
                secret.Append(key.KeyChar);
            }
        }
        Console.WriteLine();
        return secret.ToString();
    }
}
