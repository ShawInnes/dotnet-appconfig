using System;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using ConfigManager.Services;
using FluentAssertions;
using McMaster.Extensions.CommandLineUtils;
using NSubstitute;
using Xunit;

namespace ConfigManager.Tests
{
    public class KeyVaultTests
    {
        [Fact]
        public void KeyVaultTest()
        {
            var credential = new DefaultAzureCredential();
            var secretClient = new Azure.Security.KeyVault.Secrets.SecretClient(new Uri("https://vaultname.azure.net"), credential);
            var response = secretClient.SetSecret(new KeyVaultSecret("name", "value")
            {
                Properties =
                {
                    ContentType = "text/secret",
                    Tags = {{"Source", "Keepass"}}
                }
            });
        }

        [Fact]
        public void FromKeyVaultReferenceTest()
        {
            var consoleMock = Substitute.For<IConsole>();
            var appConfigService = new AppConfigService(consoleMock);

            var value = "{\"uri\": \"https://vaultname.vault.azure.net/secrets/frontdoor-id\"}";
            var secretName = appConfigService.FromKeyVaultReference(value);

            secretName.Should().Be("frontdoor-id");
        }

        [Fact]
        public void ToKeyVaultReferenceTest()
        {
            var consoleMock = Substitute.For<IConsole>();
            var appConfigService = new AppConfigService(consoleMock);

            var keyVaultName = "vaultname";
            var value = "frontdoor-id";

            var keyVaultReference = appConfigService.ToKeyVaultReference(keyVaultName, value);

            keyVaultReference.Should().Be($"{{\"uri\": \"https://vaultname.vault.azure.net/secrets/frontdoor-id\"}}");
        }
    }
}
