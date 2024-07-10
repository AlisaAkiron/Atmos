namespace Atmos.AppHost.Model;

public record ComponentVersion
{
    public string PostgreSqlTag { get; set; } = "16.3";

    public string RedisTag { get; set; } = "7.2.5-alpine";

    public string PgAdminTag { get; set; } = "8.9";
}
