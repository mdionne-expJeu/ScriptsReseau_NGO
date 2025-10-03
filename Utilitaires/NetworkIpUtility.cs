// NetworkIpUtility.cs
// Unity 2021+ / NGO v2+ compatible
using System.Collections;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using UnityEngine;

#if UNITY_NETCODE_FOUND || UNITY_2021_3_OR_NEWER
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
#endif

public class NetworkIpUtility : MonoBehaviour
{
    [Header("Public IP lookup")]
    [Tooltip("Service HTTP qui renvoie l'IP publique en texte brut.")]
    public string publicIpServiceUrl = "https://api.ipify.org";
    [Tooltip("Délai max pour la requête d'IP publique (secondes).")]
    public int publicIpTimeoutSec = 5;

    void Start()
    {
        StartCoroutine(PrintAllIPs());
    }

    IEnumerator PrintAllIPs()
    {
        string localIPv4 = GetLocalIPv4();
        Debug.Log($"[IP] Locale IPv4: {localIPv4}");

        string ngoBind = GetNGOBindAddress();
        if (!string.IsNullOrEmpty(ngoBind))
            Debug.Log($"[IP] NGO/UnityTransport écoute sur: {ngoBind}");
        else
            Debug.Log("[IP] UnityTransport non trouvé ou non configuré (NGO).");

        yield return StartCoroutine(GetPublicIP(ip =>
        {
            if (!string.IsNullOrEmpty(ip))
                Debug.Log($"[IP] Publique (WAN): {ip}");
            else
                Debug.LogWarning("[IP] Impossible d'obtenir l'IP publique (réseau ou service indisponible).");
        }));
    }

    public static string GetLocalIPv4()
    {
        // Méthode robuste: on parcourt les interfaces réseau actives
        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                    ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;

                var props = ni.GetIPProperties();
                foreach (var ua in props.UnicastAddresses)
                {
                    if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        string ip = ua.Address.ToString();
                        // Exclure APIPA 169.254.x.x
                        if (!ip.StartsWith("169.254.") && ip != "127.0.0.1")
                            return ip;
                    }
                }
            }
        }
        catch { /* fall back */ }

        // Repli simple: Dns
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork && ip.ToString() != "127.0.0.1")
                    return ip.ToString();
        }
        catch { }

        return "0.0.0.0";
    }

    public static string GetNGOBindAddress()
    {
#if UNITY_NETCODE_FOUND || UNITY_2021_3_OR_NEWER
        if (NetworkManager.Singleton != null)
        {
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                // ConnectionData.Address est l’adresse d’écoute côté serveur/host
                return transport.ConnectionData.Address;
            }
        }
#endif
        return string.Empty;
    }

    public IEnumerator GetPublicIP(System.Action<string> onDone)
    {
        using (var req = UnityEngine.Networking.UnityWebRequest.Get(publicIpServiceUrl))
        {
#if UNITY_2020_2_OR_NEWER
            req.timeout = publicIpTimeoutSec;
#endif
            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool ok = (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success);
#else
            bool ok = !(req.isNetworkError || req.isHttpError);
#endif
            onDone?.Invoke(ok ? req.downloadHandler.text.Trim() : null);
        }
    }
}
