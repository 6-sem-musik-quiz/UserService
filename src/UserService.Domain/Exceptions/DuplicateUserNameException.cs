namespace UserService.Domain.Exceptions;

// Thrown when a user name is already taken (case-insensitive). Lives in Domain so both the
// service (pre-check) and the repository (unique-index violation on a race) can raise it.
public class DuplicateUserNameException(string userName, Exception? innerException = null)
    : Exception($"The user name '{userName}' is already taken.", innerException)
{
    public string UserName { get; } = userName;
}
