namespace DevelopersHub.ClashOfWhatever { 
    using UnityEngine;
    using UnityEngine.UI;

    public class UI_Building : MonoBehaviour
    {
        [SerializeField] private Button _button = null;
        void Start()
        {
            _button.onClick.AddListener(Clicked);
        }
        private void Clicked() {

        }   
    }

}