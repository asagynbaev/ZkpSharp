using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Tessera.EntityFrameworkCore.Tests;

/// <summary>
/// Builds a fresh, isolated SQLite in-memory database per test. Each call to
/// <see cref="CreateContext"/> returns a new DbContext sharing the same underlying
/// connection (so the schema and data persist across DbContext instances within
/// the same fixture, but not across fixtures).
/// </summary>
/// <remarks>
/// Keeping the connection open is critical: SQLite drops in-memory databases when
/// the last connection closes, so we hold one as long as the fixture lives.
/// </remarks>
internal sealed class SqliteFixture : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<TesseraDbContext> _options;

    public SqliteFixture()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<TesseraDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var db = new TesseraDbContext(_options);
        db.Database.EnsureCreated();
    }

    public TesseraDbContext CreateContext() => new(_options);

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }
}
