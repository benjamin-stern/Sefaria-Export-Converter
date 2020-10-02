using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Text;

namespace Converter.Model.SQLite
{
    public class Chapter
    {
        [Key]
        public int Id { get; set; }
        [ForeignKey("Text")]
        public int TopicTextId { get; set; }
        public virtual Text TopicText { get; set; }

        public bool HasChild { get; set; }
        [ForeignKey("Chapter")]
        public int? ParentChapterId { get; set; }
        public Chapter ParentChapter { get; set; }
        public ICollection<Chapter> Children { get; set; }
        public int Index { get; set; }
        public string Path { get; set; }
        public string Text { get; set; }

        //TODO: Consider Adding a ChapterClonedText to reduce duplication...
    }
}
