using Autodesk.Revit.DB;
using RevitCopyParams.Models;
using System;
using System.Reflection;

namespace RevitCopyParams.Services
{
    public static class UpdateService
    {
        public static Version GetCurrentVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();

            return assembly.GetName().Version;
        }


        public static UpdateInfo GetUpdateInfo()
        {
            return new UpdateInfo
            {
                CurrentVersion = GetCurrentVersion()
            };
        }
    }
}