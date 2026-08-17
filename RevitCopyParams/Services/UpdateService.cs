using Newtonsoft.Json.Linq;
using RevitCopyParams.Config;
using RevitCopyParams.Models;
using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace RevitCopyParams.Services
{
    public static class UpdateService
    {
        public static Version GetCurrentVersion()
        {
            Assembly assembly =
                Assembly.GetExecutingAssembly();

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
                client.Timeout =
                    TimeSpan.FromSeconds(10);

                client.DefaultRequestHeaders.Add(
                    "User-Agent",
                    "BIM-Tools-Updater");

                string url =
                    UpdateSettings.ReleasesUrl +
                    "/latest";

                HttpResponseMessage response =
                    await client.GetAsync(url);

                response.EnsureSuccessStatusCode();

                string json =
                    await response.Content.ReadAsStringAsync();

                JObject release =
                    JObject.Parse(json);

                string tagName =
                    release["tag_name"]?.ToString();

                if (string.IsNullOrEmpty(tagName))
                {
                    throw new Exception(
                        "GitHub Release не содержит tag_name.");
                }

                string versionText =
                    tagName.TrimStart('v');

                int dashIndex =
                    versionText.IndexOf('-');

                if (dashIndex >= 0)
                {
                    versionText =
                        versionText.Substring(
                            0,
                            dashIndex);
                }

                Version latestVersion =
                    new Version(versionText);

                string downloadUrl = null;

                JArray assets =
                    release["assets"] as JArray;

                if (assets != null)
                {
                    foreach (JObject asset in assets)
                    {
                        string assetName =
                            asset["name"]?.ToString();

                        string browserDownloadUrl =
                            asset["browser_download_url"]?.ToString();

                        if (string.IsNullOrEmpty(assetName) ||
                            string.IsNullOrEmpty(browserDownloadUrl))
                        {
                            continue;
                        }

                        if (assetName.EndsWith(
                            ".zip",
                            StringComparison.OrdinalIgnoreCase))
                        {
                            downloadUrl =
                                browserDownloadUrl;

                            break;
                        }
                    }
                }

                return new UpdateInfo
                {
                    CurrentVersion =
                        GetCurrentVersion(),

                    LatestVersion =
                        latestVersion,

                    DownloadUrl =
                        downloadUrl
                };
            }
        }

        public static async Task<string> DownloadUpdate(
            UpdateInfo updateInfo)
        {
            if (updateInfo == null)
            {
                throw new ArgumentNullException(
                    nameof(updateInfo));
            }

            if (string.IsNullOrEmpty(
                updateInfo.DownloadUrl))
            {
                throw new Exception(
                    "Для Release не найден ZIP-пакет обновления.");
            }

            using (HttpClient client = new HttpClient())
            {
                client.Timeout =
                    TimeSpan.FromMinutes(5);

                client.DefaultRequestHeaders.Add(
                    "User-Agent",
                    "BIM-Tools-Updater");

                string tempDirectory =
                    Path.Combine(
                        Path.GetTempPath(),
                        "BIMToolsUpdate");

                if (!Directory.Exists(tempDirectory))
                {
                    Directory.CreateDirectory(
                        tempDirectory);
                }

                string fileName =
                    "BIMToolsUpdate_" +
                    updateInfo.LatestVersion +
                    ".zip";

                string updatePath =
                    Path.Combine(
                        tempDirectory,
                        fileName);

                if (File.Exists(updatePath))
                {
                    File.Delete(updatePath);
                }

                Console.WriteLine(
                    "Скачивание обновления:");

                Console.WriteLine(
                    updateInfo.DownloadUrl);

                byte[] data =
                    await client.GetByteArrayAsync(
                        updateInfo.DownloadUrl);

                File.WriteAllBytes(
                    updatePath,
                    data);

                if (!File.Exists(updatePath))
                {
                    throw new Exception(
                        "Файл обновления не был создан.");
                }

                return updatePath;
            }
        }
    }
}