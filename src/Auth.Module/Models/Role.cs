using Auth.Module.Models;

namespace Auth.Module.Models;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<AuthUser> Users { get; set; } = [];
}
