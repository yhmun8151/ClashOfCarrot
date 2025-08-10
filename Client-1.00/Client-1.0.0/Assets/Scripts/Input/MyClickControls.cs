namespace DevelopersHub.ClashOfWhatever {
    using UnityEngine;
    using UnityEngine.EventSystems;

    public class MyClickControls : MonoBehaviour
    {
        
        void Start()
        {
            // 현재 오브젝트의 부모 GameObject 가져오기
            GameObject parentObject = transform.parent.gameObject;
            GameObject myObject = transform.gameObject;
            
            // 2. 박스 콜라이더 컴포넌트 추가
            BoxCollider boxCollider = myObject.AddComponent<BoxCollider>();
            
        }
        
    }

}




