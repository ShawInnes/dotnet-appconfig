using System.Collections.Generic;
using ConfigManager.Models;
using FluentValidation;

public class AppConfigItemListValidator : AbstractValidator<List<AppConfigItem>>
{
    public AppConfigItemListValidator()
    {
        RuleForEach(p => p).SetValidator(new AppConfigItemValidator());
    }
}
