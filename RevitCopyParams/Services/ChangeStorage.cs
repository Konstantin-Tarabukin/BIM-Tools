using System.Collections.Generic;

namespace RevitCopyParams
{
    public static class ChangeStorage
    {
        private static readonly List<ChangeRecord> records =
            new List<ChangeRecord>();


        public static void Add(ChangeRecord record)
        {
            records.Add(record);
        }


        public static IReadOnlyList<ChangeRecord> GetAll()
        {
            return records;
        }
    }
}