using System.ComponentModel.DataAnnotations;

namespace ProjectOrigin.ServiceCommon.Otlp;

public record OtlpOptions() : IValidatableObject
{
    public const string Prefix = "Otlp";

    public Uri? Endpoint { get; init; }

    public required bool Enabled { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Enabled)
        {
            if (Endpoint == null)
            {
                yield return new ValidationResult(
                    $"The {nameof(Endpoint)} field is required when telemetry is enabled.",
                    [nameof(Endpoint)]);
            }
            else if (Endpoint.Scheme != Uri.UriSchemeHttp && Endpoint.Scheme != Uri.UriSchemeHttps)
            {
                yield return new ValidationResult(
                    $"The {nameof(Endpoint)} must use the HTTP or HTTPS scheme.",
                    [nameof(Endpoint)]);
            }
        }
    }
}
