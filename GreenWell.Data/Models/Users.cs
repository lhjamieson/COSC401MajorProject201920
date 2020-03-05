using System;
using System.Collections.Generic;

namespace Greenwell.Data.Models
{
    public partial class Users
    {
        public Users()
        {
            Files = new HashSet<Files>();
        }

        public string UserName { get; set; }
        public int UserId { get; set; }
        public string UserRole { get; set; }

        public ICollection<Files> Files { get; set; }
    }
}
