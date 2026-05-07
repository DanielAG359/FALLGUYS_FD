using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    public NetworkVariable<bool> GameStarted =
        new NetworkVariable<bool>(false);

    [SerializeField]
    private Transform[] spawnPoints;

    private List<PlayerController> finishedPlayers =
        new List<PlayerController>();

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback +=
                OnClientConnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        AssignSpawn(clientId);
    }

    private void AssignSpawn(ulong clientId)
    {
        var player =
            NetworkManager.Singleton
            .ConnectedClients[clientId]
            .PlayerObject;

        int randomSpawn =
            Random.Range(0, spawnPoints.Length);

        player.transform.position =
            spawnPoints[randomSpawn].position;
    }

    public void TryStartGame()
    {
        if (!IsServer) return;

        if (NetworkManager.Singleton.ConnectedClientsList.Count >= 2)
        {
            GameStarted.Value = true;

            Debug.Log("MATCH STARTED");
        }
    }

    public void PlayerFinished(PlayerController player)
    {
        if (finishedPlayers.Contains(player))
            return;

        finishedPlayers.Add(player);

        int total =
            NetworkManager.Singleton.ConnectedClientsList.Count;

        if (finishedPlayers.Count >= total - 1)
        {
            Debug.Log("ROUND FINISHED");
        }
    }
}