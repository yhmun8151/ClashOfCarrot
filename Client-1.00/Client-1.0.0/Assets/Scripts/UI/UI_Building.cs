namespace DevelopersHub.ClashOfWhatever {
    using DevelopersHub.RealtimeNetworking.Client;
    using UnityEngine;
    using UnityEngine.UI;

    public class UI_Building : MonoBehaviour
    {
        [SerializeField] private int _prefabIndex = 0;
        [SerializeField] private Button _button = null;
        private static UI_Building _instance = null;
        public static UI_Building instance {get { return _instance; } set {_instance = value;}}

        void Start()
        {
            _button.onClick.AddListener(Clicked);
        }
        private void Clicked() {
            // Shop에 빌딩 버튼 눌렸을때 이벤트 
            UI_Shop.instance.SetStatus(false);
            UI_Main.instance.SetStatus(true);

            Vector3 position = Vector3.zero;
            Building building = Instantiate(UI_Main.instance._buildingPrefabs[_prefabIndex], position, Quaternion.identity);
            Building.instance = building;
            building.PlacedOnGrid(20, 20);
            CameraController.instance.isPlaceBuilding = true;

            UI_Build.instance.SetStatus(true);
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