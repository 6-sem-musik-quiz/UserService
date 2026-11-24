using UserService.Domain.DTOs;
using UserService.Domain.Entities;

namespace UserService.Service.Mappings;

public static class UserMappings
{
    public static UserDto ToDto(this User user) => new(user.Id, user.UserName, user.Role);

    public static UserSummaryDto ToSummaryDto(this User user) => new(user.Id, user.UserName);
}
