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
        public int Id { get; set; }

        public string Title { get; set; }
        public int? Priority { get; set; }

        public string License { get; set; }
        public string VersionSource { get; set; }

        [ForeignKey("Language")]
        public int? LanguageId { get; set; }
        public  Language Language { get; set; }

        [ForeignKey("LabelGroup")]
        public int VersionTitleId { get; set; }
        public LabelGroup VersionTitle { get; set; }
        public string VersionNotes { get; set; }

    }
}
