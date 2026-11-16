using System.IO;
using UnityEngine;

namespace EscapeFromSpace
{
    // 저장 파일 읽고 쓰기. 파일은 PC마다 따로 있는 사용자 데이터 폴더에 두고 저장소에는 넣지 않는다.
    public static class SaveSystem
    {
        private const string FileName = "save.json";

        // 자동 테스트가 실제 저장 파일을 건드리지 않게 다른 경로를 쓸 때 넣는다.
        public static string PathOverride { get; set; }

        public static string FilePath => string.IsNullOrEmpty(PathOverride)
            ? Path.Combine(Application.persistentDataPath, FileName)
            : PathOverride;

        // 저장 파일이 없으면 null (처음 시작).
        public static SaveData Load()
        {
            if (!File.Exists(FilePath))
            {
                return null;
            }

            return JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
        }

        public static void Save(SaveData data)
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
        }

        public static void Delete()
        {
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }
        }
    }
}
