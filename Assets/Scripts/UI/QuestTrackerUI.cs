using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EscapeFromSpace
{
    // 레이드 중 화면 오른쪽의 퀘스트 진행도 (기획서 14.3: 최대 3개). 레이드에서 셀 수 있는 것(처치·탈출·방문)만 보여 준다.
    public class QuestTrackerUI : MonoBehaviour
    {
        private const int MaxShown = 3;

        private RectTransform _root;
        private readonly List<Text> _lines = new List<Text>();
        private float _refreshTime;

        private void Start()
        {
            _root = UiBuilder.Rect("QuestTracker", transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(400f, 40f + MaxShown * 52f));
            UiBuilder.Panel(_root, new Color(0.075f, 0.06f, 0.11f, 0.8f), UiTheme.SlotBorder);
            RectTransform title = UiBuilder.Rect("Title", _root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -6f), new Vector2(370f, 32f));
            Text titleText = UiBuilder.Text("퀘스트", title, 20, TextAnchor.MiddleLeft, UiTheme.Accent);
            titleText.fontStyle = FontStyle.Bold;
            for (int i = 0; i < MaxShown; i++)
            {
                RectTransform line = UiBuilder.Rect("Line", _root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -40f - i * 52f), new Vector2(370f, 50f));
                Text text = UiBuilder.Text(string.Empty, line, 19, TextAnchor.UpperLeft, UiTheme.Text);
                text.lineSpacing = 1.1f;
                _lines.Add(text);
            }
        }

        private void Update()
        {
            // 매 프레임 목록을 만들 필요는 없다.
            _refreshTime -= Time.unscaledDeltaTime;
            if (_refreshTime > 0f)
            {
                return;
            }

            _refreshTime = 0.3f;
            QuestLog log = QuestLog.Current;
            var shown = new List<QuestDatabase.Quest>();
            if (log != null)
            {
                foreach (QuestDatabase.Quest quest in log.Active())
                {
                    if (quest.Kind != QuestKind.Deliver && quest.Kind != QuestKind.RepairPod && shown.Count < MaxShown)
                    {
                        shown.Add(quest);
                    }
                }
            }

            _root.gameObject.SetActive(shown.Count > 0);
            for (int i = 0; i < _lines.Count; i++)
            {
                if (i >= shown.Count)
                {
                    _lines[i].text = string.Empty;
                    continue;
                }

                QuestDatabase.Quest quest = shown[i];
                bool complete = log.IsComplete(quest, null);
                string progress = complete ? "<color=#7CF08F>완료 · 대기실에서 보고</color>" : log.Progress(quest, null);
                _lines[i].text = $"{quest.Title}\n<size=17><color=#B9AFC9>{Goal(quest)}</color>  {progress}</size>";
            }
        }

        private static string Goal(QuestDatabase.Quest quest)
        {
            switch (quest.Kind)
            {
                case QuestKind.Escape:
                    return "탈출";
                case QuestKind.KillAny:
                    return "로봇 처치";
                case QuestKind.KillRobot:
                    return quest.Robot != null ? $"{quest.Robot.DisplayName} 처치" : "처치";
                case QuestKind.KillNamed:
                    return "네임드 처치";
                case QuestKind.AllNamed:
                    return "네임드 모두 처치";
                case QuestKind.Visit:
                    ZoneArea zone = ZoneArea.Find(quest.ZoneId);
                    return zone != null ? $"{zone.DisplayName} 방문" : "방문";
                default:
                    return string.Empty;
            }
        }
    }
}
