using Autodesk.Revit.DB;
using System;
using System.IO;

namespace RevitCopyParams.Services
{
    public static class JsonJournalPathService
    {
        public const string JournalDirectoryName = "_BIMToolsJournal";
        public const string EventsDirectoryName = "Events";
        public const string EnabledMarkerFileName = "journal.enabled";

        public static bool IsEnabled(
            Document document)
        {
            string journalRoot;
            string projectId;
            string modelId;

            return TryGetJournalRoot(
                document,
                out journalRoot,
                out projectId,
                out modelId);
        }

        public static bool TryGetJournalRoot(
            Document document,
            out string journalRoot,
            out string projectId,
            out string modelId)
        {
            journalRoot = null;
            projectId = null;
            modelId = null;

            if (document == null || document.IsLinked)
                return false;

            string userVisiblePath =
                GetUserVisiblePath(document);


            if (string.IsNullOrWhiteSpace(userVisiblePath))
                return false;

            if (userVisiblePath.StartsWith(
                    "RSN:",
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!Path.IsPathRooted(userVisiblePath))
                return false;

            string modelDirectory =
                GetModelDirectory(userVisiblePath);



            if (string.IsNullOrWhiteSpace(modelDirectory))
                return false;

            string projectDirectory =
                FindEnabledProjectDirectory(modelDirectory);



            if (string.IsNullOrWhiteSpace(projectDirectory))
                return false;

            projectId =
                new DirectoryInfo(projectDirectory).Name;

            if (string.IsNullOrWhiteSpace(projectId))
                return false;

            journalRoot = Path.Combine(
                projectDirectory,
                JournalDirectoryName,
                EventsDirectoryName);



            modelId =
                RevitLinkService.GetModelName(document);

            if (string.IsNullOrWhiteSpace(modelId))
                return false;

            return true;
        }

        public static DateTime GetEnabledSinceUtc(
            string journalRoot)
        {
            if (string.IsNullOrWhiteSpace(journalRoot))
                return DateTime.MinValue;

            try
            {
                DirectoryInfo journalDirectory =
                    new DirectoryInfo(journalRoot);

                DirectoryInfo bimToolsJournalDirectory =
                    journalDirectory.Parent;

                DirectoryInfo projectDirectory =
                    bimToolsJournalDirectory;

                if (projectDirectory == null)
                    return DateTime.MinValue;

                string markerPath = Path.Combine(
                    projectDirectory.FullName,
                    EnabledMarkerFileName);

                if (!File.Exists(markerPath))
                    return DateTime.MinValue;

                return File.GetLastWriteTimeUtc(markerPath);
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        private static string FindEnabledProjectDirectory(
            string modelDirectory)
        {
            try
            {
                DirectoryInfo current =
                    new DirectoryInfo(modelDirectory);

                while (current != null)
                {
                    string markerPath = Path.Combine(
                        current.FullName,
                        JournalDirectoryName,
                        EnabledMarkerFileName);

                    if (File.Exists(markerPath))
                        return current.FullName;

                    current = current.Parent;
                }
            }
            catch
            {
            }

            return null;
        }

        private static string GetModelDirectory(
            string userVisiblePath)
        {
            try
            {
                string fullPath =
                    Path.GetFullPath(userVisiblePath);

                if (File.Exists(fullPath))
                    return Path.GetDirectoryName(fullPath);

                string extension =
                    Path.GetExtension(fullPath);

                if (!string.IsNullOrWhiteSpace(extension))
                    return Path.GetDirectoryName(fullPath);

                return fullPath;
            }
            catch
            {
                return null;
            }
        }

        private static string GetUserVisiblePath(
            Document document)
        {
            try
            {
                if (document.IsWorkshared)
                {
                    ModelPath centralPath =
                        document.GetWorksharingCentralModelPath();

                    if (centralPath == null)
                        return null;

                    string centralUserVisiblePath =
                        ModelPathUtils.ConvertModelPathToUserVisiblePath(
                            centralPath);

                    UpdateStartupService.LogDiagnostic(
                        "JSON CENTRAL DEBUG | " +
                        centralUserVisiblePath);

                    return centralUserVisiblePath;
                }

                return document.PathName;
            }
            catch
            {
                return null;
            }
        }

        public static string SanitizePathSegment(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Unknown";

            string result = value;

            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                result = result.Replace(
                    invalid,
                    '_');
            }

            return result.Trim();
        }
    }
}
