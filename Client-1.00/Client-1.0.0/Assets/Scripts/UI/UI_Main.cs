namespace DevelopersHub.ClashOfWhatever {
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    public class UI_Main : MonoBehaviour
    {
        [SerializeField] public TextMeshProUGUI _goldText = null;
        [SerializeField] public TextMeshProUGUI _elixerText = null;
        [SerializeField] public TextMeshProUGUI _gemsText = null;
        [SerializeField] private Button _shopButton = null;
        private static UI_Main _instance = null;
        public static UI_Main instance {get { return _instance; }}
        void Awake()
        {
            _instance = this;
        }
        void Start()
        {
            _shopButton.onClick.AddListener(ShopButtonClicked);
        }
        private void ShopButtonClicked() {

        }
    }
}