using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Converter.Model.SQLite
{
    public enum LanguageTypes
    {
        Undefined = 0,
        English = 1,
        Hebrew = 2
    }
    public class Language
    {
        [Key]
        public int Id { get; set; }
        public string Value { get; set; }

        [NotMapped]
        public LanguageTypes Type
        {
            get
            {
                LanguageTypes val = LanguageTypes.Undefined;
                if (Id > 0 && Id <= 2)
                {
                    val = (LanguageTypes)Id;
                }
                return val;
            }
        }
    }
}
