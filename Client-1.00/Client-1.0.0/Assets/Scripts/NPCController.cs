using UnityEngine;
using UnityEngine.AI;
using System.Collections;

namespace DevelopersHub.ClashOfWhatever
{
    public class NPCController : MonoBehaviour
    {
        [SerializeField] private float wanderRadius = 10f;        // NPC가 돌아다닐 반경
        [SerializeField] private float minWanderDelay = 3f;      // 다음 움직임까지의 최소 대기 시간
        [SerializeField] private float maxWanderDelay = 8f;      // 다음 움직임까지의 최대 대기 시간
        [SerializeField] private float moveSpeed = 2f;           // 이동 속도
        
        private Vector3 originalPosition;                         // 초기 위치 저장
        private Animator animator;                               // 애니메이터 컴포넌트
        private bool isMoving = false;                          // 현재 이동 중인지 여부

        void Start()
        {
            originalPosition = transform.position;
            animator = GetComponent<Animator>();
            StartCoroutine(WanderRoutine());
        }

        IEnumerator WanderRoutine()
        {
            while (true)
            {
                // 랜덤한 시간만큼 대기
                yield return new WaitForSeconds(Random.Range(minWanderDelay, maxWanderDelay));

                if (!isMoving)
                {
                    // 랜덤한 위치 생성
                    Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
                    randomDirection += originalPosition;
                    randomDirection.y = transform.position.y; // y축 고정

                    // NavMesh가 있다면 사용
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
                    {
                        StartCoroutine(MoveToPosition(hit.position));
                    }
                    else
                    {
                        // NavMesh가 없다면 직접 이동
                        StartCoroutine(MoveToPosition(randomDirection));
                    }
                }
            }
        }

        IEnumerator MoveToPosition(Vector3 targetPosition)
        {
            isMoving = true;
            
            if (animator != null)
            {
                animator.SetBool("IsWalking", true);
            }

            while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
            {
                // 목표 지점을 향해 회전
                Vector3 direction = (targetPosition - transform.position).normalized;
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);

                // 이동
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
                yield return null;
            }

            if (animator != null)
            {
                animator.SetBool("IsWalking", false);
            }

            isMoving = false;
        }

        void OnDrawGizmosSelected()
        {
            // 에디터에서 이동 반경을 시각적으로 표시
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, wanderRadius);
        }
    }
}
