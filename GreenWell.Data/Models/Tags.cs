using System;
using System.Collections.Generic;

namespace Greenwell.Data.Models
{
    public partial class Tags
    {
        public Tags()
        {
            Tagmap = new HashSet<Tagmap>();
        }

        public string TagName { get; set; }
        public int TagId { get; set; }

        public ICollection<Tagmap> Tagmap { get; set; }
    }
}
