namespace DevelopersHub.ClashOfWhatever{
    using System;
    using UnityEngine;
    using UnityEngine.UI;

    public class UI_Shop : MonoBehaviour
    {
        [SerializeField] public GameObject _elements = null;
        [SerializeField] public Button _closeButton = null;
        private static UI_Shop _instance = null; public static UI_Shop instance {get { return _instance; }}

        void Awake()
        {
            _instance = this;
            _elements.SetActive(false);
        }
        void Start()
        {
            _closeButton.onClick.AddListener(CloseShop);   
        }
        private void CloseShop() {
            SetStatus(false);
            UI_Main.instance.SetStatus(true);
        }

        public void SetStatus(bool status) {
            _elements.SetActive(status);
        }
    }
}
