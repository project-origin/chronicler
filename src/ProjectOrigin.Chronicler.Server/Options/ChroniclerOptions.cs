using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ProjectOrigin.HierarchicalDeterministicKeys;
using ProjectOrigin.HierarchicalDeterministicKeys.Interfaces;

namespace ProjectOrigin.Chronicler.Server.Options;

public record ChroniclerOptions : IValidatableObject
{
    public const string SectionPrefix = "chronicler";

    public required string SigningKeyFilename { get; init; }

    private Lazy<IPrivateKey> _privateKey;

    public IPrivateKey GetPrivateKey() => _privateKey.Value;

    public ChroniclerOptions()
    {
        _privateKey = new Lazy<IPrivateKey>(() =>
        {
            using (var file = System.IO.File.OpenText(SigningKeyFilename!))
            {
                return Algorithms.Ed25519.ImportPrivateKeyText(file.ReadToEnd());
            }
        });
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var validationResults = new List<ValidationResult>();

        if (string.IsNullOrWhiteSpace(SigningKeyFilename))
        {
            validationResults.Add(new ValidationResult("The SigningKeyFilename field is required.", new[] { nameof(SigningKeyFilename) }));
        }
        else if (!System.IO.File.Exists(SigningKeyFilename))
        {
            validationResults.Add(new ValidationResult("Signing key file does not exist", new[] { nameof(SigningKeyFilename) }));
        }
        else
        {
            try
            {
                GetPrivateKey();
            }
            catch
            {
                validationResults.Add(new ValidationResult("Signing key file is not a valid private key", new[] { nameof(SigningKeyFilename) }));
            }
        }

        return validationResults;
    }
}
