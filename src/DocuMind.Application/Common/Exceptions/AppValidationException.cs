namespace DocuMind.Application.Common.Exceptions;

/// <summary>
/// Thrown when input fails business-rule validation (as opposed to model binding/data annotation
/// validation, which the API layer handles separately).
/// </summary>
public class AppValidationException(IReadOnlyList<string> errors)
    : Exception(string.Join("; ", errors))
{
    public IReadOnlyList<string> Errors { get; } = errors;
}
