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
    using UnityEditor;
    using TMPro;
    using System.Linq; // LINQ 추가

    public class Player : MonoBehaviour
    {
        // key:value 형태로 저장
        // key(메뉴명)로 value를 뽑아오기 위해
        // 원하는 형태로 선언해도 무방
        Dictionary<string, CorpData> dicCorp = new Dictionary<string, CorpData>(); // 상품명 : menu(상품 이름, 가격, 정보)
        [SerializeField] String stbd_code = "005930"; // 기본값은 삼성전자 
        [SerializeField] GameObject buildings;
        [SerializeField] public GameObject TalkSet;
        [SerializeField] public TextMeshProUGUI TalkPanel;
        public List<GameObject> _buildingList = new List<GameObject>();
        private static Player _instance = null;
        public static Player instance {get { return _instance; }}

        // 검색 UI 관련 변수들
        [SerializeField] private TextMeshProUGUI TitleText; // 제목 텍스트
        [SerializeField] private GameObject searchButton; // 돋보기 버튼
        [SerializeField] private GameObject searchPanel; // 검색 패널
        [SerializeField] private TMP_InputField searchInput; // 검색 입력 필드
        [SerializeField] private RectTransform suggestionsPanel; // 검색 제안 패널
        [SerializeField] private GameObject suggestionPrefab; // 검색 제안 항목 프리팹
        private List<GameObject> currentSuggestions = new List<GameObject>();

        // 대화창을 클릭했을 때 다음 설명을 보여주는 함수
        public void ShowNextDescription() {
            string description;
            if (GetNextDescription(currentBuildingName, out description)) {
                TalkPanel.text = description;
            } else {
                TalkSet.SetActive(false);
            }
        }
        
        // 현재 클릭된 건물의 정보
        private string currentBuildingName = "";
        private int currentTalkIndex = 0;  // 현재 대화 인덱스

        // 건물별 설명 텍스트를 저장할 Dictionary
        private Dictionary<string, string[]> buildingDescriptions = new Dictionary<string, string[]>();

        // 건물 설명 초기화
        // 검색 UI 초기화 및 이벤트 설정
        private void InitializeSearchUI() {
            // 돋보기 버튼 클릭 이벤트
            searchButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => {
                searchPanel.SetActive(!searchPanel.activeSelf);
                if (searchPanel.activeSelf) {
                    searchInput.text = "";
                    UpdateSuggestions("");
                }
            });

            // 검색 입력 이벤트
            searchInput.onValueChanged.AddListener((value) => {
                UpdateSuggestions(value);
            });

            // 초기에는 검색 패널 숨기기
            searchPanel.SetActive(false);
        }

        // 검색 제안 업데이트
        private void UpdateSuggestions(string searchText)
        {
            // 기존 제안 항목들 제거
            foreach (var suggestion in currentSuggestions)
            {
                Destroy(suggestion);
            }
            currentSuggestions.Clear();

            if (string.IsNullOrEmpty(searchText))
            {
                suggestionsPanel.gameObject.SetActive(false);
                return;
            }

            suggestionsPanel.gameObject.SetActive(true);
            searchText = searchText.ToLower();

            // 검색어와 일치하는 기업 찾기
            var matches = dicCorp.Values
                .Where(corp => corp.CompName.ToLower().Contains(searchText) ||
                             corp.Ticker.ToLower().Contains(searchText))
                .Take(5); // 최대 5개까지만 표시

            foreach (var corp in matches)
            {
                var suggestionObj = Instantiate(suggestionPrefab, suggestionsPanel);
                var suggestionText = suggestionObj.GetComponentInChildren<TextMeshProUGUI>();
                suggestionText.text = $"{corp.CompName} ({corp.Ticker})";

                // Button 컴포넌트 확인 및 추가
                var button = suggestionObj.GetComponent<UnityEngine.UI.Button>();
                if (button == null)
                {
                    // Button이 없으면 추가
                    button = suggestionObj.AddComponent<UnityEngine.UI.Button>();
                }

                // 현재 스코프의 corp 변수를 캡처하기 위해 임시 변수 사용
                var currentCorp = corp;
                button.onClick.AddListener(() => {
                    SelectCompany(currentCorp.Ticker);
                });

                currentSuggestions.Add(suggestionObj);
            }
        }

        // 기업 선택 시 처리
        private void SelectCompany(string ticker) {
            Debug.Log($"SelectCompany called with ticker: {ticker} ");
            // 선택한 기업의 정보를 가져와서 UI 업데이트
            if (!dicCorp.ContainsKey(ticker)) {
                Debug.LogWarning($"Ticker {ticker} not found in corp data.");
                return;
            }
            stbd_code = ticker;
            StartCoroutine(TransitionToNewCompany()); // 전환 효과 시작
            InitializeBuildingDescriptions(); // 건물 설명 업데이트
            searchPanel.SetActive(false);
        }

        private IEnumerator TransitionToNewCompany() {
            // 모든 건물들을 위로 올라가게 하는 애니메이션
            foreach (GameObject building in _buildingList) {
                StartCoroutine(AnimateBuilding(building, true));
            }
            
            yield return new WaitForSeconds(1f); // 건물이 사라지는 동안 대기
            
            // 건물들의 새로운 위치 계산 및 재배치
            // ShuffleBuildings();
            
            // 건물들을 아래로 내려오게 하는 애니메이션
            foreach (GameObject building in _buildingList) {
                StartCoroutine(AnimateBuilding(building, false));
            }
        }

        private void ShuffleBuildings() {
            // 현재 건물들의 위치를 저장
            List<Vector3> originalPositions = _buildingList.Select(b => b.transform.position).ToList();
            List<Quaternion> originalRotations = _buildingList.Select(b => b.transform.rotation).ToList();
            
            // Fisher-Yates 알고리즘을 사용하여 위치를 셔플
            System.Random rnd = new System.Random();
            for (int i = originalPositions.Count - 1; i > 0; i--) {
                int randomIndex = rnd.Next(0, i + 1);
                
                // 위치 교환
                Vector3 tempPos = originalPositions[i];
                originalPositions[i] = originalPositions[randomIndex];
                originalPositions[randomIndex] = tempPos;
                
                // 회전 교환
                Quaternion tempRot = originalRotations[i];
                originalRotations[i] = originalRotations[randomIndex];
                originalRotations[randomIndex] = tempRot;
            }
            
            // 셔플된 위치로 건물들 이동
            for (int i = 0; i < _buildingList.Count; i++) {
                _buildingList[i].transform.position = new Vector3(
                    originalPositions[i].x,
                    0, // y 좌표는 항상 0으로 유지
                    originalPositions[i].z
                );
                _buildingList[i].transform.rotation = originalRotations[i];
            }
        }

        private IEnumerator AnimateBuilding(GameObject building, bool goingUp) {
            float duration = 0.5f;
            float elapsedTime = 0f;
            Vector3 startPos = building.transform.position;
            Vector3 endPos = goingUp ? 
                startPos + Vector3.up * 10f : // 위로 올라갈 때
                new Vector3(startPos.x, 3f, startPos.z); // 아래로 내려올 때

            while (elapsedTime < duration) {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / duration;
                progress = goingUp ? Mathf.Sin(progress * Mathf.PI * 0.5f) : 1f - Mathf.Cos(progress * Mathf.PI * 0.5f);
                
                building.transform.position = Vector3.Lerp(startPos, endPos, progress);
                yield return null;
            }
            
            building.transform.position = endPos;
        }

        private void InitializeBuildingDescriptions() {
            CorpData corp = dicCorp[stbd_code];
            TitleText.text = $"{corp.CompName} ({corp.Ticker}) 기준가 : {Util.gf_CommaValue(corp.종가)}";
            buildingDescriptions["BPS"] = new string[] {
                "BPS(Book-value Per Share)는 주당순자산가치를 의미해요.",
                string.Format("{0}의 BPS는 {1}으로 기업의 순자산을 발행주식수인 {2}주를 나눈값 이에요.", corp.CompName, corp.BPS, Util.gf_CommaValue(corp.상장주식수)),
                string.Format("회사의 부도나 청산이 발생했을 때 1주당 청산가치라고 볼 수 있어요."),
            };
            
            buildingDescriptions["PER"] = new string[] {
                "PER(Price Earning Ratio)은 주가수익비율을 의미합니다.",
                "주가를 주당순이익(EPS)으로 나눈 값으로,",
                "기업의 수익 대비 주가의 수준을 보여줍니다."
            };

            // 나머지 건물들의 설명도 추가
            buildingDescriptions["DIV"] = new string[] {
                "DIV(배당수익률)은 주당 배당금을 주가로 나눈 값입니다.",
                "기업이 주주에게 지급하는 배당금의 수익률을 나타냅니다."
            };

            buildingDescriptions["DPS"] = new string[] {
                "DPS(Dividend Per Share)는 주당배당금을 의미합니다.",
                "1주당 지급되는 배당금액을 나타냅니다."
            };

            buildingDescriptions["EPS"] = new string[] {
                "EPS(Earning Per Share)는 주당순이익을 의미합니다.",
                "당기순이익을 발행주식수로 나눈 값으로,",
                "1주당 얼마의 이익을 창출했는지 보여줍니다."
            };

            buildingDescriptions["PBR"] = new string[] {
                "PBR(Price Book-value Ratio)은 주가순자산비율을 의미합니다.",
                "주가를 BPS로 나눈 값으로,",
                "순자산 대비 주가의 수준을 나타냅니다."
            };

            buildingDescriptions["상장주식수"] = new string[] {
                "상장주식수는 주식시장에 상장된 총 주식의 수입니다.",
                "기업의 전체 가치를 나누는 기준이 됩니다."
            };

            buildingDescriptions["시가총액"] = new string[] {
                "시가총액은 기업의 전체 가치를 의미합니다.",
                "주가와 상장주식수를 곱한 값으로,",
                "기업의 실질적인 시장가치를 나타냅니다."
            };

            buildingDescriptions["인력정보"] = new string[] {
                "해당 기업의 인력 현황을 보여줍니다.",
                "총 직원 수와 평균 근속연수 등의",
                "상세한 인력 정보를 확인할 수 있습니다."
            };
        }

        // 다음 설명 텍스트 가져오기
        public bool GetNextDescription(string buildingName, out string description) {
            description = "";
            
            // 처음 클릭한 건물이거나 다른 건물을 클릭한 경우
            if (buildingName != currentBuildingName) {
                currentBuildingName = buildingName;
                currentTalkIndex = 0;
            }

            // 해당 건물의 설명이 있는지 확인
            if (buildingDescriptions.ContainsKey(buildingName) &&
                currentTalkIndex < buildingDescriptions[buildingName].Length)
            {
                description = buildingDescriptions[buildingName][currentTalkIndex];
                currentTalkIndex++;
                return true;
            }

            // 모든 설명을 다 보여줬거나 설명이 없는 경우
            currentTalkIndex = 0;
            return false;
        }


        public enum RequestsID {
            AUTH = 1, SYNC = 2, BUILD = 3
        }
        void Awake() {
            _instance = this;
        }
        void Start()
        {
            RealtimeNetworking.OnPacketReceived += ReceivedPacket;
            ReadCSV();
            InitializeBuildingDescriptions();  // 건물 설명 초기화
            InitData();
            InitializeSearchUI();  // 검색 UI 초기화

            // TalkSet에 Button 컴포넌트 추가
            if (!TalkSet.GetComponent<UnityEngine.UI.Button>()) {
                UnityEngine.UI.Button button = TalkSet.AddComponent<UnityEngine.UI.Button>();
                button.onClick.AddListener(() => ShowNextDescription());
            }
        }

        private string[] buildingPurposes = {
            "BPS", "DIV", "DPS", "EPS", "PBR", "PER", "상장주식수","시가총액", "인력정보"
        };
        
        [SerializeField] private TMP_FontAsset buildingFont = null;

        private void InitData() {
            // 랜덤 색상을 위한 색상 배열 정의
            Color[] buildingColors = new Color[] {
                new Color(1f, 0.5f, 0.5f),  // 연한 빨강
                new Color(0.5f, 1f, 0.5f),  // 연한 초록
                new Color(0.5f, 0.5f, 1f),  // 연한 파랑
                new Color(1f, 1f, 0.5f),    // 연한 노랑
                new Color(1f, 0.5f, 1f)     // 연한 보라
            };

            // #1 buildingList init
            for (int i = 0; i < buildings.transform.childCount; i++) {
                GameObject _smallBuilding = buildings.transform.GetChild(i).gameObject;
                _smallBuilding.AddComponent<MyClickControls>();
                
                // 모든 자식 렌더러 컴포넌트 가져오기
                Renderer[] renderers = _smallBuilding.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers) {
                    Material newMaterial = new Material(renderer.material);
                    // newMaterial.color = buildingColors[i % buildingColors.Length];
                    renderer.material = newMaterial;
                }

                // 건물 위에 간판(TextMesh Pro) 추가
                GameObject signObject = new GameObject("BuildingSign");
                signObject.transform.SetParent(_smallBuilding.transform);
                
                // 건물 위에 적절한 위치로 설정 (건물의 높이를 고려)
                Renderer buildingRenderer = _smallBuilding.GetComponent<Renderer>();
                float buildingHeight = buildingRenderer != null ? buildingRenderer.bounds.size.y : 2f;
                buildingHeight = buildingHeight / _smallBuilding.GetComponent<Renderer>().transform.localScale.y;
                signObject.transform.localPosition = new Vector3(0, buildingHeight + 1.0f, 0);
                

                // 텍스트가 카메라를 향하도록 회전 (빌보드 효과)
                signObject.transform.rotation = Quaternion.Euler(45, 45, 0);
                
                // 텍스트 크기 조정을 위한 스케일 설정 (너비 2배)
                signObject.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                

                // TextMeshPro 컴포넌트 추가 및 설정
                TextMeshPro signText = signObject.AddComponent<TextMeshPro>();
                signText.text = buildingPurposes[i % buildingPurposes.Length];
                signText.fontSize = 70f;
                signText.alignment = TextAlignmentOptions.Center;
                signText.rectTransform.sizeDelta = new Vector2(40f, 10f);  // 텍스트 영역의 너비를 늘림
                signText.color = Color.white;
                
                // 텍스트가 잘 보이도록 설정
                signText.outlineWidth = 0.2f;
                signText.outlineColor = Color.black;

                // Noto Sans 폰트 설정
                if (buildingFont != null) {
                    signText.font = buildingFont;
                } else {
                    Debug.LogError("Noto Sans 폰트를 찾을 수 없습니다!");
                }
                
                _smallBuilding.name = buildingPurposes[i % buildingPurposes.Length];
                _buildingList.Add(_smallBuilding);
            }
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
        }

        public class CorpData
        {
            // Ticker,BPS,DIV,DPS,EPS,PBR,PER,거래대금,거래량,고가,등락률,상장주식수,시가,시가총액,저가,종가,dart_data,dart_code
            public String Ticker,CompName,BPS,DIV,DPS,EPS,PBR,PER,거래대금,거래량,고가,등락률,상장주식수,시가,시가총액,저가,종가,dart_data,dart_code;
        }        
    }
}
