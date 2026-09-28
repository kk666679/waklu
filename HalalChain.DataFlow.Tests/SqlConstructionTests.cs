using HalalChain.DataFlow.Configuration;
using HalalChain.DataFlow.Destination;
using HalalChain.DataFlow.Models;
using HalalChain.DataFlow.Source;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HalalChain.DataFlow.Tests;

/// <summary>
/// These tests exercise SQL construction and the guard rails around it
/// without needing a live PostgreSQL instance: the components build SQL
/// before they open a connection, and the connection is only opened at
/// execution time.
/// </summary>
public sealed class SqlConstructionTests
{
    private static readonly DataFlowConnectionOptions Connection = new()
    {
        Name = "halal-pg",
        Provider = DatabaseProvider.PostgreSQL,
        ConnectionString = "Host=localhost;Database=halalchain;Username=u;Password=p"
    };

    private static readonly DataFlowSourceOptions SourceOptions = new()
    {
        Name = "cert-source",
        ConnectionName = "halal-pg",
        EntityName = "Certificate",
        SchemaName = "halal",
        TableName = "certificates",
        BatchSize = 500
    };

    private static readonly DataFlowDestinationOptions DestinationOptions = new()
    {
        Name = "cert-dest",
        ConnectionName = "halal-pg",
        EntityName = "Certificate",
        SchemaName = "halal",
        TableName = "certificates",
        Columns = ["CertificateNumber", "IssuedAt", "Status"],
        KeyColumns = ["CertificateNumber"],
        SyncMode = SyncMode.Incremental
    };

    [Fact]
    public void Source_RejectsNonPostgresConnection()
    {
        var connection = new DataFlowConnectionOptions
        {
            Name = "sqlserver",
            Provider = DatabaseProvider.SqlServer,
            ConnectionString = "Server=x;Database=y"
        };

        // The mismatch is caught in the constructor so a misconfigured
        // pipeline fails at composition time, not on first extract.
        var ex = Assert.Throws<InvalidOperationException>(() =>
            new PostgresDataFlowSource<object>(
                SourceOptions, connection, NullLogger<PostgresDataFlowSource<object>>.Instance));

        Assert.Contains("PostgreSQL", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Destination_RejectsNonPostgresConnection()
    {
        var connection = new DataFlowConnectionOptions
        {
            Name = "sqlserver",
            Provider = DatabaseProvider.SqlServer,
            ConnectionString = "Server=x;Database=y"
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            new PostgresDataFlowDestination<Dictionary<string, object?>>(
                DestinationOptions, connection,
                NullLogger<PostgresDataFlowDestination<Dictionary<string, object?>>>.Instance));

        Assert.Contains("PostgreSQL", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DynamicSource_RejectsNonPostgresConnection()
    {
        var connection = new DataFlowConnectionOptions
        {
            Name = "sqlserver",
            Provider = DatabaseProvider.SqlServer,
            ConnectionString = "Server=x;Database=y"
        };

        Assert.Throws<InvalidOperationException>(() =>
            new DynamicPostgresDataFlowSource(
                SourceOptions, connection, NullLogger<DynamicPostgresDataFlowSource>.Instance));
    }

    [Fact]
    public void DynamicDestination_RejectsNonPostgresConnection()
    {
        var connection = new DataFlowConnectionOptions
        {
            Name = "sqlserver",
            Provider = DatabaseProvider.SqlServer,
            ConnectionString = "Server=x;Database=y"
        };

        Assert.Throws<InvalidOperationException>(() =>
            new DynamicPostgresDataFlowDestination(
                DestinationOptions, connection,
                NullLogger<DynamicPostgresDataFlowDestination>.Instance));
    }

    [Fact]
    public async Task Source_WithoutTableOrQuery_ThrowsOnUse()
    {
        var options = new DataFlowSourceOptions
        {
            Name = "bad-source",
            ConnectionName = "halal-pg",
            EntityName = "Certificate"
        };

        var source = new PostgresDataFlowSource<object>(
            options, Connection, NullLogger<PostgresDataFlowSource<object>>.Instance);

        // Count requires a resolvable relation and must fail loudly rather
        // than defaulting to a wrong table.
        await Assert.ThrowsAsync<InvalidOperationException>(() => source.GetEstimatedRecordCountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Source_RejectsQuotedIdentifier()
    {
        var options = new DataFlowSourceOptions
        {
            Name = "injection-source",
            ConnectionName = "halal-pg",
            EntityName = "Certificate",
            TableName = "cert\"; DROP TABLE certificates; --"
        };

        var source = new PostgresDataFlowSource<object>(
            options, Connection, NullLogger<PostgresDataFlowSource<object>>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => source.GetEstimatedRecordCountAsync(TestContext.Current.CancellationToken));

        Assert.Contains("double quote", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Destination_RejectsQuotedColumnIdentifier()
    {
        var options = new DataFlowDestinationOptions
        {
            Name = "injection-dest",
            ConnectionName = "halal-pg",
            EntityName = "Certificate",
            TableName = "certificates",
            Columns = ["CertificateNumber\"; DROP TABLE x; --"]
        };

        var destination = new PostgresDataFlowDestination<Dictionary<string, object?>>(
            options, Connection, NullLogger<PostgresDataFlowDestination<Dictionary<string, object?>>>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => destination.InitializeAsync(TestContext.Current.CancellationToken));
        Assert.Contains("double quote", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Destination_WithoutColumns_Throws()
    {
        var options = new DataFlowDestinationOptions
        {
            Name = "no-columns",
            ConnectionName = "halal-pg",
            EntityName = "Certificate",
            TableName = "certificates"
        };

        var destination = new PostgresDataFlowDestination<Dictionary<string, object?>>(
            options, Connection, NullLogger<PostgresDataFlowDestination<Dictionary<string, object?>>>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => destination.InitializeAsync(TestContext.Current.CancellationToken));
        Assert.Contains("at least one column", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Destination_WhenAllColumnsIgnored_Throws()
    {
        var options = new DataFlowDestinationOptions
        {
            Name = "all-ignored",
            ConnectionName = "halal-pg",
            EntityName = "Certificate",
            TableName = "certificates",
            Columns = ["CertificateNumber"],
            IgnoreColumns = ["CertificateNumber"]
        };

        var destination = new PostgresDataFlowDestination<Dictionary<string, object?>>(
            options, Connection, NullLogger<PostgresDataFlowDestination<Dictionary<string, object?>>>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => destination.InitializeAsync(TestContext.Current.CancellationToken));
        Assert.Contains("nothing to write", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Destination_WithoutTableName_ThrowsOnUse()
    {
        var options = new DataFlowDestinationOptions
        {
            Name = "no-table",
            ConnectionName = "halal-pg",
            EntityName = "Certificate",
            Columns = ["CertificateNumber"]
        };

        var destination = new PostgresDataFlowDestination<Dictionary<string, object?>>(
            options, Connection, NullLogger<PostgresDataFlowDestination<Dictionary<string, object?>>>.Instance);

        await destination.InitializeAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => destination.GetExistingRecordCountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public void SourceFactory_UnknownConnection_ThrowsWithKnownNames()
    {
        var factory = new PostgresDataFlowSourceFactory(
            new Dictionary<string, DataFlowConnectionOptions> { ["halal-pg"] = Connection },
            NullLoggerFactory.Instance);

        var options = new DataFlowSourceOptions
        {
            Name = "orphan-source",
            ConnectionName = "missing",
            EntityName = "Certificate",
            TableName = "certificates"
        };

        var ex = Assert.Throws<InvalidOperationException>(() => factory.CreateSource<object>(options));
        Assert.Contains("halal-pg", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DestinationFactory_UnknownConnection_ThrowsWithKnownNames()
    {
        var factory = new PostgresDataFlowDestinationFactory(
            new Dictionary<string, DataFlowConnectionOptions> { ["halal-pg"] = Connection },
            NullLoggerFactory.Instance);

        var options = new DataFlowDestinationOptions
        {
            Name = "orphan-dest",
            ConnectionName = "missing",
            EntityName = "Certificate",
            TableName = "certificates",
            Columns = ["CertificateNumber"]
        };

        var ex = Assert.Throws<InvalidOperationException>(
            () => factory.CreateDestination<Dictionary<string, object?>>(options));
        Assert.Contains("halal-pg", ex.Message, StringComparison.Ordinal);
    }
}
