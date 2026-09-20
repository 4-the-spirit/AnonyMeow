using AnonyMeow.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace AnonyMeow.IntegrationTests.Fixtures;

public class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("anonymeow_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(ConnectionString);
        await using var dbContext = new AppDbContext(optionsBuilder.Options);
        await dbContext.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
