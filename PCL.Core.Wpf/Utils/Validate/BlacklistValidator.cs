using System.Collections.Generic;
using System.Linq;
using FluentValidation;

namespace PCL.Core.Utils.Validate;

public class BlacklistValidator : AbstractValidator<string>
{
    public BlacklistValidator() : this([])
    {
    }

    public List<string> Blacklist { get; set; }

    public BlacklistValidator(List<string> contains)
    {
        Blacklist = contains;
        RuleFor(x => x)
            .Custom((input, context) =>
            {
                if (input is null) return;
                foreach (var items in Blacklist.Where(input.Contains))
                    context.AddFailure(ValidationMessage.Text("Common.Validate.Blacklisted", items));
            });
    }
}
