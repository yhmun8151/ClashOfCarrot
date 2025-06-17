namespace DevelopersHub.ClashOfWhatever{
    using UnityEngine;

    public class Building : MonoBehaviour
    {
        [System.Serializable] public class Level{
            public int level = 1;
            public Sprite icon = null;
            public GameObject mesh = null;
        }
        private BuildGrid _grid = null;

        [SerializeField] private int _rows = 1;
        [SerializeField] private int _columns = 1;
        [SerializeField] private MeshRenderer _baseArea = null;
        [SerializeField] Level[] _levels = null;

        private int _currentX = 0;
        private int _currentY = 0;
        
    }
}
