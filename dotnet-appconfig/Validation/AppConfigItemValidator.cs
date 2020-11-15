using ConfigManager.Models;
using FluentValidation;

public class AppConfigItemValidator : AbstractValidator<AppConfigItem>
{
    public AppConfigItemValidator()
    {
        RuleFor(p => p.Key)
            .NotNull()
            .NotEmpty()
            .Must(p => p != null && !p.Contains("%") && p != "." && p != "..");

        RuleFor(p => p.Value)
            .NotNull()
            .NotEmpty()
            .Matches("^[a-zA-Z0-9-]*$").When(p => p.KeyVault)
            .WithMessage(item => $"KeyVault Reference on '{item.Key}' is not in the correct format. {{PropertyName}}='{{PropertyValue}}'");

        RuleFor(p => p.Label)
            .Null().When(p => !string.IsNullOrEmpty(p.Environment) || !string.IsNullOrEmpty(p.Application));

        RuleFor(p => p.Application)
            .Matches("^[a-zA-Z0-9-_]*$");

        RuleFor(p => p.Environment)
            .Matches("^[a-zA-Z0-9-_]*$");
    }
}
