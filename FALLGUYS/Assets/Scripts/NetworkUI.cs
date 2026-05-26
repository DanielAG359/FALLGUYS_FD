using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class NetworkUI : MonoBehaviour
{
    private string ip = "127.0.0.1";
    private bool localReady = false;

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
            // Panel principal del juego
            GUI.Label(new Rect(10, 10, 250, 20), "Players: " + NetworkManager.Singleton.ConnectedClientsList.Count);

            // MOSTRAR GANADOR (si la partida terminó)
            if (GameManager.Instance.GameFinished.Value)
            {
                GUI.Label(new Rect(Screen.width / 2 - 100, Screen.height / 2 - 50, 200, 50),
                        "🏆 JUEGO TERMINADO 🏆",
                        EstiloTextoGrande());
                GUI.Label(new Rect(Screen.width / 2 - 100, Screen.height / 2, 200, 30),
                        $"GANADOR: Player {GameManager.Instance.GetWinnerId()}",
                        EstiloTextoCentrado());
            }
            // SOLO mostrar lobby si la partida NO ha terminado Y NO ha empezado
            else if (!GameManager.Instance.GameStarted.Value)
            {
                // ZONA READY
                if (!localReady)
                {
                    if (GUI.Button(new Rect(10, 40, 100, 30), "READY: NO"))
                    {
                        localReady = true;
                        GameManager.Instance.SetReadyRpc(true);
                    }
                }
                else
                {
                    GUI.Label(new Rect(10, 40, 100, 30), "READY: OK");
                }

                // ZONA HOST (BEGIN)
                if (NetworkManager.Singleton.IsServer)
                {
                    bool allReady = GameManager.Instance.AreAllPlayersReady();

                    GUI.enabled = allReady;

                    if (GUI.Button(new Rect(120, 40, 140, 30), "BEGIN MATCH"))
                    {
                        GameManager.Instance.TryStartGame();
                    }

                    GUI.enabled = true;

                    if (!allReady)
                    {
                        GUI.Label(new Rect(120, 75, 200, 20), "Waiting players...");
                    }
                }
            }
            else
            {
                GUI.Label(new Rect(10, 40, 200, 20), "MATCH RUNNING");
            }
        }
    }

    private GUIStyle EstiloTextoCentrado()
    {
        GUIStyle style = new GUIStyle();
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 20;
        style.normal.textColor = Color.yellow;
        return style;
    }

    private GUIStyle EstiloTextoGrande()
    {
        GUIStyle style = new GUIStyle();
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 32;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.red;
        return style;
    }
}