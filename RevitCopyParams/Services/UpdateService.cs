using Newtonsoft.Json.Linq;
using RevitCopyParams.Config;
using RevitCopyParams.Models;
using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace RevitCopyParams.Services
{
    public static class UpdateService
    {
        public static Version GetPackageVersion(
            string updatePath)
        {
            if (string.IsNullOrEmpty(updatePath))
            {
                throw new ArgumentException(
                    "Путь к пакету обновления не указан.",
                    nameof(updatePath));
            }

            if (!File.Exists(updatePath))
            {
                throw new FileNotFoundException(
                    "Пакет обновления не найден.",
                    updatePath);
            }

            using (ZipArchive archive =
                ZipFile.OpenRead(updatePath))
            {
                ZipArchiveEntry infoEntry =
                    archive.GetEntry("UpdateInfo.json");

                if (infoEntry == null)
                {
                    throw new Exception(
                        "В пакете обновления отсутствует UpdateInfo.json.");
                }

                using (StreamReader reader =
                    new StreamReader(infoEntry.Open()))
                {
                    string json =
                        reader.ReadToEnd();

                    JObject updateInfo =
                        JObject.Parse(json);

                    string versionText =
                        updateInfo["version"]?.ToString();

                    if (string.IsNullOrWhiteSpace(versionText))
                    {
                        throw new Exception(
                            "UpdateInfo.json не содержит версию пакета.");
                    }

                    Version packageVersion;

                    if (!Version.TryParse(
                        versionText,
                        out packageVersion))
                    {
                        throw new Exception(
                            "Некорректная версия в UpdateInfo.json: " +
                            versionText);
                    }

                    return packageVersion;
                }
            }
        }

        public static void ValidateUpdatePackage(
            string updatePath,
            Version currentVersion)
        {
            Version packageVersion =
                GetPackageVersion(updatePath);

            if (packageVersion <= currentVersion)
            {
                throw new Exception(
                    "Пакет обновления не является более новой версией. " +
                    "Текущая версия: " +
                    currentVersion +
                    ", версия пакета: " +
                    packageVersion +
                    ".");
            }
        }

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
                    UpdateSettings.ReleasesUrl;

                HttpResponseMessage response =
                    await client.GetAsync(url);

                response.EnsureSuccessStatusCode();

                string json =
                    await response.Content.ReadAsStringAsync();

                JArray releases =
                    JArray.Parse(json);

                JObject selectedRelease = null;
                Version selectedVersion = null;

                foreach (JObject release in releases)
                {
                    bool isDraft =
                        release["draft"]?.ToObject<bool>() ?? false;

                    bool isPrerelease =
                        release["prerelease"]?.ToObject<bool>() ?? false;

                    if (isDraft || isPrerelease)
                    {
                        continue;
                    }

                    string tagName =
                        release["tag_name"]?.ToString();

                    if (string.IsNullOrEmpty(tagName))
                    {
                        continue;
                    }

                    string versionText =
                        tagName.TrimStart('v', 'V');

                    int dashIndex =
                        versionText.IndexOf('-');

                    if (dashIndex >= 0)
                    {
                        versionText =
                            versionText.Substring(
                                0,
                                dashIndex);
                    }

                    Version releaseVersion;

                    if (!Version.TryParse(
                        versionText,
                        out releaseVersion))
                    {
                        continue;
                    }

                    if (selectedVersion == null ||
                        releaseVersion > selectedVersion)
                    {
                        selectedVersion =
                            releaseVersion;

                        selectedRelease =
                            release;
                    }
                }

                if (selectedRelease == null ||
                    selectedVersion == null)
                {
                    throw new Exception(
                        "Не найден подходящий стабильный GitHub Release.");
                }

                string downloadUrl = null;

                JArray assets =
                    selectedRelease["assets"] as JArray;

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
                        selectedVersion,

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
                    BIMToolsPaths.UpdatesDirectory;

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

                /*
                 * Проверяем, существует ли уже пакет
                 * требуемой версии.
                 */
                if (File.Exists(updatePath))
                {
                    Console.WriteLine(
                        "Пакет обновления уже существует:");

                    Console.WriteLine(
                        updatePath);

                    try
                    {
                        Version existingPackageVersion =
                            GetPackageVersion(updatePath);

                        Console.WriteLine(
                            "Версия существующего пакета: " +
                            existingPackageVersion);

                        Console.WriteLine(
                            "Требуемая версия: " +
                            updateInfo.LatestVersion);

                        if (existingPackageVersion ==
                            updateInfo.LatestVersion)
                        {
                            Console.WriteLine(
                                "Существующий пакет соответствует требуемой версии.");

                            Console.WriteLine(
                                "Повторное скачивание не требуется.");

                            return updatePath;
                        }

                        Console.WriteLine(
                            "Существующий пакет имеет другую версию.");

                        Console.WriteLine(
                            "Пакет будет удалён и скачан заново.");

                        File.Delete(
                            updatePath);
                    }
                    catch (Exception packageException)
                    {
                        Console.WriteLine(
                            "Не удалось проверить существующий пакет:");

                        Console.WriteLine(
                            packageException.ToString());

                        Console.WriteLine(
                            "Пакет будет удалён и скачан заново.");

                        try
                        {
                            File.Delete(
                                updatePath);
                        }
                        catch (Exception deleteException)
                        {
                            throw new Exception(
                                "Не удалось удалить повреждённый " +
                                "или некорректный пакет обновления.",
                                deleteException);
                        }
                    }
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

                /*
                 * После скачивания дополнительно проверяем,
                 * что пакет действительно содержит ожидаемую версию.
                 */
                try
                {
                    Version downloadedPackageVersion =
                        GetPackageVersion(updatePath);

                    if (downloadedPackageVersion !=
                        updateInfo.LatestVersion)
                    {
                        File.Delete(
                            updatePath);

                        throw new Exception(
                            "Скачанный пакет имеет неправильную версию. " +
                            "Ожидалась: " +
                            updateInfo.LatestVersion +
                            ", получена: " +
                            downloadedPackageVersion +
                            ".");
                    }
                }
                catch
                {
                    if (File.Exists(updatePath))
                    {
                        File.Delete(
                            updatePath);
                    }

                    throw;
                }

                return updatePath;
            }
        }

    }
}

