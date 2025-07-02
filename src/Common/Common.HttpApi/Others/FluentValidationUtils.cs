using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Common.HttpApi.Others;

public static class FluentValidationUtils
{
    public static void AddToModelState(this ValidationResult result, ModelStateDictionary modelState)
    {
        if (result.IsValid)
        {
            return;
        }

        foreach (var error in result.Errors)
        {
            modelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }
    }

    public static IEnumerable<Dictionary<string, object>> MapErrors(IEnumerable<ValidationFailure> errors)
    {
        return errors.Select(error =>
        {
            var errorInfo = new Dictionary<string, object>
            {
                { "propertyName", error.FormattedMessagePlaceholderValues["PropertyName"] },
                { "errorMessage", error.ErrorMessage },
                { "attemptedValue", error.AttemptedValue }
            };

            if (error.FormattedMessagePlaceholderValues.TryGetValue("CollectionIndex", out var index))
            {
                errorInfo["collectionIndex"] = index;
            }

            return errorInfo;
        });
    }
}