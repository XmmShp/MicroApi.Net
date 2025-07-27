using System.ComponentModel.DataAnnotations;

namespace MicroAPI.Sample.Models;

public class User
{
    public int Id { get; set; }

    [MaxLength(100), MinLength(5)]
    public required string Name { get; set; } = string.Empty;
    [Range(0, 60, ErrorMessage = "Wrong Age")]
    public int Age { get; set; }
    public Address? Address { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public List<string>? Roles { get; set; } = [];
}

public class Address
{
    public string Province { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
}

[Dto(typeof(User))]
public partial class UserInfo
{
    public AddressDto? Address { get; set; }
}

[Dto<Address>]
public partial class AddressDto;