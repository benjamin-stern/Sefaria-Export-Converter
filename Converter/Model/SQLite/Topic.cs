using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Converter.Model.SQLite
{
    public class Topic
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; }

        public int? Index { get; set; }

        [ForeignKey("Topic")]
        public int? ParentTopicId { get; set; }
        public Topic ParentTopic { get; set; }
        public ICollection<Topic> Children { get; set; }

        [ForeignKey("LabelGroup")]
        public int? LabelGroupId { get; set; }
        public LabelGroup LabelGroup { get; set; }

    }
}
