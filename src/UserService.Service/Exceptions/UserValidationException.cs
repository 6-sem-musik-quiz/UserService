namespace UserService.Service.Exceptions;

// Input failed business validation. Errors are keyed by JSON field name ("userName", "password").
public class UserValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("One or more validation errors occurred.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
