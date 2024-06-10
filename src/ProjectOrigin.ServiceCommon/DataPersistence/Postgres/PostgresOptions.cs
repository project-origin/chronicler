using System.ComponentModel.DataAnnotations;

namespace ProjectOrigin.ServiceCommon.DataPersistence.Postgres;

public sealed class PostgresOptions
{
    [Required]
    public required string ConnectionString { get; set; }
}
