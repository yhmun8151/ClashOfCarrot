namespace DevelopersHub.ClashOfWhatever
{
    using UnityEngine;
    using DevelopersHub.RealtimeNetworking.Client;
    using System.Collections;
    using System.Collections.Generic;
    using System;
    using Unity.VisualScripting;
    using System.IO;
    using System.Text;
    using UnityEditor.ShaderKeywordFilter;

    public class Player : MonoBehaviour
    {
        // key:value 형태로 저장
        // key(메뉴명)로 value를 뽑아오기 위해
        // 원하는 형태로 선언해도 무방
        Dictionary<string, CorpData> dicCorp = new Dictionary<string, CorpData>(); // 상품명 : menu(상품 이름, 가격, 정보)
        [SerializeField]
        String stbd_code = "005930"; // 기본값은 삼성전자 

        public enum RequestsID {
            AUTH = 1, SYNC = 2, BUILD = 3
        }
        void Start()
        {
            RealtimeNetworking.OnPacketReceived += ReceivedPacket;
            ReadCSV();
            ConnectToServer();
        }

        private void ReceivedLong(int id, long value) {
            switch(id) {
                case 1:
                    Debug.Log(value);
                    break;
            }
        }
        private void ReceivedPacket(Packet packet) {
            int id = packet.ReadInt();
            Debug.Log("ReceivedPacket is called [" + id + "]");

            switch((RequestsID)id) {
                case RequestsID.AUTH:
                    long accountID = packet.ReadLong();
                    SendSyncRequests();
                    break;
                case RequestsID.SYNC:
                    string playerData = packet.ReadString();
                    Data.Player playerSyncData = Data.Desrialize<Data.Player>(playerData);
                    SyncData(playerSyncData);
                    break;
                case RequestsID.BUILD:
                    int response = packet.ReadInt();
                    switch (response) {
                        case 0:
                            Debug.Log("No resources");
                            break;
                        case 1:
                            Debug.Log("Placed successfully");
                            SendSyncRequests();
                            break;
                        case 2:
                            Debug.Log("Place taken");
                            break;
                    }
                    break;
            }
        }

        public void SendSyncRequests() {
            Packet p = new Packet();
            p.Write((int)RequestsID.SYNC);
            p.Write(SystemInfo.deviceUniqueIdentifier);
            Sender.TCP_Send(p);
        }

        private void SyncData(Data.Player player) {
            Debug.Log("SyncData is called");
            UI_Main.instance._goldText.text = player.gold.ToString();
            UI_Main.instance._elixerText.text = player.elixir.ToString();
            UI_Main.instance._gemsText.text = player.gems.ToString();
            if(player.buildings != null && player.buildings.Count > 0) {
                for (int i = 0 ; i < player.buildings.Count; i++) {
                    Building building = UI_Main.instance._grid.GetBuilding(player.buildings[i].databaseID);
                    if (building != null) {
                        
                    } else {
                        Building prefab = UI_Main.instance.GetBuildingPrefab(player.buildings[i].id);
                        if (prefab) {
                            Building b = Instantiate(prefab, Vector3.zero, Quaternion.identity);

                            b.PlacedOnGrid(player.buildings[i].x, player.buildings[i].y);
                            b._baseArea.gameObject.SetActive(false);

                            UI_Main.instance._grid.buildings.Add(b);
                        }
                    }
                }
            }
        }

        private void ConnectionResponse(bool successful) {
            if (successful) 
            {
                RealtimeNetworking.OnDisconnectedFromServer += DisconnectedFromServer;
                string device = SystemInfo.deviceUniqueIdentifier;
                Packet packet = new Packet();
                packet.Write((int)RequestsID.AUTH);
                packet.Write(device);
                Sender.TCP_Send(packet);
            } 
            else
            {
                // TODO : Connection failed message box with retry button 
            }
            RealtimeNetworking.OnConnectingToServerResult -= ConnectionResponse;
        }

        public void ConnectToServer() {
            RealtimeNetworking.OnConnectingToServerResult += ConnectionResponse;
            RealtimeNetworking.Connect();   
        }

        private void DisconnectedFromServer() {
            RealtimeNetworking.OnDisconnectedFromServer -= DisconnectedFromServer;
            // TODO : Connection failed message box with retry button 

        }


        public void ReadCSV() {            
            // 읽어 올 파일 이름
            string path = "carrot_game_corp_data.csv";
            
            // 데이터를 저장하는 리스트 편하게 관리하기 위해 List로 선언
            List<CorpData> menuList = new List<CorpData>();

            // Application.dataPath는 Unity의 Assets폴더의 절대경로
            // 뒤에 읽으려는 파일이 있는 경로를 작성 ex) Assets > Files에 menu.csv를 읽으려면? "/" + "Files/menu.csv"추가
            StreamReader reader = new StreamReader(Application.dataPath + "/Files/" + path);

            // 마지막 줄을 판별하기 위한 bool 타입 변수
            bool isFinish = false;

            while(isFinish == false)
            {
                // ReadLine은 한줄씩 읽어서 string으로 반환하는 메서드
                // 한줄씩 읽어서 data변수에 담으면
                string data = reader.ReadLine(); // 한 줄 읽기
                
                // data 변수가 비었는지 확인
                if(data == null)
                {
                    // 만약 비었다면? 마지막 줄 == 데이터 없음이니
                    // isFinish를 true로 만들고 반복문 탈출
                    isFinish = true;
                    break;
                }
                
                // .csv는 ,(콤마)를 기준으로 데이터가 구분되어 있으므로 ,(콤마)를 기준으로 데이터를 나눠서 list에 담음
                var splitData = data.Split(','); // 콤마로 데이터 분할
                
                // 위에 생성했던 객체를 선언해주고
                CorpData corp = new CorpData();
                
                corp.Ticker = splitData[0];
                corp.CompName = splitData[1];
                corp.BPS = splitData[2];
                corp.DIV = splitData[3];
                corp.DPS = splitData[4];
                corp.EPS = splitData[5];
                corp.PBR = splitData[6];
                corp.PER = splitData[7];
                corp.거래대금 = splitData[8];
                corp.거래량 = splitData[9];
                corp.고가 = splitData[10];
                corp.등락률 = splitData[11];
                corp.상장주식수 = splitData[12];
                corp.시가 = splitData[13];
                corp.시가총액 = splitData[14];
                corp.저가 = splitData[15];
                corp.종가 = splitData[16];
                corp.dart_data = splitData[17];
                corp.dart_code = splitData[18];
                
                // menu 객체에 다 담았다면 dictionary에 key와 value값으로 저장
                // 이렇게 해두면 dicCorp.Add("005930");로 corp.시가총액, corp.PER .. 접근 가능
                dicCorp.Add(corp.Ticker, corp);
            }
            SyncData_CSV();
        }

        public class CorpData
        {
            // Ticker,BPS,DIV,DPS,EPS,PBR,PER,거래대금,거래량,고가,등락률,상장주식수,시가,시가총액,저가,종가,dart_data,dart_code
            public String Ticker,CompName,BPS,DIV,DPS,EPS,PBR,PER,거래대금,거래량,고가,등락률,상장주식수,시가,시가총액,저가,종가,dart_data,dart_code;
        }

        private void SyncData_CSV() {
            Debug.Log("SyncData is called");
            if (dicCorp[stbd_code] != null) {
                CorpData corp = dicCorp[stbd_code];
                Debug.Log(corp.CompName);
            }
            // UI_Main.instance._goldText.text = player.gold.ToString();
            // UI_Main.instance._elixerText.text = player.elixir.ToString();
            // UI_Main.instance._gemsText.text = player.gems.ToString();
            // if(player.buildings != null && player.buildings.Count > 0) {
            //     for (int i = 0 ; i < player.buildings.Count; i++) {
            //         Building building = UI_Main.instance._grid.GetBuilding(player.buildings[i].databaseID);
            //         if (building != null) {
                        
            //         } else {
            //             Building prefab = UI_Main.instance.GetBuildingPrefab(player.buildings[i].id);
            //             if (prefab) {
            //                 Building b = Instantiate(prefab, Vector3.zero, Quaternion.identity);

            //                 b.PlacedOnGrid(player.buildings[i].x, player.buildings[i].y);
            //                 b._baseArea.gameObject.SetActive(false);

            //                 UI_Main.instance._grid.buildings.Add(b);
            //             }
            //         }
            //     }
            // }
        }
        
    }
}
