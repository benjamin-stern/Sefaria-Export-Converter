using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Converter.Model.SQLite
{
    public class Version
    {
        public int Major { get; set; }
        public int Minor { get; set; }
        [Key]
        public int Build { get; set; }
    }
}
