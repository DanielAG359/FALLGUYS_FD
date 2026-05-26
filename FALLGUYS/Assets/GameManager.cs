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

    // READY STATES
    private Dictionary<ulong, bool> readyPlayers =
        new Dictionary<ulong, bool>();

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

            NetworkManager.Singleton.OnClientDisconnectCallback +=
                OnClientDisconnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        readyPlayers[clientId] = false;

        AssignSpawn(clientId);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (readyPlayers.ContainsKey(clientId))
        {
            readyPlayers.Remove(clientId);
        }
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


    [Rpc(SendTo.Server)]
    public void SetReadyRpc(bool ready, RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        readyPlayers[clientId] = ready;

    }

    public bool IsPlayerReady(ulong clientId)
    {
        if (readyPlayers.ContainsKey(clientId)) return readyPlayers[clientId];

        return false;
    }

    public bool AreAllPlayersReady()
    {
        if (readyPlayers.Count < 2) return false;

        foreach (var player in readyPlayers)
        {
            if (!player.Value) return false;
        }

        return true;
    }

    public void TryStartGame()
    {
        if (!IsServer) return;

        if (!AreAllPlayersReady())
        {
            Debug.Log("Not all players ready");
            return;
        }

        GameStarted.Value = true;

        Debug.Log("MATCH STARTED");
    }

    public void PlayerFinished(PlayerController player)
    {
        if (finishedPlayers.Contains(player)) return;

        finishedPlayers.Add(player);

        int total = NetworkManager.Singleton.ConnectedClientsList.Count;

        if (finishedPlayers.Count >= total - 1)
        {
            Debug.Log("ROUND FINISHED");
        }
    }
}