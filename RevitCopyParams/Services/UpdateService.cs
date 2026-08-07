using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitCopyParams.Config;
using RevitCopyParams.Models;
using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace RevitCopyParams.Services
{
    public static class UpdateService
    {
        public static Version GetCurrentVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            return assembly.GetName().Version;
        }

        public static UpdateInfo GetUpdateInfo()
        {
            return new UpdateInfo
            {
                CurrentVersion = GetCurrentVersion()
            };
        }

        public static async Task<UpdateInfo> GetLatestRelease()
        {


            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(10);


                client.DefaultRequestHeaders.Add(
                    "User-Agent",
                    "BIM-Tools-Updater");



                string url = UpdateSettings.ReleasesUrl;



                HttpResponseMessage response = await client.GetAsync(url);



                string json = await response.Content.ReadAsStringAsync();

                System.IO.File.WriteAllText(
    @"C:\Temp\GitHubResponse.json",
    json);



                JArray releases = JArray.Parse(json);

                JObject release = (JObject)releases[0];



                string tagName = release["tag_name"]?.ToString();

                if (string.IsNullOrEmpty(tagName))
                {
                    throw new Exception(
                        "GitHub Release не содержит tag_name");
                }



                string versionText = tagName.TrimStart('v');

                int dashIndex = versionText.IndexOf('-');

                if (dashIndex >= 0)
                {
                    versionText = versionText.Substring(0, dashIndex);
                }

                Version latestVersion = new Version(versionText);



                return new UpdateInfo
                {
                    CurrentVersion = GetCurrentVersion(),
                    LatestVersion = latestVersion
                };
            }
        }
    }
}