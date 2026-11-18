#if UNITY_ANDROID
using System.IO;
using System.Xml.Linq;
using UnityEditor.Android;

namespace PeacePark.Editor
{
    public sealed class ParkAndroidProject : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 100;
        public void OnPostGenerateGradleAndroidProject(string path)
        {
            string launcher = Path.GetFullPath(Path.Combine(path, "../launcher/src/main"));
            if (!Directory.Exists(launcher)) throw new DirectoryNotFoundException(launcher);
            EnableNetworkState(Path.Combine(launcher, "AndroidManifest.xml"));
            WriteTitle(Path.Combine(launcher, "res/values/strings.xml"), ParkLanguage.EnglishTitle);
            WriteTitle(Path.Combine(launcher, "res/values-en/strings.xml"), ParkLanguage.EnglishTitle);
            WriteTitle(Path.Combine(launcher, "res/values-ko/strings.xml"), ParkLanguage.KoreanTitle);
        }

        static void EnableNetworkState(string path)
        {
            const string permission = "android.permission.ACCESS_NETWORK_STATE";
            XName name = XNamespace.Get("http://schemas.android.com/apk/res/android") + "name";
            var document = XDocument.Load(path);
            foreach (var item in document.Root.Elements("uses-permission"))
                if ((string)item.Attribute(name) == permission) return;
            document.Root.AddFirst(new XElement("uses-permission", new XAttribute(name, permission)));
            document.Save(path);
        }

        static void WriteTitle(string path, string title)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var document = File.Exists(path) ? XDocument.Load(path) : new XDocument(new XElement("resources"));
            XElement entry = null;
            foreach (var item in document.Root.Elements("string"))
                if ((string)item.Attribute("name") == "app_name") { entry = item; break; }
            if (entry == null) { entry = new XElement("string", new XAttribute("name", "app_name")); document.Root.Add(entry); }
            entry.Value = title;
            document.Save(path);
        }
    }
}
#endif
