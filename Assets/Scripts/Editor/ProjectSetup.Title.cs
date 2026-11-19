using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EscapeFromSpace.EditorTools
{
    // 게임 시작 화면 씬 (기획서 13.3). 실행하면 이 씬이 먼저 뜨고, 시작을 누르면 대기실로 간다.
    public static partial class ProjectSetup
    {
        private const string TitleScenePath = "Assets/Scenes/Title.unity";

        private static void EnsureTitleScene()
        {
            Scene scene = File.Exists(TitleScenePath)
                ? EditorSceneManager.OpenScene(TitleScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            if (GameObject.Find("Main Camera") == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                cameraObject.transform.position = new Vector3(0f, 0f, -10f);
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                cameraObject.AddComponent<AudioListener>();
            }

            GameObject canvasObject = EnsureCanvas();
            EnsureComponent(canvasObject, out SettingsUI _);
            EnsureComponent(canvasObject, out TitleUI _);

            // 대기실과 같은 곡을 틀어, 시작을 눌러 넘어가도 음악이 끊기지 않게 한다.
            EnsureSceneAudio(null, 0f, LobbyMusicPath, 0.3f, 10f);
            EditorSceneManager.SaveScene(scene, TitleScenePath);
        }
    }
}
