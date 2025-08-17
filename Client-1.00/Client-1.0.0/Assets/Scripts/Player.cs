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
        [SerializeField] public TextMeshProUGUI TalkPanel;
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
                TalkPanel.text = description;
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
                    sb.AppendLine($"• BPS: {Util.gf_CommaValue(corp.BPS)}원");
                    double nProfit = double.Parse(corp.BPS) * double.Parse(corp.상장주식수);
                    sb.AppendLine($"• 순자산: {Util.gf_CommaValue(nProfit.ToString())}원 ({Util.ToKoreanCurrencyFormat((long)nProfit, 2)})");
                    sb.AppendLine($"• 상장주식수: {Util.gf_CommaValue(corp.상장주식수)}주");
                    sb.Append($"• 시가총액: {Util.ToKoreanCurrencyFormat(double.Parse(corp.시가총액))}원");
                    break;
                case "PER":
                    double nEPS = double.Parse(corp.시가총액) / double.Parse(corp.PER);
                    sb.AppendLine($"[{corp.CompName}] 의 올해 순이익은 {Util.gf_CommaValue(nEPS.ToString())}원으로 ({Util.ToKoreanCurrencyFormat((long)nEPS, 2)})예상돼요.");
                    sb.AppendLine($"• PER: {corp.PER}배");
                    sb.Append($"• 주당순이익: {corp.EPS}원");
                    break;
                case "DIV":
                    sb.AppendLine($"[{corp.CompName}의 배당 정보]");
                    sb.AppendLine($"• 배당수익률: {corp.DIV}%");
                    sb.AppendLine($"• DPS: {corp.DPS}원");
                    sb.Append($"• 현재가: {Util.gf_CommaValue(corp.종가)}원");
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
            TitleText.text = $"{corp.CompName} ({corp.Ticker}) 기준가 : {Util.gf_CommaValue(corp.종가)}";
            if (!g_stbd_code.Equals("005930"))
            {
                if (corp == null || (double.Parse(corp.BPS) == 0 && double.Parse(corp.DIV) == 0 && double.Parse(corp.PER) == 0))
                {
                    TalkSet.SetActive(true);
                    TalkPanel.text = "해당 기업의 재무 데이터가 없어요. 삼성전자로 이동할게요.";

                    g_stbd_code = "005930"; // 삼성전자 데이터로 초기화
                    SelectCompany(g_stbd_code); // 삼성전자 데이터로 초기화
                    return; // 데이터가 없으면 초기화 중단
                }
            }
            double nProfit = double.Parse(corp.BPS) * double.Parse(corp.상장주식수);
            buildingDescriptions["BPS"] = new string[] {
                "BPS(주당순자산)는 한 주당 회사가 가지고 있는 진짜 가치를 말해.",
                "쉽게 말하면, 회사를 다 팔아서 빚 갚고 남은 돈을 주식 수로 나눈 것이야.",
                string.Format("{0}의 BPS는 {1}으로 기업의 순자산인 {2}원을 발행주식수인 {3}주로 나눈값을 뜻해.", corp.CompName, corp.BPS, Util.gf_CommaValue(nProfit), Util.gf_CommaValue(corp.상장주식수)),
            };

            buildingDescriptions["PER"] = new string[] {
                "PER(주가수익비율)은 “이 회사가 버는 돈에 비해, 주식 가격이 얼마나 비싼지”를 나타내는 숫자야!",
                String.Format("”{0}가 1년에 버는 돈 기준으로 {1}년 있어야 주식값만큼 번다”라고 이해하면 돼", corp.CompName, corp.PER),
                "즉, PER이 높으면 주식 가격이 비싸다는 뜻이고, 낮으면 싸다는 뜻이야.",

                "PER이 낮다고 해서 무조건 좋은 회사는 아니야. PER이 낮은 이유가 있을 수 있어. 예를 들어, 회사가 돈을 잘 벌지 못하거나, 실적이 떨어졌거나, 일시적으로 이익이 많아 보여서 PER이 낮아진 경우도 있어.",
                "반대로 PER이 높다고 해서 무조건 나쁜 회사는 아니야. PER이 높다는 건, 시장에서 그 회사의 미래 성장 가능성을 높게 보고 있다는 뜻일 수도 있어.",
                "업종마다 평균 PER이 다르기 때문에, 같은 업종끼리 비교해야 정확하니 유의해! (예: IT 기업은 보통 PER이 높고, 은행주는 낮은 편이야)",
            };

            // 나머지 건물들의 설명도 추가
            buildingDescriptions["DIV"] = new string[] {
                "DIV(배당수익률)은 주식을 샀을 때, 그 회사가 나에게 매년 얼마만큼의 돈을 돌려주는지를 보여주는 비율이야.",
                "쉽게 말하면, '내가 이 회사 주식 사서 1년 동안 얼마나 용돈 받는 거지?' 를 퍼센트(%)로 나타낸 거야.",
                string.Format("{0}의 배당수익률은 {1}%로, 주식 가격 대비 매년 {2}원의 배당금을 받을것으로 예측돼", corp.CompName, corp.DIV, corp.DPS),
            };

            buildingDescriptions["DPS"] = new string[] {
                "DPS(주당배당금)은 한 주당 얼마의 배당금을 받는지를 알려줘. “내가 주식 한 주를 가지고 있으면, 1년에 얼마를 받는 거야?” 라는 질문의 정답이 DPS야.",
                "DPS가 매년 늘어난다면, 그 회사는 주주에게 꾸준히 돈을 잘 돌려주는 회사일 수 있어.",
                string.Format("{0}의 DPS는 {1}원이야. 즉, 주식 한 주를 가지고 있으면 매년 {1}원을 배당금으로 받을 수 있어.", corp.CompName, corp.DPS),
            };
            buildingDescriptions["EPS"] = new string[] {
                "EPS(주당순이익)란 “이 회사가 1년 동안 번 돈을, 주식 1주당으로 나눴을 때 얼마를 벌었는지” 를 보여주는 숫자야!",
                "즉, 내가 주식 1주를 가지고 있다면, 그 주식은 회사의 이익 중 얼마나 가치가 있는지를 의미해",
                "1주당 얼마의 이익을 창출했는지 보여주는 지표로, EPS가 높을수록, 그 회사는 돈을 잘 벌고 있는 회사일 가능성이 높아. 혹은, 주가가 너무 높아 보일 때, 실제로는 EPS도 높아서 정당한 가격일 수도 있어!",
                string.Format("{0}의 EPS는 {1}원이야. 이 회사는 1주당 {1}원의 이익을 내고 있어.", corp.CompName, corp.EPS)
            };

            buildingDescriptions["PBR"] = new string[] {
                "PBR(주가순자산비율)은 “이 회사의 실제 자산가치에 비해, 주식이 얼마나 비싸게 거래되고 있는지” 를 보여주는 숫자야!",
                "쉽게 풀면 회사가 문을 닫게되어 자산을 모두 팔았을 때, 주식 한 주당 얼마를 받을 수 있는지와 실제 시장에서의 주가를 비교하는 거야.",
                "(주가 / 순자산)으로 계산할 수 있고, PBR은 1이면 주가가 자산가치와 비슷하다는 뜻이고, 1보다 크면 주식이 자산가치보다 비싸게 거래되고 있다는 뜻, 1보다 작으면 자산가치보다 싸게 거래되고 있다는 뜻이야.",
                string.Format("{0}의 PBR는 {1}로, 현재 주가는 순자산가치보다 {2}배 비싸게 거래되고 있어.", corp.CompName, corp.PBR, corp.PBR),
                "⚠️ 하지만 주의할 점! PBR이 낮다고 해서 무조건 좋은 회사는 아니야. PBR이 낮은 이유가 있을 수 있어. 예를 들어, 회사가 부채가 많거나, 미래 성장성이 낮다고 판단되면 PBR이 낮을 수 있어.",
                "특히 재무상태가 중요한 기업 (예: 은행, 제조업)에 더 잘 어울리는 지표니까 참고해!",
            };

            buildingDescriptions["상장주식수"] = new string[] {
                "상장주식수는 주식시장에 상장된 총 주식의 수를 의미해! 회사를 피자라고 생각하면, 그 피자를 얼마나 많은 조각으로 나눠서 시장에 팔았는지를 말해!",
                "이 숫자가 주가에 영향을 주기도 하고, PER, 시가총액, DPS 등을 계산할 때 꼭 필요해!",
                "그런데 상장된 주식 수는 고정된 게 아니라, 늘어나거나 줄어들 수 있어. 예를 들어, 회사가 새로운 주식을 발행하거나, 자사주 매입을 통해 주식 수를 줄일 수 있어.",
                "추가상장 : 상장주식수가 늘어나는 경우로, 회사가 새로운 주식을 발행해서 자금을 조달할 때 발생해. 이 경우, 기존 주주들의 지분이 희석될 수 있어.",
                "감자 : 상장주식수가 줄어드는 경우로, 회사가 자사주를 매입하거나, 주식 수를 줄여서 가치를 높이려는 경우야. 이 경우, 기존 주주들의 지분이 늘어날 수 있어.",
                "⚠️ 추가상장으로 기존 주주들의 지분이 희석되더라도 성장을 위해 필요한 자금을 조달하는 등 긍정적인 해석이 될 수 있고, 감자로 기존 주주들의 지분이 늘어나더라도 회사의 재무 구조 개선이나 적자 보전 등의 부정적인 해석이 될 수 있어 유의해야 해!",
            };

            buildingDescriptions["시가총액"] = new string[] {
                "시가총액은 기업의 전체 가치를 의미하며, 주식 시장에서 거래되는 모든 주식의 가치를 합한 것이야.",
                String.Format("쉽게 말해, [{0}]를 인수하고 싶으면 {1}원 만큼의 현금을 준비해야 해!", corp.CompName, Util.gf_CommaValue(corp.시가총액)),
            };

            var (maleCount, femaleCount, maleJanuarySalary, femaleJanuarySalary, maleTotalSalary, femaleTotalSalary) = Util.GetEmployeeCount(corp.dart_data);
            double maleRatio = (double)maleCount / (maleCount + femaleCount) * 100;
            double femaleRatio = (double)femaleCount / (maleCount + femaleCount) * 100;
            double nEPS = double.Parse(corp.시가총액) / double.Parse(corp.PER);

            buildingDescriptions["인력정보"] = new string[] {
                string.Format("[{0}]는 총 {1:N0}명의 임직원이 함께하고 있어!", corp.CompName, Util.gf_CommaValue(maleCount + femaleCount)),
                string.Format("남성 직원은 {0:N0}명으로 {1:F1}%, 여성 직원은 {2:N0}명으로 {3:F1}%를 차지하고 있어.",
                    Util.gf_CommaValue(maleCount), maleRatio,
                    Util.gf_CommaValue(femaleCount), femaleRatio),
                string.Format("올해 회사의 순이익이 {0}원으로 예상되는데, 직원의 인당 생산성은 {1:N0}으로 예측돼.", Util.ToKoreanCurrencyFormat(nEPS), Util.ToKoreanCurrencyFormat(nEPS / (maleCount + femaleCount))),
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
                                CompName = item.compName,
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
                                dart_code = "",
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

        private void LoadLocalData()
        {
            try
            {
                string path = "carrot_game_corp_data.csv";
                string[] lines = File.ReadAllLines(Path.Combine(Application.dataPath, "Files", path));

                foreach (string line in lines)
                {
                    var splitData = line.Split(',');
                    if (splitData.Length < 17) continue;

                    CorpData corp = new CorpData
                    {
                        Ticker = splitData[0],
                        CompName = splitData[1],
                        BPS = Util.gf_ToNumString(splitData[2]),
                        DIV = Util.gf_ToNumString(splitData[3]),
                        DPS = Util.gf_ToNumString(splitData[4]),
                        EPS = Util.gf_ToNumString(splitData[5]),
                        PBR = Util.gf_ToNumString(splitData[6]),
                        PER = Util.gf_ToNumString(splitData[7]),
                        거래대금 = Util.gf_ToNumString(splitData[8]),
                        거래량 = Util.gf_ToNumString(splitData[9]),
                        고가 = Util.gf_ToNumString(splitData[10]),
                        등락률 = Util.gf_ToNumString(splitData[11]),
                        상장주식수 = Util.gf_ToNumString(splitData[12]),
                        시가 = Util.gf_ToNumString(splitData[13]),
                        시가총액 = Util.gf_ToNumString(splitData[14]),
                        저가 = Util.gf_ToNumString(splitData[15]),
                        종가 = Util.gf_ToNumString(splitData[16]),
                        dart_code = splitData.Length > 17 ? splitData[17] : "",
                        dart_data = new DartData[0] // 로컬 데이터에서는 dart_data를 사용하지 않음
                    };

                    if (!String.IsNullOrEmpty(corp.CompName) && !String.IsNullOrEmpty(corp.BPS))
                    {
                        dicCorp[corp.Ticker] = corp;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"LoadLocalData 로컬 데이터 로드 중 오류 발생: {e.Message}");
            }
        }

        [Serializable]
        private class StockItem
        {
            public string ticker;
            public string compName;
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
            public string dart_code;
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
