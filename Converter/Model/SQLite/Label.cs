using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Converter.Model.SQLite
{
    public class LabelGroup
    {
        [Key]
        public int Id { get; set; }

        public ICollection<Label> Labels { get; set; }
    }

    public class Label
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("LabelGroup")]
        public int LabelGroupId { get; set; }
        public LabelGroup Group { get; set; }

        [ForeignKey("Language")]
        public int LanguageId { get; set; }
        public Language Language { get; set; }

        public string Text { get; set; }
    }
}
