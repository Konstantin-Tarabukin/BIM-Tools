using System;

namespace RevitCopyParams.Models
{
    public class UpdateInfo
    {
        public Version CurrentVersion { get; set; }

        public Version LatestVersion { get; set; }

        public string DownloadUrl { get; set; }



        public bool UpdateAvailable
        {
            get
            {
                if (CurrentVersion == null || LatestVersion == null)
                    return false;

                return LatestVersion > CurrentVersion;
            }
        }
    }
}