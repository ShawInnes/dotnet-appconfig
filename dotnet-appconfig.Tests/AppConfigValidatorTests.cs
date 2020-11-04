using ConfigManager.Models;
using FluentAssertions;
using Xunit;

namespace ConfigManager.Tests
{
    public class AppConfigValidatorTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(".")]
        [InlineData("..")]
        [InlineData("Key%Percent")]
        public void InvalidKeyTests(string key)
        {
            var validator = new AppConfigItemValidator();

            var result = validator.Validate(new AppConfigItem
            {
                Key = key
            });

            result.IsValid.Should().BeFalse();
        }

        [Theory]
        [InlineData("Key.SubKey")]
        [InlineData("Key..SubKey")]
        [InlineData("Key:Colon")]
        [InlineData("Key_Underscore")]
        [InlineData("Key__DoubleUnderscore")]
        public void ValidKeyTests(string key)
        {
            var validator = new AppConfigItemValidator();

            var result = validator.Validate(new AppConfigItem
            {
                Key = key,
                Value = "value"
            });

            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("testvalue", true)]
        [InlineData("test-value", true)]
        [InlineData("test_value", false)]
        [InlineData("test:value", false)]
        [InlineData("test.value", false)]
        public void KeyVaultValueTests(string value, bool expected)
        {
            var validator = new AppConfigItemValidator();

            var result = validator.Validate(new AppConfigItem
            {
                KeyVault = true,
                Key = "valid-key",
                Value = value
            });

            result.IsValid.Should().Be(expected);
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData("testvalue", true)]
        [InlineData("test-value", true)]
        [InlineData("test_value", true)]
        [InlineData("test:value", true)]
        [InlineData("test.value", true)]
        public void NonKeyVaultValueTests(string value, bool expected)
        {
            var validator = new AppConfigItemValidator();

            var result = validator.Validate(new AppConfigItem
            {
                KeyVault = false,
                Key = "valid-key",
                Value = value
            });

            result.IsValid.Should().Be(expected);
        }
    }
}
