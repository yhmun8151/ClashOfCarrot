namespace DevelopersHub.ClashOfWhatever {
    using DevelopersHub.RealtimeNetworking.Client;
    using UnityEngine;
    using UnityEngine.UI;

    public class UI_Building : MonoBehaviour
    {
        [SerializeField] private int _prefabIndex = 0;
        [SerializeField] private Button _button = null;
        void Start()
        {
            _button.onClick.AddListener(Clicked);
        }
        private void Clicked() {
            UI_Shop.instance.SetStatus(false);
            UI_Main.instance.SetStatus(true);

            Vector3 position = Vector3.zero;
            Building building = Instantiate(UI_Main.instance._buildingPrefabs[_prefabIndex], position, Quaternion.identity);
        }   
        private void ConfirmBuild() {
            Packet packet = new Packet();
            packet.Write((int)Player.RequestsID.BUILD);
            packet.Write(SystemInfo.deviceUniqueIdentifier);
            packet.Write(_prefabIndex);
            // send requests to server 
            Sender.TCP_Send(packet);
        }
    }

}