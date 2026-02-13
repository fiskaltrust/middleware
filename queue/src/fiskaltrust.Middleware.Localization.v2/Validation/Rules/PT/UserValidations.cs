using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.v2.Helpers;
using FluentValidation;

namespace fiskaltrust.Middleware.Localization.v2.Validation.Rules.PT;

public class UserValidations : AbstractValidator<ReceiptRequest>
{
    public UserValidations()
    {
        Include(new UserStructure());
    }

    public class UserStructure : AbstractValidator<ReceiptRequest>
    {
        public UserStructure()
        {
            RuleFor(x => x)
                .Custom((request, context) =>
                {
                    string? user;
                    try
                    {
                        user = request.GetcbUserOrNull();
                    }
                    catch (System.Text.Json.JsonException ex)
                    {
                        context.AddFailure("cbUser", $"cbUser format is invalid: {ex.Message}");
                        return;
                    }

                    if (string.IsNullOrEmpty(user) || user.Length < 3)
                    {
                        context.AddFailure("cbUser", "cbUser must be at least 3 characters long");
                    }
                });
        }
    }
}
