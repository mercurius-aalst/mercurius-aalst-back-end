using System.Collections.Generic;

namespace MercuriusAPI.Models.Auth
{
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
