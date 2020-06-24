using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Converter.Model.SQLite
{
    public class LinkItem
    {
        [Key]
        public int Id { get; set; }
        [ForeignKey("LinkGroup")]
        public int LinkGroupId { get; set; }
        public LinkGroup LinkGroup { get; set; }
        public string PrimaryLocation { get; set; }
        public string SecondaryLocation { get; set; }
    }

    public class LinkGroup {
        public LinkGroup() {
            LinkedLanguages = new List<LinkLanguage>();
        }

        public int Id { get; set; }
        [ForeignKey("Topic")]
        public int PrimaryTopicId { get; set; }
        [ForeignKey("Topic")]
        public int SecondaryTopicId { get; set; }

        public ICollection<LinkLanguage> LinkedLanguages { get; set; }
    }

    public class LinkLanguage { 
        public int Id { get; set; }
        [ForeignKey("Language")]
        public int LanguageId { get; set; }
        [ForeignKey("LinkGroup")]
        public int LinkGroupId { get; set; }
        public LinkGroup LinkGroup { get; set; }
        [ForeignKey("Topic")]
        public int TopicId { get; set; }

        public int Count { get; set; }
    }
}
