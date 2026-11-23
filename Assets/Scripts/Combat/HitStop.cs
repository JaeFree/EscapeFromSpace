using System.Collections;
using UnityEngine;

namespace EscapeFromSpace
{
    // 로봇을 부순 순간 아주 잠깐 멈춘다 (기획서 20.6). 처치를 손으로 느끼게 하려는 것이다.
    // 일시 정지(설정)나 결과 화면도 timeScale을 쓰므로, 멈춘 사이에 그쪽이 바꾸면 되돌리지 않는다.
    public class HitStop : MonoBehaviour
    {
        private static HitStop _instance;

        // 멈춰 있는 동안 설정 창이 열리면, 닫을 때 되돌릴 값을 1로 본다 (멈춘 채로 남지 않게).
        public static bool IsActive { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
            IsActive = false;
        }

        public static void Play(float seconds)
        {
            if (seconds <= 0f || IsActive || Time.timeScale < 1f)
            {
                return;
            }

            if (_instance == null)
            {
                _instance = new GameObject("HitStop").AddComponent<HitStop>();
            }

            _instance.StartCoroutine(_instance.Run(seconds));
        }

        private IEnumerator Run(float seconds)
        {
            const float StopScale = 0.0001f;
            IsActive = true;
            Time.timeScale = StopScale;
            yield return new WaitForSecondsRealtime(seconds);

            // 그사이 일시 정지나 결과 화면이 timeScale을 0으로 바꿨으면 그대로 둔다.
            if (Mathf.Approximately(Time.timeScale, StopScale))
            {
                Time.timeScale = 1f;
            }

            IsActive = false;
        }

        private void OnDestroy()
        {
            // 씬이 바뀌어 멈춘 채로 지워지면 되돌린다.
            if (IsActive && Mathf.Approximately(Time.timeScale, 0.0001f))
            {
                Time.timeScale = 1f;
            }

            IsActive = false;
        }
    }
}
