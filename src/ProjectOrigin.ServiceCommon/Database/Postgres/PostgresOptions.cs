using System.ComponentModel.DataAnnotations;

namespace ProjectOrigin.ServiceCommon.Database.Postgres;

public sealed class PostgresOptions
{
    [Required]
    public required string ConnectionString { get; set; }
}
