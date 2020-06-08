using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Converter.Model.SQLite
{
    public class Text
    {
        [Key]
        public int Id;

        public string Title;
        public int Priority;

        public string License;
        public string VersionSource;

        [ForeignKey("Language")]
        public int LanguageId;
        public Language Language;

        public string VersionString;

    }
}
