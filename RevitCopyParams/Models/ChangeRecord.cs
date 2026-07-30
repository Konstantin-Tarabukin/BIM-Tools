using System;

namespace RevitCopyParams
{
    public class ChangeRecord
    {
        public ChangeRecord()
        {
            Id = Guid.NewGuid();
            CreatedDate = DateTime.Now;
        }

        public string Description { get; set; }

        public string Disciplines { get; set; }

        public DateTime CreatedDate { get; set; }

        public string Author { get; set; }

        public Guid Id { get; set; }
    }
}