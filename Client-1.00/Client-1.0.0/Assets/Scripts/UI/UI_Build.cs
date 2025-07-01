namespace DevelopersHub.ClashOfWhatever {
    using DevelopersHub.RealtimeNetworking.Client;
    using UnityEngine;
    using UnityEngine.UI;
    public class UI_Build : MonoBehaviour
    {
        [SerializeField] public GameObject _elements = null;
        public RectTransform buttonConfirm = null;
        public RectTransform buttonCancel = null;

        private static UI_Build _instance = null; public static UI_Build instance {get { return _instance; }}

        void Awake()
        {
            _instance = this;
            _elements.SetActive(false);
        }

        private void Start()
        {
            buttonConfirm.gameObject.GetComponent<Button>().onClick.AddListener(Confirm);
            buttonCancel.gameObject.GetComponent<Button>().onClick.AddListener(Cancel);
            buttonConfirm.anchorMin = Vector3.zero;
            buttonConfirm.anchorMax = Vector3.zero;
            buttonCancel.anchorMin = Vector3.zero;
            buttonCancel.anchorMax = Vector3.zero;
        }
        
        void Update()
        {
            if (Building.instance != null && CameraController.instance.isPlaceBuilding) {
                Vector3 end = UI_Main.instance._grid.GetEndPosition(Building.instance);
                
                Vector3 planDownLeft = CameraController.instance.CameraScreenPositionToPlanePosition(Vector2.zero);
                Vector3 planTopRight = CameraController.instance.CameraScreenPositionToPlanePosition(new Vector2(Screen.width, Screen.height));

                float w = planTopRight.x - planDownLeft.x;
                float h = planTopRight.z - planDownLeft.z;

                float endW = end.x - planDownLeft.x;
                float endH = end.z - planDownLeft.z;
                
                Vector2 screenPoint = new Vector2(endW / w * Screen.width, endH / h * Screen.height);

                Vector2 confirmPoint = screenPoint;
                confirmPoint.x += (buttonConfirm.rect.width + 10f);
                buttonConfirm.anchoredPosition = confirmPoint;

                Vector2 cancelPoint = screenPoint;
                cancelPoint.x -= (buttonCancel.rect.width + 10f);
                buttonCancel.anchoredPosition = cancelPoint;
            }
        }
        public void SetStatus(bool status) {
            _elements.SetActive(status);
        }

        private void Confirm() {
            if(Building.instance != null) {
                Packet packet = new Packet();
                packet.Write((int)Player.RequestsID.BUILD);
                packet.Write(SystemInfo.deviceUniqueIdentifier);
                packet.Write(Building.instance.id);
                packet.Write(Building.instance.currentX);
                packet.Write(Building.instance.currentY);
                // send requests to server 
                Sender.TCP_Send(packet);
                Cancel();
            }
        }
        
        public void Cancel() {
            if (Building.instance != null) {
                CameraController.instance.isPlaceBuilding = false;
                Building.instance.RemovedFromGrid();
            }
        }
    }
}
