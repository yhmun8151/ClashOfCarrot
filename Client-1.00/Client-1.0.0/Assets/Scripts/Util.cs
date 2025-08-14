namespace DevelopersHub.ClashOfWhatever
{
    using UnityEngine;
    using System.Xml.Serialization;
    using System.IO;
    using System.Collections.Generic;
    using System;

    public class Util : MonoBehaviour
    {


        public static String gf_CommaValue(string number)
        {
            // 숫자 문자열을 정수로 변환하고, 천 단위 구분 기호를 추가하여 반환
            // 예외가 발생할 경우 원래 문자열을 반환
            if (string.IsNullOrEmpty(number))
            {
                return "0"; // 빈 문자열 처리
            }

            if (double.TryParse(number, out double parsedDoubleNumber))
            {
                number = parsedDoubleNumber.ToString();
            }

            // 숫자 문자열이 유효한지 확인
            if (long.TryParse(number, out long parsedLongNumber))
            {
                number = parsedLongNumber.ToString();
            }

            try
            {
                return string.Format("{0:N0}", long.Parse(number));
            }
            catch (Exception)
            {
                return number; // 유효하지 않은 숫자 문자열은 그대로 반환
            }
        }

        public static String gf_ToNumString(string number)
        {
            if (string.IsNullOrEmpty(number))
            {
                return "0"; // 빈 문자열 처리
            }

            if (double.TryParse(number, out double parsedDoubleNumber))
            {
                number = parsedDoubleNumber.ToString();
            }

            // 숫자 문자열이 유효한지 확인
            if (long.TryParse(number, out long parsedLongNumber))
            {
                number = parsedLongNumber.ToString();
            }

            return number;
        }

        public static String gf_CommaValue(long number)
        {
            return string.Format("{0:N0}", number);
        }

        public static String gf_CommaValue(int number)
        {
            return string.Format("{0:N0}", number);
        }

        public static String gf_CommaValue(double number)
        {
            return string.Format("{0:N0}", number);
        }

        /// <summary>
        /// 한국식 화폐 단위(조, 억, 만, 원)로 변환
        /// </summary>
        public static string ToKoreanCurrencyFormat(double number)
        {
            return ToKoreanCurrencyFormat((long)number, 2); // 모든 단위 표시 (조,억,만,원)
        }

        /// <summary>
        /// 한국식 화폐 단위로 변환하며, 표시할 단위의 수를 제한
        /// </summary>
        /// <param name="number">변환할 숫자</param>
        /// <param name="unitCount">표시할 단위의 수 (1-4: 조부터 시작해서 원까지)</param>
        public static string ToKoreanCurrencyFormat(long number, int unitCount)
        {
            if (number == 0) return "0원";
            if (unitCount < 1 || unitCount > 4) unitCount = 4;

            var units = new List<(long value, string name)>
            {
                (number / 1000000000000L, "조"),
                ((number % 1000000000000L) / 100000000, "억"),
                ((number % 100000000) / 10000, "만"),
                (number % 10000, "원")
            };

            var sb = new System.Text.StringBuilder();
            int displayedUnits = 0;
            bool hasValue = false;

            foreach (var unit in units)
            {
                if (unit.value > 0 || hasValue)
                {
                    if (displayedUnits < unitCount)
                    {
                        sb.Append($"{unit.value}{unit.name} ");
                        displayedUnits++;
                        hasValue = true;
                    }
                }
            }

            if (sb.Length == 0) return "0원";
            if (sb.Length > 0 && sb[sb.Length - 1] == ' ') sb.Length -= 1;
            return sb.ToString().Trim();
        }

        /// <summary>
        /// DART 인력 정보 JSON 문자열에서 남녀 직원 수를 계산
        /// </summary>
        /// <param name="dartData">DART 인력 정보 JSON 문자열</param>
        /// <returns>(남성 전체 인원, 여성 전체 인원) 튜플</returns>

        public static (int male, int female, long maleJanuarySalary, long femaleJanuarySalary, long maleTotalSalary, long femaleTotalSalary) GetEmployeeCount(string dartData)
        {
            try
            {
                Debug.Log("DART 데이터 파싱 시작..." );
                if (string.IsNullOrEmpty(dartData))
                {
                    Debug.LogWarning("DART 데이터가 비어있습니다.");
                    return (0, 0, 0, 0, 0, 0);
                }

                // 1. 큰따옴표 이스케이프 정리
                dartData = dartData.Replace("\"\"", "\"");
                dartData = dartData.Trim('"');

                // 2. JSON 배열 파싱
                DartDataItem[] items = JsonHelper.FromJson<DartDataItem>(dartData);

                int maleTotal = 0;
                int femaleTotal = 0;
                long maleTotalSalary = 0;
                long femaleTotalSalary = 0;
                long maleJanuarySalary = 0;
                long femaleJanuarySalary = 0;
                long totalSalary = 0;


                // 직원 수와 급여 총액 계산
                // 남성 직원은 "남", 여성 직원은 "여"로 구분
                // rgllbr_co는 직원, cnttk_co는 임원, sm은 임직원, fyer_salary_totamt는 연간 총 급여
                // 급여는 fyer_salary_totamt 필드에서 추출
                foreach (var emp in items)
                {
                    if (emp.sexdstn == "남")
                    {
                        int male = string.IsNullOrEmpty(emp.sm) || emp.sm == "-" ?
                            0 : int.Parse(emp.sm.Replace(",", ""));
                        int _januarySalary = string.IsNullOrEmpty(emp.jan_salary_am) || emp.jan_salary_am == "-" ?
                            0 : int.Parse(emp.jan_salary_am.Replace(",", ""));
                        long _totalSalary = string.IsNullOrEmpty(emp.fyer_salary_totamt) || emp.fyer_salary_totamt == "-" ?
                            0 : long.Parse(emp.fyer_salary_totamt.Replace(",", ""));

                        maleTotal += male;
                        maleJanuarySalary += _januarySalary;
                        maleTotalSalary += _totalSalary;
                    }
                    else if (emp.sexdstn == "여")
                    {
                        int female = string.IsNullOrEmpty(emp.sm) || emp.sm == "-" ?
                            0 : int.Parse(emp.sm.Replace(",", ""));
                        int _januarySalary = string.IsNullOrEmpty(emp.jan_salary_am) || emp.jan_salary_am == "-" ?
                            0 : int.Parse(emp.jan_salary_am.Replace(",", ""));
                        long _totalSalary = string.IsNullOrEmpty(emp.fyer_salary_totamt) || emp.fyer_salary_totamt == "-" ?
                            0 : long.Parse(emp.fyer_salary_totamt.Replace(",", ""));

                        femaleTotal += female;
                        femaleJanuarySalary += _januarySalary;
                        femaleTotalSalary += _totalSalary;
                    }
                    totalSalary = maleTotalSalary + femaleTotalSalary;

                    if (long.TryParse(emp.fyer_salary_totamt.Replace(",", ""), out long salary))
                    {
                        totalSalary += salary;
                    }
                }

                Debug.Log($"직원 수 계산 완료 - 남성: {maleTotal}, 여성: {femaleTotal}");
                Debug.Log($"급여 총액 계산 완료 - 남성: {maleTotalSalary}, 여성: {femaleTotalSalary}, 전체: {totalSalary}");

                // 3. 결과 반환
                // 남성 전체 인원, 여성 전체 인원
                Debug.Log("DART 데이터 파싱 완료.");
                return (maleTotal, femaleTotal, maleJanuarySalary, femaleJanuarySalary, maleTotalSalary, femaleTotalSalary);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"DART 데이터 파싱 에러: {e.Message}");
                return (0, 0, 0, 0, 0, 0);
            }
        }

        [System.Serializable]
        private class DartDataItem
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

        [System.Serializable]
        private class DartDataWrapper
        {
            public string items;
        }
        

    // JsonUtility가 배열을 바로 파싱할 수 있도록 도와주는 Helper
    public static class JsonHelper
    {
        public static T[] FromJson<T>(string json)
        {
            string newJson = "{ \"array\": " + json + "}";
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
            return wrapper.array;
        }

        [Serializable]
        private class Wrapper<T>
        {
            public T[] array;
        }
    }

    }
}