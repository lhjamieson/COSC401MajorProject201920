using System;
using System.Collections.Generic;

namespace Greenwell.Data.Models
{
    public partial class Tagmap
    {
        public int Id { get; set; }
        public int FileId { get; set; }
        public int TagId { get; set; }

        public Files File { get; set; }
        public Tags Tag { get; set; }
    }
}
