using System.Text.Json.Serialization;

namespace UserService.Domain.Entities;

// Stored as int in the database (Player = 0, Admin = 1); serialized as its name in JSON.
[JsonConverter(typeof(JsonStringEnumConverter<UserRole>))]
public enum UserRole
{
    Player = 0,
    Admin = 1
}
