using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using FluentAssertions;
using ProjectOrigin.Chronicler.Server.Options;
using ProjectOrigin.HierarchicalDeterministicKeys;
using Xunit;

namespace ProjectOrigin.Chronicler.Test
{
    public class ChroniclerOptionsTests
    {
        [Fact]
        public void ChroniclerOptions_EmptyFilename()
        {
            // Arrange
            var options = new ChroniclerOptions()
            {
                SigningKeyFilename = string.Empty,
                JobInterval = TimeSpan.FromMinutes(1)
            };

            // Act
            var validationResults = new List<ValidationResult>();
            var result = Validator.TryValidateObject(options, new ValidationContext(options), validationResults, true);

            // Assert
            result.Should().BeFalse();
            validationResults.Should().ContainEquivalentOf(new ValidationResult("The SigningKeyFilename field is required.", new[] { nameof(ChroniclerOptions.SigningKeyFilename) }));
        }

        [Fact]
        public void ChroniclerOptions_FileNotFound()
        {
            // Arrange
            var options = new ChroniclerOptions()
            {
                SigningKeyFilename = "example.key",
                JobInterval = TimeSpan.FromMinutes(1)
            };

            // Act
            var validationResults = new List<ValidationResult>();
            var result = Validator.TryValidateObject(options, new ValidationContext(options), validationResults, true);

            // Assert
            result.Should().BeFalse();
            validationResults.Should().ContainEquivalentOf(new ValidationResult("Signing key file does not exist", new[] { nameof(ChroniclerOptions.SigningKeyFilename) }));
        }

        [Fact]
        public void ChroniclerOptions_InvalidFormat()
        {
            // Arrange
            var filename = Path.GetTempFileName();
            var options = new ChroniclerOptions()
            {
                SigningKeyFilename = filename,
                JobInterval = TimeSpan.FromMinutes(1)
            };
            var data = "invalid data";
            File.WriteAllText(filename, data);

            // Act
            var validationResults = new List<ValidationResult>();
            var result = Validator.TryValidateObject(options, new ValidationContext(options), validationResults, true);

            // Assert
            result.Should().BeFalse();
            validationResults.Should().ContainEquivalentOf(new ValidationResult("Signing key file is not a valid private key", new[] { nameof(ChroniclerOptions.SigningKeyFilename) }));
        }

        [Fact]
        public void ChroniclerOptions_Valid()
        {
            // Arrange
            var key = Algorithms.Ed25519.GenerateNewPrivateKey();
            var filename = Path.GetTempFileName();
            var options = new ChroniclerOptions()
            {
                SigningKeyFilename = filename,
                JobInterval = TimeSpan.FromMinutes(1)
            };
            var data = key.ExportPkixText();
            File.WriteAllText(filename, data);

            // Act
            var validationResults = new List<ValidationResult>();
            var result = Validator.TryValidateObject(options, new ValidationContext(options), validationResults, true);

            // Assert
            result.Should().BeTrue();
            validationResults.Should().BeEmpty();
            options.GetPrivateKey().Should().BeEquivalentTo(key);
        }
    }
}
