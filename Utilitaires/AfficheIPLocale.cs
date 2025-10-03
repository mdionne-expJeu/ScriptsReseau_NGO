using System.Net;
using System.Net.Sockets;
using UnityEngine;
using TMPro;

public class AfficheIPLocale : MonoBehaviour
{
    void Start()
    {
        string localIP = GetLocalIPAddress();
        GetComponent<TextMeshProUGUI>().text = "Mon IP locale : " + localIP;
    }

    string GetLocalIPAddress()
    {
        string localIP = "";
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork) // IPv4
            {
                localIP = ip.ToString();
                break;
            }
        }
        return localIP;
    }
}