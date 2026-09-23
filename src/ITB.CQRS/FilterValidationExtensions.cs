using FluentValidation;
using ITB.CQRS.Models;

namespace ITB.CQRS;

public static class FilterValidationExtensions
{
    // A sort path the handler does not understand is rejected with a 422 that lists the valid columns,
    // instead of quietly falling back to some other ordering. Sending no sort at all is still valid.
    public static IRuleBuilderOptions<T, string> MustBeValidSortPath<T>(
        this IRuleBuilder<T, string> ruleBuilder,
        params string[] validSortPaths)
        where T : FilterBase
        => ruleBuilder
            .Must(path => validSortPaths.Any(valid => string.Equals(valid, path.Trim(), StringComparison.Ordinal)))
            .When(q => !string.IsNullOrWhiteSpace(q.Sorting?.Path))
            .WithMessage(q => $"Invalid sorting path '{q.Sorting?.Path}'. Valid values are: {string.Join(", ", validSortPaths)}.");
}
