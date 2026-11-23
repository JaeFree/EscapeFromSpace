using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EscapeFromSpace
{
    // 네임드 패턴 (기획서 21장). 기본 공격 사이에 일반·특수 패턴을 섞어 쓴다.
    // 패턴마다 내부 쿨이 있고 방금 쓴 패턴은 바로 다시 고르지 않는다. 상위 3기는 HP가 낮아지면 하위 보스의 특수 패턴을 빌려 쓴다.
    public class BossBrain : MonoBehaviour
    {
        // 몸의 판정 반경 (돌진할 때 벽에 닿는지 볼 때)
        private const float BodyRadius = 0.4f;

        // 탄의 크기
        private const float BulletScale = 0.3f;

        private static readonly Color CrateColor = new Color(0.62f, 0.45f, 0.25f, 1f);
        private static readonly Color GasColor = new Color(0.55f, 1f, 0.35f, 0.4f);
        private static readonly Color FireColor = new Color(1f, 0.5f, 0.15f, 0.5f);
        private static readonly Color ElectricColor = new Color(0.45f, 0.85f, 1f, 1f);
        private static readonly Color BulletColor = new Color(1f, 0.8f, 0.3f, 1f);
        private static readonly Color LaserColor = new Color(1f, 0.3f, 0.9f, 1f);
        private static readonly Color SmokeColor = new Color(0.6f, 0.6f, 0.65f, 0.75f);

        private class Slot
        {
            public BossPattern Pattern;
            public BossData Owner;
            public bool Borrowed;
            public float ReadyTime;
            public int Uses;
        }

        private readonly List<Slot> _slots = new List<Slot>();
        private readonly List<Slot> _candidates = new List<Slot>();
        private readonly List<GameObject> _temporary = new List<GameObject>();

        private Robot _robot;
        private BossData _data;
        private Rigidbody2D _body;
        private Slot _last;
        private Slot _current;
        private Coroutine _running;
        private float _nextAllowed;
        private bool _engaged;
        private float _stunAfter;

        public bool IsBusy => _current != null;
        public bool InLastStand { get; private set; }

        // 시험용: 지금 쓰는 패턴 이름, 지금까지 쓴 패턴 수
        public string CurrentPattern => _current != null ? _current.Pattern.DisplayName : null;
        public int PatternsUsed { get; private set; }

        private Health Player => _robot.Player;
        private Vector2 Position => _body.position;
        private Vector2 PlayerPosition => Player.transform.position;
        private Material LineMaterial => _robot.Attack.LineMaterial;

        public void Initialize(Robot robot, BossData data)
        {
            _robot = robot;
            _data = data;
            _body = GetComponent<Rigidbody2D>();
            _slots.Clear();
            AddSlot(data.Normal, data, false);
            AddSlot(data.Special, data, false);
        }

        private void AddSlot(BossPattern pattern, BossData owner, bool borrowed)
        {
            if (pattern != null)
            {
                _slots.Add(new Slot { Pattern = pattern, Owner = owner, Borrowed = borrowed });
            }
        }

        private void Update()
        {
            if (_data == null || InLastStand || !_data.HasLastStand || !_engaged)
            {
                return;
            }

            Health health = _robot.Health;
            if (health.MaxHp > 0f && health.CurrentHp / health.MaxHp <= _data.LastStandHpRatio && !health.IsDead)
            {
                EnterLastStand();
            }
        }

        // 마지막 발악: 하위 보스의 특수 패턴을 긴 내부 쿨로 더한다. 한꺼번에 쏟아지지 않게 처음 쓰는 때를 흩어 둔다.
        private void EnterLastStand()
        {
            InLastStand = true;
            foreach (BossData source in _data.LastStandSources)
            {
                if (source == null || source == _data)
                {
                    continue;
                }

                AddSlot(source.Special, source, true);
                _slots[_slots.Count - 1].ReadyTime = Time.time + Random.Range(2f, _data.BorrowedCooldown * 0.5f);
            }

            Sfx.PlayAt(SfxId.SentinelAlarm, Position);
            BossFx.Flash(Position, 2.5f, BossFx.WithAlpha(BossFx.Warning, 0.5f), 0.6f);
            Debug.Log($"[Boss] {_robot.Data.DisplayName} 마지막 발악");
        }

        // Robot이 사격 상태에서 부른다. 패턴을 시작했으면 true.
        public bool TryBegin(float distance)
        {
            if (IsBusy || Player == null || Player.IsDead)
            {
                return false;
            }

            float now = Time.time;
            if (!_engaged)
            {
                // 보자마자 패턴부터 쓰지 않게 첫 쿨을 둔다.
                _engaged = true;
                _slots[0].ReadyTime = now + _data.FirstNormalDelay;
                if (_slots.Count > 1)
                {
                    _slots[1].ReadyTime = now + _data.FirstSpecialDelay;
                }

                _nextAllowed = now;
                return false;
            }

            if (now < _nextAllowed)
            {
                return false;
            }

            _candidates.Clear();
            foreach (Slot slot in _slots)
            {
                BossPattern pattern = slot.Pattern;
                bool usesLeft = pattern.MaxUses <= 0 || slot.Uses < pattern.MaxUses;
                if (slot != _last && slot.ReadyTime <= now && usesLeft && distance <= pattern.UseDistance)
                {
                    _candidates.Add(slot);
                }
            }

            if (_candidates.Count == 0)
            {
                return false;
            }

            Run(_candidates[Random.Range(0, _candidates.Count)]);
            return true;
        }

        private void Run(Slot slot)
        {
            _current = slot;
            _last = slot;
            slot.Uses++;
            PatternsUsed++;
            _stunAfter = 0f;
            _running = StartCoroutine(Execute(slot));
        }

        private IEnumerator Execute(Slot slot)
        {
            yield return Pattern(slot.Pattern, slot.Owner);
            Finish();
            if (_stunAfter > 0f)
            {
                // 돌진하다 벽에 부딪혔다. 기절은 EMP와 같은 멈춤을 쓴다.
                _robot.Stun(_stunAfter);
            }
        }

        // EMP에 맞거나 끊어야 할 때. 쓰던 패턴의 예고와 몸 제어를 정리한다.
        public void Abort()
        {
            if (!IsBusy)
            {
                return;
            }

            if (_running != null)
            {
                StopCoroutine(_running);
            }

            Finish();
        }

        private void Finish()
        {
            if (_current == null)
            {
                return;
            }

            float now = Time.time;
            BossPattern pattern = _current.Pattern;
            float cooldown = _current.Borrowed ? Mathf.Max(pattern.Cooldown, _data.BorrowedCooldown) : pattern.Cooldown;
            _current.ReadyTime = now + cooldown;
            _nextAllowed = now + _data.MinGap;
            _current = null;
            _running = null;
            ClearTemporary();
            _robot.Motor.Suspended = false;
            _body.linearVelocity = Vector2.zero;
        }

        private void OnDestroy()
        {
            ClearTemporary();
        }

        private T Keep<T>(T component) where T : Component
        {
            _temporary.Add(component.gameObject);
            return component;
        }

        private void ClearTemporary()
        {
            foreach (GameObject temporary in _temporary)
            {
                if (temporary != null)
                {
                    Destroy(temporary);
                }
            }

            _temporary.Clear();
        }

        private Vector2 DirectionToPlayer()
        {
            Vector2 toPlayer = PlayerPosition - Position;
            return toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : _robot.Motor.Facing;
        }

        private void Face(Vector2 direction)
        {
            _robot.Motor.TurnTowards(direction);
        }

        private void Hit(float damage)
        {
            Player.TakeDamage(damage, PlayerPosition);
        }

        // ---------- 패턴 ----------

        private IEnumerator Pattern(BossPattern p, BossData owner)
        {
            switch (p.Kind)
            {
                case BossPatternKind.CrateThrow:
                    return CrateThrow(p);
                case BossPatternKind.Charge:
                    return Charge(p, false);
                case BossPatternKind.WeldDash:
                    return Charge(p, true);
                case BossPatternKind.FanShot:
                    return FanShot(p);
                case BossPatternKind.Summon:
                    return Summon(p, owner);
                case BossPatternKind.GasPool:
                    return GasPool(p);
                case BossPatternKind.Quarantine:
                    return Quarantine(p);
                case BossPatternKind.SpinSlash:
                    return SelfBlast(p, new Color(1f, 0.75f, 0.3f, 1f));
                case BossPatternKind.Discharge:
                    return SelfBlast(p, ElectricColor);
                case BossPatternKind.ChainLightning:
                    return ChainLightning(p);
                case BossPatternKind.Spiral:
                    return Spiral(p);
                case BossPatternKind.PiercingLaser:
                    return PiercingLaser(p);
                case BossPatternKind.TripleSnipe:
                    return TripleSnipe(p);
                case BossPatternKind.Artillery:
                    return Artillery(p);
                default:
                    return null;
            }
        }

        // 짐꾼 K-7: 상자가 날아가는 동안 떨어질 자리에 원이 진해진다.
        private IEnumerator CrateThrow(BossPattern p)
        {
            Vector2 from = Position;
            Vector2 target = PlayerPosition;
            Face(target - from);
            SpriteRenderer mark = Keep(BossFx.Disc(target, p.Radius, BossFx.WithAlpha(BossFx.Warning, 0.15f), BossFx.GroundOrder));
            SpriteRenderer crate = Keep(BossFx.Disc(from, 0.35f, CrateColor, BossFx.BulletOrder));
            Sfx.PlayAt(SfxId.RobotCharge, from);

            float time = 0f;
            float windup = Mathf.Max(0.1f, p.Windup);
            while (time < windup)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / windup);
                mark.color = BossFx.WithAlpha(BossFx.Warning, Mathf.Lerp(0.15f, 0.5f, t));

                // 위에서 본 화면이라 높이는 크기로 보인다.
                crate.transform.position = Vector2.Lerp(from, target, t);
                BossFx.SetRadius(crate, 0.35f + Mathf.Sin(t * Mathf.PI) * 0.35f);
                yield return null;
            }

            if (BossFx.InBlast(Player, target, p.Radius, _robot.WallMask))
            {
                Hit(p.Damage);
            }

            BossFx.Flash(target, p.Radius, BossFx.WithAlpha(CrateColor, 0.7f), 0.35f);
            Sfx.PlayAt(SfxId.WallImpact, target);
        }

        // 짐꾼 K-7 돌진, 용접공 E-5 용접 돌진. 앞부분은 플레이어를 따라 겨누고, 끝부분은 방향을 고정해서 옆으로 피할 수 있다.
        private IEnumerator Charge(BossPattern p, bool leavesFire)
        {
            Vector2 direction = DirectionToPlayer();
            LineRenderer line = Keep(BossFx.Line(LineMaterial, BossFx.WithAlpha(BossFx.Warning, 0.35f), p.Radius * 2f));
            Sfx.PlayAt(SfxId.RobotCharge, Position);

            float time = 0f;
            float windup = Mathf.Max(0.1f, p.Windup);
            bool locked = false;
            while (time < windup)
            {
                time += Time.deltaTime;
                if (time < windup * 0.6f)
                {
                    direction = DirectionToPlayer();
                }
                else if (!locked)
                {
                    locked = true;
                    Sfx.PlayAt(SfxId.RobotAimLock, Position);
                    BossFx.SetLine(line, BossFx.WithAlpha(BossFx.Warning, 0.7f), p.Radius * 2f);
                }

                Face(direction);
                line.SetPosition(0, Position);
                line.SetPosition(1, BossFx.BeamEnd(Position, direction, p.Length, _robot.WallMask));
                yield return null;
            }

            Destroy(line.gameObject);
            _robot.Motor.Stop();
            _robot.Motor.Suspended = true;
            Sfx.PlayAt(_robot.Data.AttackSound, Position);

            float travelled = 0f;
            float sinceFire = 0f;
            bool hitPlayer = false;
            bool hitWall = false;
            int stuckFrames = 0;
            while (travelled < p.Length)
            {
                float step = p.Speed * Time.fixedDeltaTime;
                RaycastHit2D wall = Physics2D.CircleCast(Position, BodyRadius, direction, step, _robot.WallMask);
                if (wall.collider != null)
                {
                    hitWall = true;
                    break;
                }

                Vector2 before = Position;
                _body.linearVelocity = direction * p.Speed;
                yield return new WaitForFixedUpdate();
                float moved = Vector2.Distance(before, Position);
                travelled += moved;

                // 무언가(다른 로봇 등)에 막혀 거의 못 갔다. 벽처럼 친다.
                stuckFrames = moved < step * 0.2f ? stuckFrames + 1 : 0;
                if (stuckFrames >= 3)
                {
                    hitWall = true;
                    break;
                }

                if (!hitPlayer && Vector2.Distance(PlayerPosition, Position) <= p.Radius + BodyRadius)
                {
                    hitPlayer = true;
                    Hit(p.Damage);
                }

                sinceFire += moved;
                if (leavesFire && sinceFire >= 0.7f)
                {
                    sinceFire = 0f;
                    BossHazard.Spawn(Position, 0.6f, 0f, p.SecondaryDamage, p.Duration, FireColor, Player, LineMaterial);
                }
            }

            _body.linearVelocity = Vector2.zero;
            _robot.Motor.Suspended = false;
            if (hitWall)
            {
                Sfx.PlayAt(SfxId.WallImpact, Position);
                if (!leavesFire)
                {
                    _stunAfter = p.Duration;
                }
            }
        }

        // 야간 경비 N-3: 부채꼴 선으로 예고한 뒤 눈에 보이는 탄을 부채꼴로 쏜다.
        private IEnumerator FanShot(BossPattern p)
        {
            int count = Mathf.Max(1, p.Count);
            var lines = new LineRenderer[count];
            for (int i = 0; i < count; i++)
            {
                lines[i] = Keep(BossFx.Line(LineMaterial, BossFx.WithAlpha(BossFx.Warning, 0.35f), 0.05f));
            }

            Sfx.PlayAt(SfxId.RobotCharge, Position);
            Vector2 direction = DirectionToPlayer();
            float time = 0f;
            while (time < p.Windup)
            {
                time += Time.deltaTime;
                direction = DirectionToPlayer();
                Face(direction);
                for (int i = 0; i < count; i++)
                {
                    Vector2 d = BossFx.Rotate(direction, FanAngle(i, count, p.Angle));
                    lines[i].SetPosition(0, Position);
                    lines[i].SetPosition(1, BossFx.BeamEnd(Position, d, 4f, _robot.WallMask));
                }

                yield return null;
            }

            Sfx.PlayAt(_robot.Data.AttackSound, Position);
            for (int i = 0; i < count; i++)
            {
                Vector2 d = BossFx.Rotate(direction, FanAngle(i, count, p.Angle));
                AcidSpit.Launch(Position + d * 0.5f, d, p.Speed, p.Length, p.Damage, _robot.Attack.ShotMask, BulletColor, BulletScale);
            }
        }

        private static float FanAngle(int index, int count, float totalAngle)
        {
            return count <= 1 ? 0f : Mathf.Lerp(-totalAngle * 0.5f, totalAngle * 0.5f, index / (float)(count - 1));
        }

        // 야간 경비 N-3: 경보를 울리고 둘레에 로봇을 부른다.
        private IEnumerator Summon(BossPattern p, BossData owner)
        {
            SpriteRenderer mark = Keep(BossFx.Disc(Position, 1.2f, BossFx.WithAlpha(BossFx.Warning, 0.3f), BossFx.GroundOrder));
            Sfx.PlayAt(SfxId.SentinelAlarm, Position);
            float time = 0f;
            while (time < p.Windup)
            {
                time += Time.deltaTime;
                mark.transform.position = Position;
                mark.color = BossFx.WithAlpha(BossFx.Warning, Mathf.Repeat(time * 4f, 1f) < 0.5f ? 0.45f : 0.15f);
                yield return null;
            }

            if (owner.SummonPrefab == null || owner.SummonData == null)
            {
                yield break;
            }

            for (int i = 0; i < p.Count; i++)
            {
                if (!TryFindFloor(Position, 1.5f, 3.5f, false, out Vector2 cell))
                {
                    continue;
                }

                Transform parent = transform.parent;
                var group = new GameObject("Summoned").transform;
                group.SetParent(parent, true);
                var start = new GameObject("Waypoint 0").transform;
                start.SetParent(group, false);
                start.position = cell;
                var home = new GameObject("Waypoint 1").transform;
                home.SetParent(group, false);
                home.position = Position;

                Robot robot = Instantiate(owner.SummonPrefab, cell, Quaternion.identity, group);
                robot.name = owner.SummonData.DisplayName;
                robot.Initialize(owner.SummonData, new[] { start, home }, _robot.Motor.Grid, Player);
                if (_robot.Zone != null)
                {
                    robot.ConfineTo(_robot.Zone);
                }

                robot.Hunt(PlayerPosition);
                BossFx.Flash(cell, 0.8f, BossFx.WithAlpha(BossFx.Warning, 0.5f), 0.4f);
            }
        }

        // 소독기 M-1: 플레이어가 선 자리에 가스 장판.
        private IEnumerator GasPool(BossPattern p)
        {
            Vector2 target = PlayerPosition;
            Face(target - Position);
            SpriteRenderer mark = Keep(BossFx.Disc(target, p.Radius, BossFx.WithAlpha(BossFx.Warning, 0.15f), BossFx.GroundOrder));
            Sfx.PlayAt(SfxId.RobotCharge, Position);
            float time = 0f;
            while (time < p.Windup)
            {
                time += Time.deltaTime;
                mark.color = BossFx.WithAlpha(BossFx.Warning, Mathf.Lerp(0.15f, 0.5f, time / Mathf.Max(0.01f, p.Windup)));
                yield return null;
            }

            BossHazard.Spawn(target, p.Radius, 0f, p.Damage, p.Duration, GasColor, Player, LineMaterial);
        }

        // 소독기 M-1: 플레이어 둘레를 가스 고리로 막고 산탄을 쏜다. 고리 안쪽은 안전하다.
        private IEnumerator Quarantine(BossPattern p)
        {
            Vector2 center = PlayerPosition;
            LineRenderer mark = Keep(BossFx.Ring(LineMaterial, center, p.Radius, BossFx.WithAlpha(BossFx.Warning, 0.2f), 0.15f));
            Sfx.PlayAt(SfxId.RobotCharge, Position);
            float time = 0f;
            while (time < p.Windup)
            {
                time += Time.deltaTime;
                BossFx.SetLine(mark, BossFx.WithAlpha(BossFx.Warning, Mathf.Lerp(0.2f, 0.7f, time / Mathf.Max(0.01f, p.Windup))), 0.15f);
                yield return null;
            }

            Destroy(mark.gameObject);
            BossHazard.Spawn(center, p.Radius + 0.4f, p.Radius - 0.4f, p.Damage, p.Duration, GasColor, Player, LineMaterial);

            for (int burst = 0; burst < Mathf.Max(1, p.Repeat); burst++)
            {
                Vector2 direction = DirectionToPlayer();
                Face(direction);
                Sfx.PlayAt(_robot.Data.AttackSound, Position);
                int pellets = Mathf.Max(1, p.Count);
                for (int i = 0; i < pellets; i++)
                {
                    Vector2 d = BossFx.Rotate(direction, FanAngle(i, pellets, p.Angle));
                    AcidSpit.Launch(Position + d * 0.5f, d, p.Speed, p.Length, p.SecondaryDamage, _robot.Attack.ShotMask, BulletColor, BulletScale);
                }

                yield return new WaitForSeconds(p.Interval);
            }
        }

        // 용접공 E-5 회전 베기, 과부하 P-X 방전: 자기 둘레 원 예고 뒤 한 번에 터진다.
        private IEnumerator SelfBlast(BossPattern p, Color color)
        {
            SpriteRenderer mark = Keep(BossFx.Disc(Position, p.Radius, BossFx.WithAlpha(BossFx.Warning, 0.1f), BossFx.GroundOrder));
            Sfx.PlayAt(SfxId.RobotCharge, Position);
            float time = 0f;
            while (time < p.Windup)
            {
                time += Time.deltaTime;
                mark.transform.position = Position;
                mark.color = BossFx.WithAlpha(BossFx.Warning, Mathf.Lerp(0.1f, 0.45f, time / Mathf.Max(0.01f, p.Windup)));
                yield return null;
            }

            if (BossFx.InBlast(Player, Position, p.Radius, _robot.WallMask))
            {
                Hit(p.Damage);
            }

            BossFx.Flash(Position, p.Radius, BossFx.WithAlpha(color, 0.6f), 0.3f);
            Sfx.PlayAt(_robot.Data.AttackSound, Position);
        }

        // 과부하 P-X: 십자 번개가 천천히 돈다. 줄 사이로 들어가거나 멀리 빠지면 피한다.
        private IEnumerator ChainLightning(BossPattern p)
        {
            int count = Mathf.Max(1, p.Count);
            var lines = new LineRenderer[count];
            for (int i = 0; i < count; i++)
            {
                lines[i] = Keep(BossFx.Line(LineMaterial, BossFx.WithAlpha(BossFx.Warning, 0.3f), 0.06f));
            }

            // 처음에는 플레이어를 피해 줄 사이에 두고 시작한다 (예고를 보고 바로 피할 틈이 있게).
            Vector2 toPlayer = DirectionToPlayer();
            float angle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg + 180f / count;
            Sfx.PlayAt(SfxId.RobotCharge, Position);
            float time = 0f;
            while (time < p.Windup)
            {
                time += Time.deltaTime;
                PlaceBeams(lines, angle, p.Length);
                yield return null;
            }

            for (int i = 0; i < count; i++)
            {
                BossFx.SetLine(lines[i], ElectricColor, p.Radius * 2f);
            }

            float tick = 0f;
            float sound = 0f;
            time = 0f;
            while (time < p.Duration)
            {
                time += Time.deltaTime;
                angle += p.Speed * Time.deltaTime;
                PlaceBeams(lines, angle, p.Length);

                sound -= Time.deltaTime;
                if (sound <= 0f)
                {
                    sound = 0.5f;
                    Sfx.PlayAt(_robot.Data.AttackSound, Position);
                }

                tick -= Time.deltaTime;
                if (tick <= 0f)
                {
                    tick = 0.25f;
                    foreach (LineRenderer line in lines)
                    {
                        if (BossFx.DistanceToSegment(PlayerPosition, line.GetPosition(0), line.GetPosition(1)) <= p.Radius)
                        {
                            Hit(p.Damage * 0.25f);
                            break;
                        }
                    }
                }

                yield return null;
            }
        }

        private void PlaceBeams(LineRenderer[] lines, float angle, float length)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                float a = (angle + i * 360f / lines.Length) * Mathf.Deg2Rad;
                var direction = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                lines[i].SetPosition(0, Position);
                lines[i].SetPosition(1, BossFx.BeamEnd(Position, direction, length, _robot.WallMask));
            }
        }

        // 감시자 R-9: 제자리에서 돌며 나선형으로 탄을 뿌린다.
        private IEnumerator Spiral(BossPattern p)
        {
            LineRenderer mark = Keep(BossFx.Ring(LineMaterial, Position, 1.2f, BossFx.WithAlpha(BossFx.Warning, 0.6f), 0.1f));
            Sfx.PlayAt(SfxId.RobotCharge, Position);
            yield return new WaitForSeconds(p.Windup);
            Destroy(mark.gameObject);

            Vector2 toPlayer = DirectionToPlayer();
            float angle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
            int arms = Mathf.Max(1, p.Count);
            float time = 0f;
            int shot = 0;
            float interval = Mathf.Max(0.03f, p.Interval);
            while (time < p.Duration)
            {
                for (int i = 0; i < arms; i++)
                {
                    float a = (angle + i * 360f / arms) * Mathf.Deg2Rad;
                    var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    AcidSpit.Launch(Position + d * 0.5f, d, p.Speed, p.Length, p.Damage, _robot.Attack.ShotMask, BulletColor, BulletScale);
                }

                if (shot % 3 == 0)
                {
                    Sfx.PlayAt(_robot.Data.AttackSound, Position);
                }

                shot++;
                angle += p.Angle;
                Face(new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)));
                time += interval;
                yield return new WaitForSeconds(interval);
            }
        }

        // 감시자 R-9: 몇 줄의 예고 뒤 벽까지 닿는 빔. 마지막 부분에는 방향이 고정된다.
        private IEnumerator PiercingLaser(BossPattern p)
        {
            int count = Mathf.Max(1, p.Count);
            var lines = new LineRenderer[count];
            for (int i = 0; i < count; i++)
            {
                lines[i] = Keep(BossFx.Line(LineMaterial, BossFx.WithAlpha(BossFx.Warning, 0.3f), 0.05f));
            }

            Sfx.PlayAt(SfxId.RobotCharge, Position);
            Vector2 direction = DirectionToPlayer();
            float time = 0f;
            bool locked = false;
            while (time < p.Windup)
            {
                time += Time.deltaTime;
                if (time < p.Windup * 0.7f)
                {
                    direction = DirectionToPlayer();
                }
                else if (!locked)
                {
                    locked = true;
                    Sfx.PlayAt(SfxId.RobotAimLock, Position);
                    foreach (LineRenderer line in lines)
                    {
                        BossFx.SetLine(line, BossFx.WithAlpha(BossFx.Warning, 0.8f), 0.12f);
                    }
                }

                Face(direction);
                SetSpread(lines, direction, p.Angle, p.Length);
                yield return null;
            }

            Sfx.PlayAt(_robot.Data.AttackSound, Position);
            bool hit = false;
            foreach (LineRenderer line in lines)
            {
                Vector2 from = line.GetPosition(0);
                Vector2 to = line.GetPosition(1);
                if (!hit && BossFx.DistanceToSegment(PlayerPosition, from, to) <= p.Radius)
                {
                    hit = true;
                    Hit(p.Damage);
                }

                LineRenderer beam = BossFx.Line(LineMaterial, LaserColor, p.Radius * 2f);
                beam.SetPosition(0, from);
                beam.SetPosition(1, to);
                beam.gameObject.AddComponent<BossFade>().Begin(0.3f);
            }
        }

        private void SetSpread(LineRenderer[] lines, Vector2 direction, float angle, float length)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                float offset = (i - (lines.Length - 1) * 0.5f) * angle;
                Vector2 d = BossFx.Rotate(direction, offset);
                lines[i].SetPosition(0, Position);
                lines[i].SetPosition(1, BossFx.BeamEnd(Position, d, length, _robot.WallMask));
            }
        }

        // 감시탑 S-0: 조준선 → 고정 → 발사를 짧게 세 번.
        private IEnumerator TripleSnipe(BossPattern p)
        {
            LineRenderer line = Keep(BossFx.Line(LineMaterial, BossFx.WithAlpha(BossFx.Warning, 0.4f), 0.03f));
            for (int shot = 0; shot < Mathf.Max(1, p.Repeat); shot++)
            {
                Sfx.PlayAt(SfxId.RobotCharge, Position);
                Vector2 direction = DirectionToPlayer();
                bool locked = false;
                BossFx.SetLine(line, BossFx.WithAlpha(BossFx.Warning, 0.4f), 0.03f);
                line.enabled = true;
                float time = 0f;
                while (time < p.Windup)
                {
                    time += Time.deltaTime;
                    if (time < p.Windup * 0.6f)
                    {
                        direction = DirectionToPlayer();
                    }
                    else if (!locked)
                    {
                        locked = true;
                        Sfx.PlayAt(SfxId.RobotAimLock, Position);
                        BossFx.SetLine(line, BossFx.WithAlpha(BossFx.Warning, 1f), 0.07f);
                    }

                    Face(direction);
                    line.SetPosition(0, Position);
                    line.SetPosition(1, BossFx.BeamEnd(Position, direction, p.Length, _robot.WallMask));
                    yield return null;
                }

                line.enabled = false;
                Vector2 end = BossFx.BeamEnd(Position, direction, p.Length, _robot.WallMask);
                if (BossFx.DistanceToSegment(PlayerPosition, Position, end) <= p.Radius)
                {
                    Hit(p.Damage);
                }

                LineRenderer tracer = BossFx.Line(LineMaterial, Color.white, 0.08f);
                tracer.SetPosition(0, Position);
                tracer.SetPosition(1, end);
                tracer.gameObject.AddComponent<BossFade>().Begin(0.15f);
                Sfx.PlayAt(_robot.Data.AttackSound, Position);
                yield return new WaitForSeconds(p.Interval);
            }
        }

        // 감시탑 S-0: 플레이어 둘레에 차례로 포탄이 떨어지고, 그 틈에 연막을 치고 자리를 옮긴다.
        private IEnumerator Artillery(BossPattern p)
        {
            Sfx.PlayAt(SfxId.SentinelAlarm, Position);
            for (int i = 0; i < Mathf.Max(1, p.Count); i++)
            {
                // 첫 포탄은 선 자리에 떨어진다. 서 있으면 맞으니 움직이게 하려는 것이다.
                Vector2 target = i == 0 ? PlayerPosition : PlayerPosition + Random.insideUnitCircle * p.Length;
                BossBlast.Spawn(target, p.Radius, p.Windup, p.Damage, Player, _robot.WallMask);
                yield return new WaitForSeconds(p.Interval);
            }

            yield return new WaitForSeconds(p.Windup);

            BossFx.Flash(Position, 1.6f, SmokeColor, 1.2f);
            if (TryFindFloor(Position, 6f, 12f, true, out Vector2 cell))
            {
                _body.position = cell;
                transform.position = cell;
                BossFx.Flash(cell, 1.2f, SmokeColor, 1f);
            }
        }

        // 근처의 빈 바닥 칸. 구역에 묶인 네임드는 구역 안에서만 찾는다.
        private bool TryFindFloor(Vector2 around, float min, float max, bool needPath, out Vector2 cell)
        {
            for (int i = 0; i < 60; i++)
            {
                Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(min, max);
                Vector2 candidate = new Vector2(Mathf.Floor(around.x + offset.x) + 0.5f, Mathf.Floor(around.y + offset.y) + 0.5f);
                if (_robot.Zone != null && !_robot.Zone.Contains(candidate))
                {
                    continue;
                }

                if (Physics2D.OverlapCircle(candidate, 0.45f, _robot.WallMask) != null)
                {
                    continue;
                }

                // 부른 로봇은 벽 너머에 생기지 않게 보이는 곳에, 옮겨 가는 자리는 걸어서 닿는 곳에 둔다.
                if (!needPath && Physics2D.Linecast(around, candidate, _robot.WallMask).collider != null)
                {
                    continue;
                }

                if (needPath && (_robot.Motor.Grid == null || !_robot.Motor.Grid.FindPath(around, candidate, _pathBuffer)))
                {
                    continue;
                }

                cell = candidate;
                return true;
            }

            cell = around;
            return false;
        }

        private readonly List<Vector2> _pathBuffer = new List<Vector2>();
    }

    // 정해진 시간 뒤 터지는 폭발 (포격). 보스가 부서지거나 패턴이 끊겨도 이미 떨어진 포탄은 터진다.
    public class BossBlast : MonoBehaviour
    {
        private SpriteRenderer _mark;
        private float _radius;
        private float _delay;
        private float _time;
        private float _damage;
        private Health _target;
        private LayerMask _wallMask;

        public static void Spawn(Vector2 center, float radius, float delay, float damage, Health target, LayerMask wallMask)
        {
            SpriteRenderer mark = BossFx.Disc(center, radius, BossFx.WithAlpha(BossFx.Warning, 0.15f), BossFx.GroundOrder);
            var blast = mark.gameObject.AddComponent<BossBlast>();
            blast.name = "BossBlast";
            blast._mark = mark;
            blast._radius = radius;
            blast._delay = Mathf.Max(0.1f, delay);
            blast._damage = damage;
            blast._target = target;
            blast._wallMask = wallMask;
            Sfx.PlayAt(SfxId.RobotCharge, center);
        }

        private void Update()
        {
            _time += Time.deltaTime;
            _mark.color = BossFx.WithAlpha(BossFx.Warning, Mathf.Lerp(0.15f, 0.55f, _time / _delay));
            if (_time < _delay)
            {
                return;
            }

            Vector2 center = transform.position;
            if (_target != null && !_target.IsDead && BossFx.InBlast(_target, center, _radius, _wallMask))
            {
                _target.TakeDamage(_damage, _target.transform.position);
            }

            BossFx.Flash(center, _radius, new Color(1f, 0.6f, 0.2f, 0.7f), 0.35f);
            Sfx.PlayAt(SfxId.RobotDestroyed, center);
            Destroy(gameObject);
        }
    }
}
