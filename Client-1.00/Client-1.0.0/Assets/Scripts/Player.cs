namespace DevelopersHub.ClashOfWhatever
{
    using UnityEngine;
    using UnityEngine.Networking;
    using System.Collections;
    using System.Collections.Generic;
    using System;
    using Unity.VisualScripting;
    using System.IO;
    using System.Text;
    using TMPro;
    using System.Linq; // LINQ 추가
    using System.Threading.Tasks;

    public class Player : MonoBehaviour
    {
        // key:value 형태로 저장
        // key(메뉴명)로 value를 뽑아오기 위해
        // 원하는 형태로 선언해도 무방
        Dictionary<string, CorpData> dicCorp = new Dictionary<string, CorpData>(); // 상품명 : menu(상품 이름, 가격, 정보)
        [SerializeField] String g_stbd_code = "005930"; // 기본값은 삼성전자 
        [SerializeField] GameObject buildings;
        [SerializeField] public GameObject TalkSet;
        public List<GameObject> _buildingList = new List<GameObject>();
        private static Player _instance = null;
        public static Player instance { get { return _instance; } }

        // 검색 UI 관련 변수들
        [SerializeField] private TextMeshProUGUI TitleText; // 제목 텍스트
        [SerializeField] private GameObject searchButton; // 돋보기 버튼
        [SerializeField] private GameObject searchPanel; // 검색 패널
        [SerializeField] private TMP_InputField searchInput; // 검색 입력 필드
        [SerializeField] private TextMeshProUGUI TalkSet_infoPanel; // 정보 표시 패널
        [SerializeField] private GameObject TalkSet_infoBackground; // 정보 패널 배경
        [SerializeField] private RectTransform suggestionsPanel; // 검색 제안 패널
        [SerializeField] private GameObject suggestionPrefab; // 검색 제안 항목 프리팹
        private List<GameObject> currentSuggestions = new List<GameObject>();
      
        // 대화창을 클릭했을 때 다음 설명을 보여주는 함수
        public void ShowNextDescription()
        {
            string description;
            if (GetNextDescription(currentBuildingName, out description))
            {
                ShowBuildingInfo(currentBuildingName); // 정보 패널 업데이트
            }
            else
            {
                TalkSet.SetActive(false);
                if (TalkSet_infoBackground != null)
                {
                    TalkSet_infoBackground.SetActive(false);
                }
            }
        }

        public async Task ShowBuildingInfo(string buildingName)
        {
            if (string.IsNullOrEmpty(buildingName) || TalkSet_infoPanel == null)
            {
                TalkSet_infoPanel.text = "";
                if (TalkSet_infoBackground != null)
                {
                    TalkSet_infoBackground.SetActive(false);
                }
                return;
            }

            CorpData corp = await GetCorpData(g_stbd_code);
            StringBuilder sb = new StringBuilder();

            if (buildingName.Contains("Plane_Field"))
            {
                buildingName = "인력정보"; // 인력정보 건물은 별도의 설명이 없으므로 처리
            }

            switch (buildingName)
            {
                case "BPS":
                    sb.AppendLine($"BPS(장부상 주당순자산가치)\n");
                    sb.AppendLine($"• BPS: {Util.gf_CommaValue(corp.BPS)}원");
                    double nProfit = double.Parse(corp.BPS) * double.Parse(corp.상장주식수);
                    sb.AppendLine($"• 순자산:  {Util.ToKoreanCurrencyFormat((long)nProfit, 2)}");
                    sb.AppendLine($"• 상장주식수: {Util.ToKoreanCurrencyFormat(long.Parse(corp.상장주식수), 3)}주");
                    sb.Append($"• 시가총액: {Util.ToKoreanCurrencyFormat(double.Parse(corp.시가총액))}");
                    break;
                case "PER":
                    sb.AppendLine($"PER(주가수익비율)\n");
                    double nEPS = double.Parse(corp.시가총액) / double.Parse(corp.PER);
                    sb.AppendLine($"• 예상 순이익 : {Util.ToKoreanCurrencyFormat((long)nEPS, 2)}");
                    sb.AppendLine($"• PER: {corp.PER}배");
                    sb.Append($"• 주당순이익: {Util.gf_CommaValue(corp.EPS)}원");
                    break;
                case "DIV":
                    sb.AppendLine($"DIV(배당수익률)\n");
                    sb.AppendLine($"• 배당수익률: {Util.gf_CommaValue(corp.DIV)}%");
                    sb.AppendLine($"• 예측배당금: {Util.gf_CommaValue(corp.DPS)}원");
                    sb.Append($"• 기준가:{Util.gf_CommaValue(corp.종가)}원");
                    break;
                case "DPS":
                    sb.AppendLine($"[{corp.CompName}의 배당금 정보]");
                    sb.AppendLine($"• DPS: {corp.DPS}원");
                    break;
                case "EPS":
                    sb.AppendLine($"[{corp.CompName}의 주당순이익 정보]");
                    sb.AppendLine($"• EPS: {corp.EPS}원");
                    sb.AppendLine($"• 올해 예상 순이익: {Util.gf_CommaValue((double.Parse(corp.EPS) * double.Parse(corp.상장주식수)).ToString())}원");
                    sb.Append($"• 현재가: {Util.gf_CommaValue(corp.종가)}원");
                    break;
                case "PBR":
                    sb.AppendLine($"[{corp.CompName}의 주가순자산비율 정보]");
                    sb.AppendLine($"• PBR: {corp.PBR}배");
                    sb.Append($"• 현재가: {Util.gf_CommaValue(corp.종가)}원");
                    break;
                case "시가총액":
                    sb.AppendLine($"[{corp.CompName}의 시장 가치]");
                    sb.AppendLine($"• 시가총액: {Util.ToKoreanCurrencyFormat(double.Parse(corp.시가총액))}원");
                    sb.AppendLine($"• 현재가: {Util.gf_CommaValue(corp.종가)}원");
                    sb.Append($"• 거래량: {Util.gf_CommaValue(corp.거래량)}주");
                    break;
                case "인력정보":
                    var (maleCount, femaleCount, maleJanuarySalary, femaleJanuarySalary, maleTotalSalary, femaleTotalSalary) = Util.GetEmployeeCount(corp.dart_data);
                    sb.AppendLine($"[{corp.CompName}의 인력 정보]");
                    sb.AppendLine($"• 남성 직원 수: {Util.gf_CommaValue(maleCount)} (평균 급여:{Util.gf_CommaValue(maleJanuarySalary)}원)");
                    sb.AppendLine($"• 여성 직원 수: {Util.gf_CommaValue(femaleCount)} (평균 급여:{Util.gf_CommaValue(femaleJanuarySalary)}원)");
                    sb.AppendLine($"• 총 급여: {Util.ToKoreanCurrencyFormat(maleTotalSalary + femaleTotalSalary)}원\n(남 :{Util.ToKoreanCurrencyFormat(maleTotalSalary)}원, 여 :{Util.ToKoreanCurrencyFormat(femaleTotalSalary)}원)");
                    break;
                default:
                    sb.AppendLine($"[{corp.CompName}의 기본 정보]");
                    sb.AppendLine($"• 현재가: {Util.gf_CommaValue(corp.종가)}원");
                    sb.AppendLine($"• 시가총액: {Util.ToKoreanCurrencyFormat(double.Parse(corp.시가총액))}원");
                    sb.Append($"• 등락률: {corp.등락률}%");
                    break;
            }

            TalkSet_infoPanel.text = sb.ToString();
            if (TalkSet_infoBackground != null)
            {
                TalkSet_infoBackground.SetActive(true);
            }
        }

        // 현재 클릭된 건물의 정보
        private string currentBuildingName = "";
        private int currentTalkIndex = 0;  // 현재 대화 인덱스

        // 건물별 설명 텍스트를 저장할 Dictionary
        private Dictionary<string, string[]> buildingDescriptions = new Dictionary<string, string[]>();

        // 건물 설명 초기화
        // 검색 UI 초기화 및 이벤트 설정
        private void InitializeSearchUI()
        {
            // 돋보기 버튼 클릭 이벤트
            searchButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() =>
            {
                searchPanel.SetActive(!searchPanel.activeSelf);
                if (searchPanel.activeSelf)
                {
                    searchInput.text = "";
                    UpdateSuggestions("");
                }
            });

            // 검색 입력 이벤트
            searchInput.onValueChanged.AddListener((value) =>
            {
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

            // suggestionPrefab 자체는 비활성화
            if (suggestionPrefab.activeSelf)
            {
                suggestionPrefab.SetActive(false);
            }

            // 검색어와 일치하는 기업 찾기
            var matches = dicCorp.Values
                .Where(corp => corp.CompName.ToLower().Contains(searchText) ||
                             corp.Ticker.ToLower().Contains(searchText))
                .Take(5); // 최대 5개까지만 표시

            foreach (var corp in matches)
            {
                var suggestionObj = Instantiate(suggestionPrefab, suggestionsPanel);
                suggestionObj.SetActive(true);  // 복제된 객체는 활성화
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
                button.onClick.AddListener(() =>
                {
                    SelectCompany(currentCorp.Ticker);
                });

                currentSuggestions.Add(suggestionObj);
            }
        }

        // 기업 선택 시 처리
        private void SelectCompany(string ticker)
        {
            Debug.Log($"SelectCompany called with ticker: {ticker} ");
            // 선택한 기업의 정보를 가져와서 UI 업데이트
            if (!dicCorp.ContainsKey(ticker))
            {
                Debug.LogWarning($"Ticker {ticker} not found in corp data.");
                return;
            }
            g_stbd_code = ticker;
            StartCoroutine(TransitionToNewCompany()); // 전환 효과 시작
            InitializeBuildingDescriptions(); // 건물 설명 업데이트
            searchPanel.SetActive(false);
        }

        private IEnumerator TransitionToNewCompany()
        {
            List<Coroutine> animations = new List<Coroutine>();

            // 모든 건물들을 위로 올라가게 하는 애니메이션
            foreach (GameObject building in _buildingList)
            {
                if (building.name.Contains("Plane_Field")) continue; // 밭 건물은 제외
                animations.Add(StartCoroutine(AnimateBuilding(building, true)));
            }

            // 모든 상승 애니메이션이 완료될 때까지 대기
            foreach (var anim in animations)
            {
                yield return anim;
            }

            // 건물들 회전 애니메이션
            animations.Clear();
            foreach (GameObject building in _buildingList)
            {
                if (building.name.Contains("Plane_Field")) continue;
                animations.Add(StartCoroutine(RotateBuilding(building)));
            }

            // 모든 회전 애니메이션이 완료될 때까지 대기
            foreach (var anim in animations)
            {
                yield return anim;
            }

            // 건물들을 아래로 내려오게 하는 애니메이션
            animations.Clear();
            foreach (GameObject building in _buildingList)
            {
                if (building.name.Contains("Plane_Field")) continue;
                animations.Add(StartCoroutine(AnimateBuilding(building, false)));
            }

            // 모든 하강 애니메이션이 완료될 때까지 대기
            foreach (var anim in animations)
            {
                yield return anim;
            }
        }

        private IEnumerator RotateBuilding(GameObject building)
        {
            float rotationDuration = 1.5f;  // 회전 시간을 늘림
            float elapsedTime = 0f;
            Vector3 startRotation = building.transform.eulerAngles;
            Vector3 endRotation = startRotation + new Vector3(0f, 360f, 0f);

            while (elapsedTime < rotationDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / rotationDuration;

                // 부드러운 회전을 위해 SmoothStep 사용
                float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

                // Vector3.Lerp를 사용하여 직접 오일러 각도 보간
                Vector3 currentRotation = Vector3.Lerp(startRotation, endRotation, smoothProgress);
                building.transform.eulerAngles = currentRotation;

                yield return null;
            }

            // 정확한 시작 회전값으로 복원
            building.transform.eulerAngles = startRotation;
        }

        private void ShuffleBuildings()
        {
            // 현재 건물들의 위치를 저장
            List<Vector3> originalPositions = _buildingList.Select(b => b.transform.position).ToList();
            List<Quaternion> originalRotations = _buildingList.Select(b => b.transform.rotation).ToList();

            // Fisher-Yates 알고리즘을 사용하여 위치를 셔플
            System.Random rnd = new System.Random();
            for (int i = originalPositions.Count - 1; i > 0; i--)
            {
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
            for (int i = 0; i < _buildingList.Count; i++)
            {
                _buildingList[i].transform.position = new Vector3(
                    originalPositions[i].x,
                    0, // y 좌표는 항상 0으로 유지
                    originalPositions[i].z
                );
                _buildingList[i].transform.rotation = originalRotations[i];
            }
        }

        private IEnumerator AnimateBuilding(GameObject building, bool goingUp)
        {
            float duration = 0.5f;
            float elapsedTime = 0f;
            Vector3 startPos = building.transform.position;
            Vector3 endPos = goingUp ?
                startPos + Vector3.up * 10f : // 위로 올라갈 때
                new Vector3(startPos.x, 3f, startPos.z); // 아래로 내려올 때

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / duration;
                progress = goingUp ? Mathf.Sin(progress * Mathf.PI * 0.5f) : 1f - Mathf.Cos(progress * Mathf.PI * 0.5f);

                building.transform.position = Vector3.Lerp(startPos, endPos, progress);
                yield return null;
            }

            building.transform.position = endPos;
        }

        private async void InitializeBuildingDescriptions()
        {
            CorpData corp = await GetCorpData(g_stbd_code);
            TitleText.text = $"{corp.CompName} ({corp.Ticker}) 기준가 : {Util.gf_CommaValue(corp.종가)} ({corp.Date})";
            if (!g_stbd_code.Equals("005930"))
            {
                if (corp == null || (double.Parse(corp.BPS) == 0 && double.Parse(corp.DIV) == 0 && double.Parse(corp.PER) == 0))
                {
                    TalkSet.SetActive(true);

                    g_stbd_code = "005930"; // 삼성전자 데이터로 초기화
                    SelectCompany(g_stbd_code); // 삼성전자 데이터로 초기화
                    return; // 데이터가 없으면 초기화 중단
                }
            }
            double nProfit = double.Parse(corp.BPS) * double.Parse(corp.상장주식수);
            buildingDescriptions["BPS"] = new string[] {
                ""
            };

            buildingDescriptions["PER"] = new string[] {
                ""
            };

            // 나머지 건물들의 설명도 추가
            buildingDescriptions["DIV"] = new string[] {
                ""
            };

            buildingDescriptions["DPS"] = new string[] {
                ""
            };
            buildingDescriptions["EPS"] = new string[] {
                ""
            };

            buildingDescriptions["PBR"] = new string[] {
                ""
            };

            buildingDescriptions["상장주식수"] = new string[] {
                ""
            };

            buildingDescriptions["시가총액"] = new string[] {
                ""
            };

            buildingDescriptions["인력정보"] = new string[] {
                ""
            };
        }

        // 다음 설명 텍스트 가져오기
        public bool GetNextDescription(string buildingName, out string description)
        {
            description = "";

            if (buildingName.Contains("Plane_Field"))
            {
                buildingName = "인력정보"; // 인력정보 건물은 별도의 설명이 없으므로 처리
            }

            // 처음 클릭한 건물이거나 다른 건물을 클릭한 경우
            if (buildingName != currentBuildingName)
            {
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

        void Awake()
        {
            _instance = this;
        }
        async void Start()
        {
            await System.Threading.Tasks.Task.Yield(); // 첫 프레임을 기다림
            ReadCSV(); // API 호출 시작
            InitializeBuildingDescriptions();  // 건물 설명 초기화
            InitData();
            
            // suggestionPrefab 초기 설정
            if (suggestionPrefab != null)
            {
                suggestionPrefab.SetActive(false);
            }
            
            InitializeSearchUI();  // 검색 UI 초기화

            // TalkSet에 Button 컴포넌트 추가
            if (!TalkSet.GetComponent<UnityEngine.UI.Button>())
            {
                UnityEngine.UI.Button button = TalkSet.AddComponent<UnityEngine.UI.Button>();
                button.onClick.AddListener(() => ShowNextDescription());
            }
        }

        private string[] buildingPurposes = {
            "BPS", "DIV", "DPS", "EPS", "PBR", "PER", "상장주식수","시가총액", "인력정보"
        };

        [SerializeField] private TMP_FontAsset buildingFont = null;

        private void InitData()
        {
            // 랜덤 색상을 위한 색상 배열 정의
            Color[] buildingColors = new Color[] {
                new Color(1f, 0.5f, 0.5f),  // 연한 빨강
                new Color(0.5f, 1f, 0.5f),  // 연한 초록
                new Color(0.5f, 0.5f, 1f),  // 연한 파랑
                new Color(1f, 1f, 0.5f),    // 연한 노랑
                new Color(1f, 0.5f, 1f)     // 연한 보라
            };

            // #1 buildingList init
            for (int i = 0; i < buildings.transform.childCount; i++)
            {
                GameObject _smallBuilding = buildings.transform.GetChild(i).gameObject;
                _smallBuilding.AddComponent<MyClickControls>();

                // 모든 자식 렌더러 컴포넌트 가져오기
                Renderer[] renderers = _smallBuilding.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
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
                if (buildingFont != null)
                {
                    signText.font = buildingFont;
                }
                else
                {
                    Debug.LogError("Noto Sans 폰트를 찾을 수 없습니다!");
                }

                if (!_smallBuilding.name.Contains("Plane_Field"))
                { // "Plane_Field"는 밭을 클릭하는거라 제외
                    _smallBuilding.name = buildingPurposes[i % buildingPurposes.Length];
                }

                _buildingList.Add(_smallBuilding);
            }
        }


        private async void ReadCSV()
        {
            try 
            {
                using (UnityWebRequest webRequest = UnityWebRequest.Get("https://carrotstock.com/api/carrot_game/master/"))
                {
                    // 요청 보내기
                    var operation = webRequest.SendWebRequest();

                    // 요청이 완료될 때까지 대기
                    while (!operation.isDone)
                        await System.Threading.Tasks.Task.Yield();

                    if (webRequest.result == UnityWebRequest.Result.Success)
                    {
                        string jsonData = webRequest.downloadHandler.text;
                        ProcessApiData(jsonData);
                    }
                    else
                    {
                        Debug.LogError($"ReadCSV API 요청 실패: {webRequest.error}");
                        // 에러 발생 시 로컬 데이터로 폴백
                        // LoadLocalData();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"데이터 로드 중 오류 발생: {e.Message}");
                // LoadLocalData();
            }
        }

        private void ProcessApiData(string jsonData)
        {
            try
            {
                // API에서 받은 JSON 데이터를 파싱
                var response = JsonUtility.FromJson<MasterList>(jsonData);
                if (response?.success == true && response?.data != null)
                {
                    foreach (var item in response.data)
                    {
                        // 개별 종목 정보를 API로 가져오기
                        if (!dicCorp.ContainsKey(item.ticker))
                        {
                            dicCorp.Add(item.ticker, new CorpData
                            {
                                Ticker = item.ticker,
                                CompName = item.CompName,
                                BPS = "0",
                                DIV = "0",
                                DPS = "0",
                                EPS = "0",
                                PBR = "0",
                                PER = "0",
                                거래대금 = "0",
                                거래량 = "0",
                                고가 = "0",
                                등락률 = "0",
                                상장주식수 = "0",
                                시가 = "0",
                                시가총액 = "0",
                                저가 = "0",
                                종가 = "0",
                                Date = "",
                                dart_data = new DartData[0]
                            });
                        }
                    }
                }
                else
                {
                    Debug.LogError("ProcessApiData 응답에서 유효한 데이터를 찾을 수 없습니다.");
                    // LoadLocalData();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"ProcessApiData JSON 파싱 중 오류 발생: {e.Message}");
                // LoadLocalData();
            }
        }

        [Serializable]
        private class StockItem
        {
            public string ticker;
            public string CompName;
        }

        [Serializable]
        private class MasterList
        {
            public bool success;
            public List<StockItem> data;
        }

        public class WebJsonData
        {
            public bool success;
            public CorpData data;  // 서버에서 'data' 필드로 응답을 보내므로 이름을 변경
        }

        [Serializable]
        public class DartData
        {
            public string rcept_no;
            public string corp_cls;
            public string corp_code;
            public string corp_name;
            public string sexdstn;
            public string fo_bbm;
            public string reform_bfe_emp_co_rgllbr;
            public string reform_bfe_emp_co_cnttk;
            public string reform_bfe_emp_co_etc;
            public string rgllbr_co;
            public string rgllbr_abacpt_labrr_co;
            public string cnttk_co;
            public string cnttk_abacpt_labrr_co;
            public string sm;
            public string avrg_cnwk_sdytrn;
            public string fyer_salary_totamt;
            public string jan_salary_am;
            public string rm;
            public string stlm_dt;
        }

        [Serializable]
        public class CorpData
        {
            public string Ticker;
            public string CompName;
            public string BPS;
            public string DIV;
            public string DPS;
            public string EPS;
            public string PBR;
            public string PER;
            public string 거래대금;
            public string 거래량;
            public string 고가;
            public string 등락률;
            public string 상장주식수;
            public string 시가;
            public string 시가총액;
            public string 저가;
            public string 종가;
            public string Date;
            public DartData[] dart_data;
        }      

        public async System.Threading.Tasks.Task<CorpData> GetCorpData(string szCode)
        {
            // 로컬 데이터가 있으면 반환
            if (dicCorp.ContainsKey(szCode) && dicCorp[szCode].BPS != "0")
            {
                return dicCorp[szCode];
            }

            // API를 통해 데이터 요청
            try
            {
                string url = $"https://carrotstock.com/api/carrot_game/stbd_code/?ticker={szCode}";
                using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
                {
                    var operation = webRequest.SendWebRequest();
                    while (!operation.isDone)
                        await System.Threading.Tasks.Task.Yield();

                    if (webRequest.result == UnityWebRequest.Result.Success)
                    {
                        string jsonData = webRequest.downloadHandler.text;
                        // API 응답에서 단일 CorpData 파싱
                        var rawData = JsonUtility.FromJson<WebJsonData>(jsonData);
                        var corpData = rawData.data;
                        Debug.Log($"GetCorpData API 응답: {corpData.CompName} ({corpData.dart_data})");
                        if (corpData != null && !string.IsNullOrEmpty(corpData.CompName))
                        {
                            dicCorp[szCode] = corpData; // 캐시에 저장
                            return corpData;
                        }
                    }
                    Debug.LogError($"GetCorpData API 요청 실패:{url} {webRequest.error}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"GetCorpData 데이터 로드 중 오류 발생: {e.Message}");
            }

            // API 호출 실패시 null 반환
            return null;
        }  
    }
}
