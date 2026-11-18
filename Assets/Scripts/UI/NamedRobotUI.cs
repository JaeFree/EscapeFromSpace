using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EscapeFromSpace
{
    // 네임드 로봇의 머리 위 이름과 HP 바 (기획서 15.2: 늘 보인다). 화면 안에 있을 때만 그린다.
    public class NamedRobotUI : MonoBehaviour
    {
        [SerializeField, Tooltip("칸. 로봇 중심에서 이만큼 위에 띄운다")] private float _offset = 1.25f;

        private class Plate
        {
            public RectTransform Root;
            public Text Name;
            public Image Fill;
        }

        private readonly List<Plate> _plates = new List<Plate>();
        private Canvas _canvas;
        private Camera _camera;

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _camera = Camera.main;
        }

        private void LateUpdate()
        {
            IReadOnlyList<Robot> robots = Robot.Named;
            while (_plates.Count < robots.Count)
            {
                _plates.Add(BuildPlate());
            }

            for (int i = 0; i < _plates.Count; i++)
            {
                Plate plate = _plates[i];
                Robot robot = i < robots.Count ? robots[i] : null;
                bool show = robot != null && !robot.Health.IsDead;
                Vector3 screen = Vector3.zero;
                if (show)
                {
                    screen = _camera.WorldToScreenPoint(robot.transform.position + Vector3.up * _offset);
                    show = screen.z > 0f && screen.x > -100f && screen.y > -100f && screen.x < Screen.width + 100f && screen.y < Screen.height + 100f;
                }

                plate.Root.gameObject.SetActive(show);
                if (!show)
                {
                    continue;
                }

                UiBuilder.MoveToScreenPoint(_canvas, plate.Root, screen);
                plate.Name.text = robot.Data.DisplayName;
                plate.Fill.fillAmount = robot.Health.MaxHp > 0f ? robot.Health.CurrentHp / robot.Health.MaxHp : 0f;
            }
        }

        private Plate BuildPlate()
        {
            var plate = new Plate();
            plate.Root = UiBuilder.Rect("NamedPlate", transform, Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(220f, 52f));
            plate.Root.SetAsFirstSibling();

            RectTransform name = UiBuilder.Rect("Name", plate.Root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(240f, 30f));
            plate.Name = UiBuilder.Text(string.Empty, name, 20, TextAnchor.MiddleCenter, new Color(1f, 0.82f, 0.35f));
            plate.Name.fontStyle = FontStyle.Bold;
            UiBuilder.Outline(plate.Name);

            plate.Fill = UiBuilder.Bar("Hp", plate.Root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(170f, 16f),
                new Color(0.95f, 0.25f, 0.2f));
            plate.Root.gameObject.SetActive(false);
            return plate;
        }
    }
}
