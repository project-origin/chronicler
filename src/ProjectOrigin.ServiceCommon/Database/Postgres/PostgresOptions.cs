using System.ComponentModel.DataAnnotations;

namespace ProjectOrigin.ServiceCommon.Database.Postgres;

public record PostgresOptions
{
    [Required(AllowEmptyStrings = false)]
    public required string ConnectionString { get; set; }
}
