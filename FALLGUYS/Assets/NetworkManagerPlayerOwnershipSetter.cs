using Unity.Netcode;
using UnityEngine;

public class NetworkManagerPlayerOwnershipSetter : MonoBehaviour {
    void Awake() {
        //NetworkManager.Singleton.OnClientConnectedCallback += ( clientId) =>
        //{
        //    var player = Instantiate(playerPrefab);
        //    player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
        //};
    }
}
