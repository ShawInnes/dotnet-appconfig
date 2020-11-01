using System.Text.RegularExpressions;
using Azure.Data.AppConfiguration;
using Azure.Security.KeyVault.Secrets;
using ConfigManager.Services;
using FluentAssertions;
using McMaster.Extensions.CommandLineUtils;
using NSubstitute;
using Xunit;

namespace ConfigManager.Tests
{
    public class AppConfigTests
    {
        [Fact]
        public void FromConnectionString()
        {
            var connectionString =
                "Endpoint=https://app-config.azconfig.io;Id=xyz4-rg-s9:rgBiKka0wKZuEta/0Eta;Secret=fdahytbtF+fjdhfksdhfsdfc=";

            var consoleMock = Substitute.For<IConsole>();
            var configurationClientMock = Substitute.For<ConfigurationClient>();
            var secretClientMock = Substitute.For<SecretClient>();
            var appConfigService = new AppConfigService(consoleMock, configurationClientMock, secretClientMock);

            var (endpoint, name, id, secret) = appConfigService.SplitAppConfigConnectionString(connectionString);

            endpoint.Should().Be("https://app-config.azconfig.io");
            name.Should().Be("app-config");
            id.Should().Be("xyz4-rg-s9:rgBiKka0wKZuEta/0Eta");
            secret.Should().Be("fdahytbtF+fjdhfksdhfsdfc=");
        }

        [Theory]
        [InlineData("{}", true)]
        [InlineData("[]", true)]
        [InlineData("{\"k\":\"v\"}", true)]
        [InlineData("{\"k\":\"v\"", false)]
        [InlineData("{k}", false)]
        [InlineData("{k:v}", false)]
        [InlineData("{\"k\": true}", true)]
        [InlineData("{\"k\": {}}", true)]
        [InlineData("{\"k\": []}", true)]
        public void ValidJson(string json, bool expected)
        {
            var consoleMock = Substitute.For<IConsole>();
            var configurationClientMock = Substitute.For<ConfigurationClient>();
            var secretClientMock = Substitute.For<SecretClient>();
            var appConfigService = new AppConfigService(consoleMock, configurationClientMock, secretClientMock);

            appConfigService.IsValidJson(json).Should().Be(expected);
        }
    }
}