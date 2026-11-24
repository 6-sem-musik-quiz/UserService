namespace UserService.Service.Exceptions;

// Raised when login fails — same type for unknown user and wrong password,
// so callers cannot distinguish the two cases (prevents user enumeration).
public class InvalidCredentialsException()
    : Exception("Invalid user name or password.");
