using UnityEngine;

namespace EscapeFromSpace
{
    public enum VolumeChannel
    {
        Master,
        Music,
        Effects,
        Ambience,
    }

    // 설정 메뉴의 음량 (기획서 13.1). 저장 파일(SaveData)이 아니라 그 PC의 PlayerPrefs에 둔다.
    // 학교 PC와 집 PC의 스피커가 달라서, 진행 상황과 함께 옮겨 다니면 오히려 불편하다.
    public static class GameSettings
    {
        private static readonly string[] Keys = { "Volume.Master", "Volume.Music", "Volume.Effects", "Volume.Ambience" };
        private static float[] _values;

        public static float Get(VolumeChannel channel)
        {
            Load();
            return _values[(int)channel];
        }

        public static void Set(VolumeChannel channel, float value)
        {
            Load();
            value = Mathf.Clamp01(value);
            _values[(int)channel] = value;
            try
            {
                PlayerPrefs.SetFloat(Keys[(int)channel], value);
                PlayerPrefs.Save();
            }
            catch (System.Exception)
            {
                // 저장이 막혀 있어도 이번 실행 동안은 바꾼 값으로 들린다.
            }
        }

        private static void Load()
        {
            if (_values != null)
            {
                return;
            }

            _values = new float[Keys.Length];
            for (int i = 0; i < Keys.Length; i++)
            {
                _values[i] = PlayerPrefs.GetFloat(Keys[i], 1f);
            }
        }
    }
}
