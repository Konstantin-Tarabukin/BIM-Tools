using Autodesk.Revit.DB;
using RevitCopyParams.Config;
using RevitCopyParams.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace RevitCopyParams
{
    public static class RevitLinkReloadService
    {
        public static void ReloadLoadedTopLevelLinks(
            Document document)
        {
            DateTime sessionStart =
                DateTime.Now;

            if (document == null)
            {
                Log(
                    "RELOAD SESSION ERROR | Document=null");

                return;
            }

            try
            {
                string hostModel =
                    RevitLinkService.GetModelName(document);

                Log(
                    "RELOAD SESSION START | Model=" +
                    hostModel);

                FilteredElementCollector collector =
                    new FilteredElementCollector(document);

                ICollection<Element> linkInstances =
                    collector
                        .OfClass(typeof(RevitLinkInstance))
                        .ToElements();

                HashSet<ElementId> linkTypeIds =
                    new HashSet<ElementId>();

                foreach (Element element in linkInstances)
                {
                    RevitLinkInstance linkInstance =
                        element as RevitLinkInstance;

                    if (linkInstance == null)
                        continue;

                    ElementId typeId =
                        linkInstance.GetTypeId();

                    if (typeId == null ||
                        typeId == ElementId.InvalidElementId)
                    {
                        continue;
                    }

                    linkTypeIds.Add(typeId);
                }

                Log(
                    "LINKS DISCOVERED | Model=" +
                    hostModel +
                    " | Instances=" +
                    linkInstances.Count +
                    " | UniqueTypes=" +
                    linkTypeIds.Count);

                int processed = 0;
                int reloaded = 0;
                int skipped = 0;
                int failed = 0;

                foreach (ElementId typeId in linkTypeIds)
                {
                    processed++;

                    RevitLinkType linkType =
                        document.GetElement(typeId)
                        as RevitLinkType;

                    if (linkType == null)
                    {
                        skipped++;

                        Log(
                            "LINK SKIP | Reason=TypeNotFound" +
                            " | TypeId=" +
                            typeId.IntegerValue);

                        continue;
                    }

                    string linkName;

                    try
                    {
                        linkName =
                            linkType.Name;
                    }
                    catch
                    {
                        linkName =
                            "<unknown>";
                    }

                    Log(
                        "LINK FOUND" +
                        " | Host=" +
                        hostModel +
                        " | Link=" +
                        linkName +
                        " | TypeId=" +
                        typeId.IntegerValue);

                    bool isLoaded;

                    try
                    {
                        isLoaded =
                            RevitLinkType.IsLoaded(
                                document,
                                typeId);
                    }
                    catch (Exception ex)
                    {
                        skipped++;

                        Log(
                            "LINK SKIP" +
                            " | Reason=IsLoadedException" +
                            " | Link=" +
                            linkName +
                            " | Error=" +
                            ex);

                        continue;
                    }

                    if (!isLoaded)
                    {
                        skipped++;

                        Log(
                            "LINK SKIP" +
                            " | Reason=NotLoaded" +
                            " | Link=" +
                            linkName);

                        continue;
                    }

                    bool canReload;

                    try
                    {
                        canReload =
                            linkType
                                .IsNotLoadedIntoMultipleOpenDocuments();
                    }
                    catch (Exception ex)
                    {
                        skipped++;

                        Log(
                            "LINK SKIP" +
                            " | Reason=MultipleOpenDocumentsCheckException" +
                            " | Link=" +
                            linkName +
                            " | Error=" +
                            ex);

                        continue;
                    }

                    Log(
                        "LINK STATE" +
                        " | Link=" +
                        linkName +
                        " | Loaded=True" +
                        " | IsNotLoadedIntoMultipleOpenDocuments=" +
                        canReload);

                    if (!canReload)
                    {
                        skipped++;

                        Log(
                            "LINK SKIP" +
                            " | Reason=MultipleOpenDocuments" +
                            " | Link=" +
                            linkName);

                        continue;
                    }

                    Stopwatch stopwatch =
                        Stopwatch.StartNew();

                    Log(
                        "RELOAD START" +
                        " | Link=" +
                        linkName +
                        " | TypeId=" +
                        typeId.IntegerValue);

                    try
                    {
                        LinkLoadResult result =
                            linkType.Reload();

                        stopwatch.Stop();

                        string resultCode =
                            result != null
                                ? result.LoadResult.ToString()
                                : "<null>";

                        Log(
                            "RELOAD RESULT" +
                            " | Link=" +
                            linkName +
                            " | Result=" +
                            resultCode +
                            " | DurationMs=" +
                            stopwatch.ElapsedMilliseconds);

                        if (result != null)
                        {
                            result.Dispose();
                        }

                        if (resultCode ==
                            LinkLoadResultType.LinkLoaded.ToString())
                        {
                            reloaded++;
                        }
                        else
                        {
                            failed++;
                        }
                    }
                    catch (Exception ex)
                    {
                        stopwatch.Stop();

                        failed++;

                        Log(
                            "RELOAD ERROR" +
                            " | Link=" +
                            linkName +
                            " | DurationMs=" +
                            stopwatch.ElapsedMilliseconds +
                            " | Exception=" +
                            ex);
                    }
                }

                TimeSpan totalDuration =
                    DateTime.Now -
                    sessionStart;

                Log(
                    "RELOAD SESSION END" +
                    " | Model=" +
                    hostModel +
                    " | Processed=" +
                    processed +
                    " | Reloaded=" +
                    reloaded +
                    " | Skipped=" +
                    skipped +
                    " | Failed=" +
                    failed +
                    " | DurationMs=" +
                    totalDuration.TotalMilliseconds);

                Log(
                    "--------------------------------------------------");
            }
            catch (Exception ex)
            {
                Log(
                    "RELOAD SESSION FATAL ERROR" +
                    " | Exception=" +
                    ex);

                Log(
                    "--------------------------------------------------");
            }
        }

        private static void Log(
            string message)
        {
            try
            {
                UpdateStartupService.LogDiagnostic(
                    message);
            }
            catch
            {
                // Ошибка логирования не должна
                // влиять на работу Revit.
            }
        }
    }
    }
