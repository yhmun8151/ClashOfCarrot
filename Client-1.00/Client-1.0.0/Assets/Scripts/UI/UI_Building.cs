namespace DevelopersHub.ClashOfWhatever {
    using DevelopersHub.RealtimeNetworking.Client;
    using UnityEngine;
    using UnityEngine.UI;

    public class UI_Building : MonoBehaviour
    {
        [SerializeField] private string _id = "";
        [SerializeField] private Button _button = null;
        private static UI_Building _instance = null;
        public static UI_Building instance {get { return _instance; } set {_instance = value;}}

        void Start()
        {
            _button.onClick.AddListener(Clicked);
        }
        private void Clicked() {
            Building prefab = UI_Main.instance.GetBuildingPrefab(_id);
            if (prefab) {
                // Shop에 빌딩 버튼 눌렸을때 이벤트 
                UI_Shop.instance.SetStatus(false);
                UI_Main.instance.SetStatus(true);

                Vector3 position = Vector3.zero;

                Building building = Instantiate(prefab, position, Quaternion.identity);
                building.PlacedOnGrid(20, 20);
                building._baseArea.gameObject.SetActive(true);

                Building.instance = building;
                CameraController.instance.isPlaceBuilding = true;

                UI_Build.instance.SetStatus(true);
            }
        }
        
    }

}