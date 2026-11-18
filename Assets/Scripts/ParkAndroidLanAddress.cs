#if UNITY_ANDROID
using System.Net;
using UnityEngine;
using UnityEngine.Android;

namespace PeacePark
{
    internal static class ParkAndroidLanAddress
    {
        internal static IPAddress Read()
        {
            try
            {
                using var manager = AndroidApplication.currentContext?.Call<AndroidJavaObject>("getSystemService", "connectivity");
                if (manager == null) return null;

                using var capabilities = new AndroidJavaClass("android.net.NetworkCapabilities");
                int wifi = capabilities.GetStatic<int>("TRANSPORT_WIFI");
                int ethernet = capabilities.GetStatic<int>("TRANSPORT_ETHERNET");
                int vpn = capabilities.GetStatic<int>("TRANSPORT_VPN");
                using var active = manager.Call<AndroidJavaObject>("getActiveNetwork");
                var address = ReadNetwork(active);
                if (address != null) return address;

                // The default route can be cellular even while a local Wi-Fi network is available.
                var networks = manager.Call<AndroidJavaObject[]>("getAllNetworks");
                if (networks == null) return null;
                try
                {
                    foreach (var network in networks)
                    {
                        address = ReadNetwork(network);
                        if (address != null) return address;
                    }
                    return null;
                }
                finally
                {
                    foreach (var network in networks) network?.Dispose();
                }

                IPAddress ReadNetwork(AndroidJavaObject network)
                {
                    if (network == null) return null;
                    using var info = manager.Call<AndroidJavaObject>("getNetworkCapabilities", network);
                    if (info == null || info.Call<bool>("hasTransport", vpn) ||
                        !(info.Call<bool>("hasTransport", wifi) || info.Call<bool>("hasTransport", ethernet)))
                        return null;

                    using var properties = manager.Call<AndroidJavaObject>("getLinkProperties", network);
                    using var links = properties?.Call<AndroidJavaObject>("getLinkAddresses");
                    if (links == null) return null;
                    int count = links.Call<int>("size");
                    for (int i = 0; i < count; i++)
                    {
                        using var link = links.Call<AndroidJavaObject>("get", i);
                        using var ip = link.Call<AndroidJavaObject>("getAddress");
                        if (IPAddress.TryParse(ip.Call<string>("getHostAddress"), out var parsed) &&
                            ParkLanPanel.IsAdvertisableIPv4(parsed))
                            return parsed;
                    }
                    return null;
                }
            }
            catch (AndroidJavaException)
            {
                return null;
            }
        }
    }
}
#endif
