using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class NetworkUI : MonoBehaviour
{
    private string ip = "127.0.0.1";

    void OnGUI()
    {
        if (!NetworkManager.Singleton) return;

        if (!NetworkManager.Singleton.IsClient &&
            !NetworkManager.Singleton.IsServer)
        {
            GUI.Label(new Rect(10, 10, 100, 20), "IP:");

            ip = GUI.TextField(
                new Rect(40, 10, 150, 25),
                ip
            );

            if (GUI.Button(new Rect(10, 50, 100, 30), "Host"))
            {
                var transport =
                    NetworkManager.Singleton
                    .GetComponent<UnityTransport>();

                transport.ConnectionData.Address = "0.0.0.0";

                NetworkManager.Singleton.StartHost();
            }

            if (GUI.Button(new Rect(120, 50, 100, 30), "Client"))
            {
                var transport =
                    NetworkManager.Singleton
                    .GetComponent<UnityTransport>();

                transport.ConnectionData.Address = ip;

                NetworkManager.Singleton.StartClient();
            }
        }
        else
        {
            GUI.Label(
                new Rect(10, 10, 250, 20),
                "Players: " +
                NetworkManager.Singleton.ConnectedClientsList.Count
            );

            if (NetworkManager.Singleton.IsServer)
            {
                if (GUI.Button(
                    new Rect(10, 40, 120, 30),
                    "Begin Match"))
                {
                    GameManager.Instance.TryStartGame();
                }
            }
        }
    }
}