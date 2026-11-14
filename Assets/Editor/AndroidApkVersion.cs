using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;

namespace PeacePark.Editor
{
    internal static class AndroidApkVersion
    {
        internal static (string Name, int Code) Next(string publishedApk)
        {
            string name=PlayerSettings.bundleVersion;
            int code=PlayerSettings.Android.bundleVersionCode;
            if(File.Exists(publishedApk))
            {
                var published=Read(publishedApk);
                if(published.Code>=code)(name,code)=published;
            }
            if(!Version.TryParse(name,out var version))
                throw new InvalidOperationException("APK version must be numeric, for example 1.0.0.");
            return ($"{version.Major}.{version.Minor}.{checked(Math.Max(0,version.Build)+1)}",checked(code+1));
        }

        internal static (string Name, int Code) Read(string apk)
        {
            var tools=Path.Combine(EditorApplication.applicationContentsPath,
                "PlaybackEngines","AndroidPlayer","SDK","build-tools");
            string executable=ApplicationPlatformExe();
            var aapt=Directory.EnumerateDirectories(tools)
                .OrderByDescending(directory=>Version.TryParse(Path.GetFileName(directory),out var version)?version:new Version())
                .Select(directory=>Path.Combine(directory,executable)).FirstOrDefault(File.Exists);
            if(aapt==null)throw new FileNotFoundException("aapt is missing from the Unity Android SDK build-tools.");

            using var process=Process.Start(new ProcessStartInfo(aapt,$"dump badging \"{Path.GetFullPath(apk)}\"")
            {
                UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true
            });
            string output=process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            var package=Regex.Match(output,@"^package: .*?versionCode='([0-9]+)' versionName='([^']+)'",RegexOptions.Multiline);
            if(process.ExitCode!=0||!package.Success)
                throw new InvalidDataException($"Could not read the version in {apk}.");
            return (package.Groups[2].Value,int.Parse(package.Groups[1].Value,CultureInfo.InvariantCulture));
        }

        static string ApplicationPlatformExe()=>Path.DirectorySeparatorChar=='\\'?"aapt.exe":"aapt";
    }
}
