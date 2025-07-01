namespace DevelopersHub.ClashOfWhatever
{
    using UnityEngine;
    using DevelopersHub.RealtimeNetworking.Client;

    public class Player : MonoBehaviour
    {
        public enum RequestsID {
            AUTH = 1, SYNC = 2, BUILD = 3
        }
        void Start()
        {
            RealtimeNetworking.OnPacketReceived += ReceivedPacket;
            ConnectToServer();
        }

        private void ReceivedLong(int id, long value) {
            switch(id) {
                case 1:
                    Debug.Log(value);
                    break;
            }
        }
        private void ReceivedPacket(Packet packet) {
            int id = packet.ReadInt();
            Debug.Log("ReceivedPacket is called [" + id + "]");

            switch((RequestsID)id) {
                case RequestsID.AUTH:
                    long accountID = packet.ReadLong();
                    SendSyncRequests();
                    break;
                case RequestsID.SYNC:
                    string playerData = packet.ReadString();
                    Data.Player playerSyncData = Data.Desrialize<Data.Player>(playerData);
                    SyncData(playerSyncData);
                    break;
                case RequestsID.BUILD:
                    int response = packet.ReadInt();
                    switch (response) {
                        case 0:
                            Debug.Log("No resources");
                            break;
                        case 1:
                            Debug.Log("Placed successfully");
                            SendSyncRequests();
                            break;
                        case 2:
                            Debug.Log("Place taken");
                            break;
                    }
                    break;
            }
        }

        public void SendSyncRequests() {
            Packet p = new Packet();
            p.Write((int)RequestsID.SYNC);
            p.Write(SystemInfo.deviceUniqueIdentifier);
            Sender.TCP_Send(p);
        }

        private void SyncData(Data.Player player) {
            Debug.Log("SyncData is called");
            UI_Main.instance._goldText.text = player.gold.ToString();
            UI_Main.instance._elixerText.text = player.elixir.ToString();
            UI_Main.instance._gemsText.text = player.gems.ToString();
            if(player.buildings != null && player.buildings.Count > 0) {
                for (int i = 0 ; i < player.buildings.Count; i++) {
                    Building building = UI_Main.instance._grid.GetBuilding(player.buildings[i].databaseID);
                    if (building != null) {
                        
                    } else {
                        Building prefab = UI_Main.instance.GetBuildingPrefab(player.buildings[i].id);
                        if (prefab) {
                            Building b = Instantiate(prefab, Vector3.zero, Quaternion.identity);

                            b.PlacedOnGrid(player.buildings[i].x, player.buildings[i].y);
                            b._baseArea.gameObject.SetActive(false);

                            UI_Main.instance._grid.buildings.Add(b);
                        }
                    }
                }
            }
        }

        private void ConnectionResponse(bool successful) {
            if (successful) 
            {
                RealtimeNetworking.OnDisconnectedFromServer += DisconnectedFromServer;
                string device = SystemInfo.deviceUniqueIdentifier;
                Packet packet = new Packet();
                packet.Write((int)RequestsID.AUTH);
                packet.Write(device);
                Sender.TCP_Send(packet);
            } 
            else
            {
                // TODO : Connection failed message box with retry button 
            }
            RealtimeNetworking.OnConnectingToServerResult -= ConnectionResponse;
        }

        public void ConnectToServer() {
            RealtimeNetworking.OnConnectingToServerResult += ConnectionResponse;
            RealtimeNetworking.Connect();   
        }

        private void DisconnectedFromServer() {
            RealtimeNetworking.OnDisconnectedFromServer -= DisconnectedFromServer;
            // TODO : Connection failed message box with retry button 

        }
        
    }
}
