using System;
using System.Collections.Generic;

namespace RevitCopyParams
{
    public class ChangeRecord
    {
        public ChangeRecord()
        {
            Id = Guid.NewGuid();
            CreatedDate = DateTime.Now;
            TargetModels = new List<string>();
        }

        public string Description { get; set; }

        public List<string> TargetModels { get; set; }

        public string TargetModelsText
        {
            get
            {
                return string.Join(", ", TargetModels);
            }
        }

        public DateTime CreatedDate { get; set; }

        public string Author { get; set; }

        public string SourceModel { get; set; }

        public Guid Id { get; set; }


        public ChangeStatus Status { get; set; }


    }
}