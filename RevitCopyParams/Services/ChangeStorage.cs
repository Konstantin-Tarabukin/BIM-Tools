using System.Collections.Generic;

namespace RevitCopyParams
{
    public static class ChangeStorage
    {
        private static List<ChangeRecord> records =
            new List<ChangeRecord>();


        public static void Add(ChangeRecord record)
        {
            records.Add(record);
        }


        public static List<ChangeRecord> GetAll()
        {
            return records;
        }
    }
}