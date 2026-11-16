using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace EscapeFromSpace.EditorTools
{
    // Windows 빌드와 저장 파일 초기화. 빌드 결과는 저장소에 넣지 않는 Builds 폴더에 나온다.
    public static class BuildScript
    {
        private const string OutputPath = "Builds/Windows/EscapeFromSpace.exe";

        [MenuItem("EscapeFromSpace/Windows 빌드")]
        public static void BuildWindows()
        {
            var options = new BuildPlayerOptions
            {
                // 대기실에서 시작한다. 템플릿의 SampleScene은 쓰지 않으므로 넣지 않는다.
                scenes = new[] { "Assets/Scenes/Lobby.unity", "Assets/Scenes/Station.unity" },
                locationPathName = OutputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            bool succeeded = report.summary.result == BuildResult.Succeeded;
            Debug.Log($"[BuildScript] 빌드 {(succeeded ? "성공" : "실패")}: {OutputPath} ({report.summary.totalSize / (1024 * 1024)}MB)");

            // 명령줄로 돌렸을 때 실패를 종료 코드로 알 수 있게 한다.
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(succeeded ? 0 : 1);
            }
        }

        // 지우면 다음에 대기실에 들어갈 때 기본 세트 3세트로 처음부터 시작한다. 전체 맵 기록도 지워진다.
        [MenuItem("EscapeFromSpace/저장 초기화")]
        public static void ResetSave()
        {
            SaveSystem.Delete();
            Debug.Log($"[BuildScript] 저장 파일을 지웠다: {SaveSystem.FilePath}");
        }
    }
}
