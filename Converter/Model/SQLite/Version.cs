using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Converter.Model.SQLite
{
    public class Version
    {
        [Key]
        public int Id { get; set; }
        public int Major { get; set; }
        public int Minor { get; set; }
        public int Build { get; set; }

        override public string ToString() {
            return Major + "." + Minor + "." + Build;
        }
    }
}
