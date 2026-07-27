using System;

namespace RevitCopyParams
{
    public class ChangeRecord
    {
        public string Description { get; set; }

        public string Disciplines { get; set; }

        public DateTime CreatedDate { get; set; }

        public string Author { get; set; }
    }
}