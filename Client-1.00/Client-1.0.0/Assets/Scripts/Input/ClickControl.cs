using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Unity.VisualScripting;
using System.IO;
using System.Text;

namespace DevelopersHub.ClashOfWhatever
{
    public class ClickControl : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            Debug.Log("Click Control Start is called");
        }

        void Update()
        {
            if(Input.touchCount > 0) // 터치 갯수
            {
                Debug.Log("Click intercept..");
                Touch touch = Input.GetTouch(0); // 0번 - 가장 먼저 터치된 정보
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        // 터치가 발생한 시점
                        break;
                    case TouchPhase.Moved:
                        // 터치가 움직일 때
                        break;
                    case TouchPhase.Stationary:
                        // 터치가 대기중일 때
                        break;
                    case TouchPhase.Ended:
                        // 터치가 끝났을 때
                        break;
                    case TouchPhase.Canceled:
                        // 5개 이상의 터치가 발생하여 터치가 취소됐을 때
                        break;
                }
            }
        }
    }

}
