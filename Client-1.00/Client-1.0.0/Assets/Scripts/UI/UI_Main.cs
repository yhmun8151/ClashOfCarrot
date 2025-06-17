namespace DevelopersHub.ClashOfWhatever {
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    public class UI_Main : MonoBehaviour
    {
        [SerializeField] private GameObject _elements = null;
        [SerializeField] public TextMeshProUGUI _goldText = null;
        [SerializeField] public TextMeshProUGUI _elixerText = null;
        [SerializeField] public TextMeshProUGUI _gemsText = null;
        [SerializeField] private Button _shopButton = null;
        [SerializeField] private Building[] _buildingPrefabs = null;
        private static UI_Main _instance = null;
        public static UI_Main instance {get { return _instance; }}
        private bool _active = true; public bool isActive{ get { return _active; }}
        void Awake()
        {
            _instance = this;
            _elements.SetActive(true);
        }
        void Start()
        {
            _shopButton.onClick.AddListener(ShopButtonClicked);
        }
        private void ShopButtonClicked() {
            UI_Shop.instance.SetStatus(true);
            SetStatus(false);
        }

        public void SetStatus(bool status) {
            _active = status;
            _elements.SetActive(status);
        }
    }
}