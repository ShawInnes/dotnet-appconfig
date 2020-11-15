using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Azure.Data.AppConfiguration;
using Azure.Security.KeyVault.Secrets;
using ConfigManager.Models;
using ConfigManager.Services;
using FluentAssertions;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace ConfigManager.Tests
{
    public class AppConfigTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public AppConfigTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

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
            AppConfigService.IsValidJson(json).Should().Be(expected);
        }

        [Theory]
        [InlineData("{\"k\":\"v\"")]
        [InlineData("{k}")]
        [InlineData("{k:v}")]
        public void InvalidJson(string json)
        {
            var (_, errors) = AppConfigService.ReadAppConfigItems(json);

            errors.Count.Should().Be(1);
        }

        [Theory]
        [InlineData(null, null, null, null)]
        [InlineData("SomeRandomString", "SomeRandomString", null, null)]
        [InlineData("Application:MyApplication", null, null, "MyApplication")]
        [InlineData("Environment:Production", null, "Production", null)]
        [InlineData("Environment:Production/Application:MyApplication", null, "Production", "MyApplication")]
        public void LabelToConfigItem(string label, string expectedLabel, string expectedEnvironment,
            string expectedApplication)
        {
            var appConfigItem = new AppConfigItem();
            appConfigItem = AppConfigService.LabelToConfigItem(appConfigItem, label);

            appConfigItem.Label.Should().Be(expectedLabel);
            appConfigItem.Environment.Should().Be(expectedEnvironment);
            appConfigItem.Application.Should().Be(expectedApplication);
        }

        [Theory]
        [InlineData(null, null, null, null)]
        [InlineData("SomeRandomString", null, null, "SomeRandomString")]
        [InlineData(null, null, "MyApplication", "Application:MyApplication")]
        [InlineData(null, "Production", null, "Environment:Production")]
        [InlineData(null, "Production", "MyApplication", "Environment:Production/Application:MyApplication")]
        public void ConfigItemToLabel(string label, string environment, string application, string expectedLabel)
        {
            var appConfigItem = new AppConfigItem
            {
                Label = label,
                Environment = environment,
                Application = application
            };

            AppConfigService.ConfigItemToLabel(appConfigItem).Should().Be(expectedLabel);
        }
    }
}