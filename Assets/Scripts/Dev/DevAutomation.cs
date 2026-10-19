using System;
using System.Collections;
using System.IO;
using Fishing.Angling;
using Fishing.Core;
using Fishing.Data;
using UnityEngine;

namespace Fishing.Dev
{
    public struct DevArgs
    {
        public bool Enabled;
        public string ShotDir;
        /// <summary>-autoshotStop 이름: 그 장면을 찍고 바로 끝낸다 (한 장면만 빨리 확인할 때).</summary>
        public string StopAfter;
        /// <summary>-autoshotMaps id,id: 낚시터 둘러보기(CoastTour)에서 그 맵 장면만 찍고 끝낸다 (새 맵을 빨리 확인할 때).</summary>
        public string Maps;
    }

    /// <summary>
    /// 빌드한 플레이어를 `-autoshot &lt;폴더&gt;` 인자로 실행하면 주요 장면을 자동으로 연출하고 스크린샷을 찍은 뒤 종료한다.
    /// 세이브 파일은 건드리지 않는다. (Game.SaveDisabled)
    /// </summary>
    public class DevAutomation : MonoBehaviour
    {
        DevArgs _args;
        float _fps;   // 최근 프레임의 평균 FPS (지수 이동 평균, 촬영 로그용)
        string[] _tourOnly; // -autoshotMaps: 둘러보기에서 찍을 맵

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt <= 0f) return;
            _fps = _fps <= 0f ? 1f / dt : Mathf.Lerp(_fps, 1f / dt, 0.1f);
        }

        public static DevArgs Parse()
        {
            var result = new DevArgs();
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-autoshot")
                {
                    result.Enabled = true;
                    result.ShotDir = args[i + 1];
                }
                else if (args[i] == "-autoshotStop") result.StopAfter = args[i + 1];
                else if (args[i] == "-autoshotMaps") result.Maps = args[i + 1];
            return result;
        }

        public void Init(DevArgs args)
        {
            _args = args;
            Directory.CreateDirectory(_args.ShotDir);
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            // 인트로: 사업자 로고 · 타이틀을 찍고 넘어간다
            var intro = Fishing.UI.IntroSequence.Instance;
            if (intro != null)
            {
                yield return new WaitUntil(() => intro.Current == Fishing.UI.IntroSequence.Stage.Company && intro.T >= 3.6f);
                yield return Shot("00_intro_company");
                yield return new WaitUntil(() => intro.Ready);
                yield return new WaitForSecondsRealtime(1.5f);
                yield return Shot("00_intro_title");
                intro.Continue();
                yield return new WaitUntil(() => Fishing.UI.IntroSequence.Instance == null);
            }
            if (!string.IsNullOrEmpty(_args.Maps))
            {
                _tourOnly = _args.Maps.Split(',');
                yield return CoastTour();
                Debug.Log("[Dev] 자동 스크린샷 완료 (-autoshotMaps " + _args.Maps + ")");
                Application.Quit();
                yield break;
            }

            var p = Game.Profile;
            p.Data.level = 6;
            p.Data.gold = 8400;
            p.AddItem("bait_corn", 20);
            p.AddItem("bait_minnow", 5);

            Game.Clock.Set(1, 10 * 60);
            yield return new WaitForSeconds(3f);
            yield return Shot("00a_lobby");
            Game.UI.OpenExclusive(Game.UI.Areas);
            yield return new WaitForSeconds(0.5f);
            yield return Shot("00b_area_list");
            Game.UI.EnterField(1);
            Game.UI.Help.Close();
            Game.Clock.Set(1, 6 * 60);
            yield return new WaitForSeconds(2f);
            yield return Shot("01_field_morning");

            // 캐릭터 정면 클로즈업 (한낮)
            Game.Weather.Force(Weather.Clear);
            Game.Clock.Set(1, 12 * 60);
            Game.CameraRig.Yaw = Game.Player.Yaw + 180f;
            Game.CameraRig.Pitch = 8f;
            Game.CameraRig.Distance = 3.2f;
            yield return new WaitForSeconds(1.5f);
            // 접힌 낚싯대를 끝 마디부터 펼치는 중간 (빗자루 길이 → 다 펼침)
            Game.Player.CollapseRod();
            yield return new WaitForSeconds(0.3f);
            Game.Player.ExtendRod();
            yield return new WaitForSeconds(0.5f);
            yield return Shot("00c_rod_extend");
            yield return new WaitForSeconds(1.5f);
            yield return Shot("00_character");

            // 걷기·달리기 (Human Basic Motions): 옆에서
            Game.Player.Teleport(new Vector3(-75f, 1.1f, -64f), 90f);
            Game.CameraRig.Yaw = 0f;
            Game.CameraRig.Pitch = 6f;
            Game.CameraRig.Distance = 4.5f;
            Game.Player.DebugMove = new Vector2(1f, 0f);
            yield return new WaitForSeconds(1.2f);
            yield return Shot("00d_walk");
            Game.Player.DebugRun = true;
            yield return new WaitForSeconds(1.2f);
            yield return Shot("00e_run");
            Game.Player.DebugMove = Vector2.zero;
            Game.Player.DebugRun = false;
            yield return new WaitForSeconds(1.2f);
            yield return Shot("00f_idle");
            // 둔치 공원 나무 (Nature Starter Kit 2 활엽수 + Kenney 소나무)
            Game.Player.Teleport(new Vector3(-40f, 1.1f, -74f), 180f);
            Game.CameraRig.Yaw = 200f;
            Game.CameraRig.Pitch = 10f;
            Game.CameraRig.Distance = 9f;
            yield return new WaitForSeconds(1f);
            yield return Shot("00g_park_trees");
            // 서울 스카이라인: 데크 끝에서 북동쪽 남산서울타워·롯데월드타워, 북서쪽 63빌딩
            Game.Player.Teleport(new Vector3(25f, 1.1f, -30f), 15f);
            Game.CameraRig.Yaw = 22f;
            Game.CameraRig.Pitch = -4f;
            Game.CameraRig.Distance = 7f;
            yield return new WaitForSeconds(1.5f);
            yield return Shot("00q_skyline_namsan_lotte");
            Game.CameraRig.Yaw = -50f;
            yield return new WaitForSeconds(1f);
            yield return Shot("00r_skyline_63");
            // CU 편의점 (자전거도로 남쪽)
            var cu = Fishing.World.HanRiverMap.StorePos;
            Game.Player.Teleport(new Vector3(cu.x + 3f, 1.1f, cu.z + 13f), 180f);
            Game.CameraRig.Yaw = 200f;
            Game.CameraRig.Pitch = 8f;
            Game.CameraRig.Distance = 9f;
            yield return new WaitForSeconds(1.5f);
            yield return Shot("00t_cu_store");

            // 받은 캐릭터 (ExtraNpcs): 버스킹 무대의 낚시 아이돌 유니티짱, 밤의 좀비·귀신
            if (Fishing.Characters.CharacterLibrary.Lib != null)
            {
                float day0 = Game.Clock.Minutes;
                Game.Player.Teleport(new Vector3(10f, 1.1f, -65f), 180f);
                Game.CameraRig.Yaw = 150f;
                Game.CameraRig.Pitch = 8f;
                Game.CameraRig.Distance = 5f;
                yield return new WaitForSeconds(2.2f);
                yield return Shot("00h_idol_greeting");
                // 무대 멀리서 (인사하지 않고 춤·포즈 중)
                Game.Player.Teleport(new Vector3(19f, 1.1f, -62f), -138f);
                Game.CameraRig.Yaw = -138f;
                Game.CameraRig.Pitch = 10f;
                Game.CameraRig.Distance = 4f;
                yield return new WaitForSeconds(1.5f);
                yield return Shot("00i_idol_stage");
                // 공원 벤치(아이스박스)에서 수다 떠는 두 사람
                Game.Player.Teleport(new Vector3(-46.5f, 1.1f, -80.2f), -90f);
                Game.CameraRig.Yaw = -70f;
                Game.CameraRig.Pitch = 8f;
                Game.CameraRig.Distance = 5f;
                yield return new WaitForSeconds(2f);
                yield return Shot("00l_park_chat");
                // 산책하는 의사: 걷는 발이 미끄러지지 않는지 0.25초 간격 두 장
                var doctor = GameObject.Find("Wanderer_doctor");
                if (doctor != null)
                {
                    var dp = doctor.transform.position;
                    Game.Player.Teleport(new Vector3(dp.x + 1.5f, 1.1f, dp.z - 5.5f), 0f);
                    Game.CameraRig.Yaw = -12f;
                    Game.CameraRig.Pitch = 6f;
                    Game.CameraRig.Distance = 3.5f;
                    yield return new WaitForSeconds(1.2f);
                    yield return Shot("00o_doctor_walk");
                    yield return new WaitForSeconds(0.25f);
                    yield return Shot("00o_doctor_walk_b");
                }
                // 데크 입구: 데크 판과 둔치 땅·석축이 만나는 곳
                Game.Player.Teleport(new Vector3(19.5f, 1.1f, -64.5f), 40f);
                Game.CameraRig.Yaw = 40f;
                Game.CameraRig.Pitch = 22f;
                Game.CameraRig.Distance = 4f;
                yield return new WaitForSeconds(1.2f);
                yield return Shot("00p_pier_entrance");
                // 밤 스카이라인 (창 불빛·푸른 남산타워·흰 왕관)과 별
                Game.Clock.Set(Game.Clock.Day, 22 * 60);
                var weather0 = Game.Weather.Current;
                Game.Weather.Force(Weather.Clear);
                Game.Player.Teleport(new Vector3(25f, 1.1f, -30f), 15f);
                Game.CameraRig.Yaw = 22f;
                Game.CameraRig.Pitch = -5f;
                Game.CameraRig.Distance = 7f;
                yield return new WaitForSeconds(3f);
                yield return Shot("00s_skyline_night");
                var cuN = Fishing.World.HanRiverMap.StorePos;
                Game.Player.Teleport(new Vector3(cuN.x + 3f, 1.1f, cuN.z + 13f), 180f);
                Game.CameraRig.Yaw = 200f;
                Game.CameraRig.Pitch = 8f;
                Game.CameraRig.Distance = 9f;
                yield return new WaitForSeconds(1.5f);
                yield return Shot("00u_cu_night");
                // 별이 총총한 밤하늘 (위를 올려다본다)
                Game.Player.Teleport(new Vector3(25f, 1.1f, -40f), 0f);
                Game.CameraRig.Yaw = 0f;
                Game.CameraRig.Pitch = -5f;
                Game.CameraRig.Distance = 3f;
                yield return new WaitForSeconds(1.5f);
                yield return Shot("00v_night_stars");
                Game.Weather.Force(weather0);
                Game.Clock.Set(Game.Clock.Day, 23 * 60);
                Game.Player.Teleport(new Vector3(-4f, 1.1f, -64.5f), 200f);
                Game.CameraRig.Yaw = 170f;
                Game.CameraRig.Pitch = 10f;
                Game.CameraRig.Distance = 6f;
                yield return new WaitForSeconds(3f);
                yield return Shot("00j_night_zombie");
                Game.Player.Teleport(new Vector3(Fishing.World.HanRiverMap.BridgeX + 15f, 1.1f, Fishing.World.HanRiverMap.RiverMinZ - 10f), -34f);
                Game.CameraRig.Yaw = -34f;
                Game.CameraRig.Pitch = 6f;
                Game.CameraRig.Distance = 5f;
                yield return new WaitForSeconds(3.5f);
                yield return Shot("00k_night_ghost");
                Game.Player.Teleport(new Vector3(27f, 1.1f, -70f), 0f);
                Game.CameraRig.Yaw = 10f;
                Game.CameraRig.Pitch = 8f;
                Game.CameraRig.Distance = 6f;
                yield return new WaitForSeconds(2.5f);
                yield return Shot("00m_night_dancer");
                Game.Player.Teleport(new Vector3(-75f, 1.1f, -67.5f), -90f);
                Game.CameraRig.Yaw = -80f;
                Game.CameraRig.Distance = 6f;
                yield return new WaitForSeconds(3f);
                yield return Shot("00n_night_clown");
                Game.Clock.Set(Game.Clock.Day, day0);
            }
            Game.CameraRig.Yaw = Game.Player.Yaw;
            Game.CameraRig.Distance = 7f;

            // 어군: 데크 앞 물 위에 오래 머무는 하얀 소용돌이
            Game.Weather.Force(Weather.Clear);
            Game.Clock.Set(1, 10 * 60);
            Game.Player.Teleport(new Vector3(25f, 1.1f, -29f), 0f);
            var school = Game.Schools.Schools[0];
            school.Spot = new Vector3(22f, Fishing.World.FieldMap.WaterY, -15f);
            Fishing.World.FieldMap.TryCell(school.Spot, out school.Col, out school.Row);
            school.Hidden = false;
            school.MoveAt = Time.time + 600f;
            Game.CameraRig.Yaw = 0f;
            Game.CameraRig.Pitch = 24f;
            Game.CameraRig.Distance = 7f;
            yield return new WaitForSeconds(8f); // 옛 자리에서 사라졌다 새 자리에 떠오르기까지
            yield return Shot("01b_fish_school");

            // 원작 영상처럼 밤의 낚시 데크에서
            Game.Clock.Set(1, 21 * 60);
            Game.Player.Teleport(new Vector3(25f, 1.1f, -29f), 0f);
            Game.CameraRig.Yaw = -20f;
            Game.CameraRig.Pitch = 14f;
            Game.CameraRig.Distance = 5.5f;
            yield return new WaitForSeconds(2f);

            // 캐스팅 제스처 (당겼다가 미는 중)
            Game.Fishing.DebugAim(0.62f, 10f);
            yield return new WaitForSeconds(0.3f);
            yield return Shot("02_casting");
            Game.Fishing.DebugResume();
            CheckClear("DebugCast");
            Game.Fishing.DebugCast(15.5f);
            yield return new WaitForSeconds(0.45f);
            yield return Shot("02b_cast_flight");
            yield return new WaitForSeconds(2.05f);
            yield return Shot("03_waiting");
            // 찌를 감는 중: 오른손은 낚싯대를 쥔 채 그대로, 왼손이 릴 손잡이를 돌린다
            Game.Fishing.DebugReeling = true;
            yield return new WaitForSeconds(0.5f);
            yield return Shot("03b_reeling");
            var rig0 = (Game.CameraRig.Yaw, Game.CameraRig.Pitch, Game.CameraRig.Distance);
            Game.Fishing.UserOrbited = true;
            Game.CameraRig.Yaw = Game.Player.Yaw + 150f;
            Game.CameraRig.Pitch = 8f;
            Game.CameraRig.Distance = 2.2f;
            yield return new WaitForSeconds(2f);
            yield return Shot("03c_reeling_close");
            Game.Fishing.DebugReeling = false;
            // 찌를 기다리며 낚싯대를 눕히면 줄이 처져 물에 눕고, 세우면 팽팽하다 (옆에서)
            Game.CameraRig.Yaw = Game.Player.Yaw + 95f;
            Game.CameraRig.Pitch = 6f;
            Game.CameraRig.Distance = 9f;
            Game.Fishing.DebugRodAim(8f);
            yield return new WaitForSeconds(1.5f);
            yield return Shot("03d_rod_low_slack");
            Game.Fishing.DebugRodAim(76f);
            yield return new WaitForSeconds(1.2f);
            yield return Shot("03e_rod_high_taut");
            Game.Fishing.DebugRodAim(28f);
            Game.Fishing.UserOrbited = false;
            (Game.CameraRig.Yaw, Game.CameraRig.Pitch, Game.CameraRig.Distance) = rig0;
            yield return new WaitForSeconds(0.3f);

            // 본신: 찌가 잠기는 챔질 게이지
            Game.Fishing.DebugForceBite("carp", 82f);
            // 원작 0:36.5: 머리 위 빨간 느낌표 → 0:39.5 어깨 뒤 낮은 챔질 클로즈업
            yield return new WaitForSeconds(0.2f);
            yield return Shot("04a_bite_alert");
            yield return new WaitForSeconds(0.65f);
            yield return Shot("04_bite_gauge");

            // 자동 조작: 찌가 완전히 잠기면 흔들어 PERFECT 챔질 → 파이팅
            Game.Fishing.Autoplay = true;
            float timeout = Time.time + 5f;
            while (Game.Fishing.State != FishingState.Fighting && Time.time < timeout) yield return null;
            yield return new WaitForSeconds(0.25f);
            yield return Shot("05_hook_perfect");

            var fight = Game.Fishing.Fight;
            if (fight == null)
            {
                Debug.Log("[Dev] 입질이 파이팅으로 이어지지 않아 강제로 시작합니다.");
                CheckClear("DebugStartFight");
                Game.Fishing.DebugStartFight("carp", 82f, 18f);
                fight = Game.Fishing.Fight;
            }
            yield return new WaitForSeconds(2f);
            yield return Shot("06_fight");

            // 장력 위험: 화면 가장자리가 빨갛게
            for (float t = 0f; t < 0.35f; t += Time.deltaTime)
            {
                fight.DebugSetTension(0.96f);
                yield return null;
            }
            yield return Shot("06b_tension_danger");
            fight.DebugSetTension(0.5f);

            // 카운터 찬스 집중선 (모서리까지 덮이는지)
            Fishing.UI.FightHudView.DebugForceCounterChance = true;
            yield return new WaitForSeconds(0.3f);
            yield return Shot("06c_speedlines");
            Fishing.UI.FightHudView.DebugForceCounterChance = false;

            // 포커싱 (Q): 장력이 느리게 변하고 데미지가 커진다. 콤보 칸 위 금색 막대
            fight.Focus();
            p.UseFocus();
            yield return new WaitForSeconds(0.6f);
            yield return Shot("06d_focus");

            bool countered = false, chanceShot = false;
            fight.Countered += () => countered = true;
            Game.Fishing.AutoplayCounterChance = 0f;
            timeout = Time.time + 25f;
            while (!countered && Time.time < timeout && Game.Fishing.State == FishingState.Fighting)
            {
                if (!chanceShot && fight.CounterChance)
                {
                    chanceShot = true;
                    yield return new WaitForSeconds(0.15f);
                    yield return Shot("07_counter_chance");
                    Game.Fishing.AutoplayCounterChance = 0.5f;
                }
                yield return null;
            }
            if (countered) yield return Shot("08_counter");

            timeout = Time.time + 120f;
            bool landingShot = false;
            while (Game.Fishing.State == FishingState.Fighting && Time.time < timeout)
            {
                if (!landingShot && fight.Landing && fight.LandingDistance < 3.2f)
                {
                    landingShot = true;
                    yield return Shot("09_landing");
                }
                yield return null;
            }
            Debug.Log($"[Dev] 파이팅 종료: 어종={fight.Species.Name} {fight.Length:0.0}cm, 상태={Game.Fishing.State}, 경과={fight.Elapsed:0.0}s, HP={fight.HP:0}/{fight.MaxHP:0}, 결과={fight.Outcome}");
            if (Game.Fishing.State == FishingState.Result)
            {
                // 원작 1:53~1:55: 1인칭으로 낚싯대를 들어 올리고 → 3인칭 옆모습, 줄 끝에 매단 고기
                yield return new WaitForSecondsRealtime(0.6f);
                yield return Shot("09c_lift");
                yield return new WaitForSecondsRealtime(1.3f);
                yield return Shot("09d_catch");
            }
            yield return WaitResult();
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Shot("10_result");
            yield return CloseResult();

            // 원작 RESULT 오른쪽 말풍선 토스트(노랑 보상형·하늘색 기록형) · BONUS 보상 · 이벤트 어종 EXP +200% · 파이팅 시간 · 등급
            {
                var rs = FishDatabase.Get("carp");
                var demo = new CatchRewards.Outcome { BonusGold = 460, BonusExp = 1910, EventPct = 200, FightSeconds = 67f, Grade = CatchRewards.Grade(rs) };
                demo.Toasts.Add(new CatchRewards.Toast { Title = "첫 월척 물고기", Body = "처음으로 대물급 잉어를 낚았습니다.", Gold = 230, Exp = 955, Reward = true });
                demo.Toasts.Add(new CatchRewards.Toast { Title = "5가지 물고기", Body = "지금까지 5가지 물고기를 낚았습니다.", Gold = 230, Exp = 955, Reward = true });
                demo.Toasts.Add(new CatchRewards.Toast { Title = "연속 공격!", Body = "가장 많은 카운터를 친 파이팅이었습니다. 신규 기록: 7회 / 기존 기록: 5회" });
                demo.Toasts.Add(new CatchRewards.Toast { Title = "최고 기록", Body = "이전 최고 기록 61.20cm를 72.40cm로 갱신했습니다." });
                Game.UI.Result.Show(new CaughtFish { speciesId = "carp", length = 72.4f, weight = Mathf.Round(rs.WeightGrams(72.4f)), day = 1, zone = "C2" },
                    3 * rs.Exp, true, 61.2f, 1, false, false, demo, _ => { });
                yield return new WaitForSecondsRealtime(1.9f);
                yield return Shot("10f_result_rewards");
                yield return CloseResult();
            }

            // 원작 실패: 위쪽 WARNING 카드(내구도·줄 길이 감소) + 오른쪽 아래 "재도전 하시겠습니까?" → 오른쪽 위 X2 재도전
            Game.UI.Hud.ShowWarning("낚싯줄이 끊어졌습니다! 장력이 HIGH를 넘지 않게 조심하세요.",
                "낚싯대 내구도 감소 : 1.00 %  릴 내구도 감소 : 2.00 %  줄 길이 감소 : 7.00m");
            CatchRewards.OnLost(FishDatabase.Get("carp"), 72.4f);
            Game.UI.Hud.OfferRetry("잉어", CatchRewards.Mult);
            yield return new WaitForSecondsRealtime(0.7f);
            yield return Shot("10g_warning_retry");
            Game.UI.Hud.AcceptRetry();
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Shot("10h_retry_badge");
            CatchRewards.DebugClear();

            // 작은 고기: 결과 화면에서 캐릭터가 왼손에 들어 보인다 (원작 1:56~1:58)
            CheckClear("DebugStartFight");
            Game.Fishing.Autoplay = true;
            Game.Fishing.DebugStartFight("crucian", 30f, 7f);
            timeout = Time.time + 60f;
            while (Game.Fishing.State == FishingState.Fighting && Time.time < timeout) yield return null;
            if (Game.Fishing.State == FishingState.Result)
            {
                yield return new WaitForSecondsRealtime(2.1f);
                yield return Shot("09e_catch_small");
            }
            yield return WaitResult();
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Shot("10b_result_held");
            yield return CloseResult();
            // 손가락만 한 고기(빙어 12cm): 손에 들어 보이는 모양
            CheckClear("DebugStartFight");
            Game.Fishing.DebugStartFight("smelt", 12f, 3f);
            timeout = Time.time + 60f;
            while (Game.Fishing.State == FishingState.Fighting && Time.time < timeout) yield return null;
            yield return WaitResult();
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Shot("10b2_result_smelt");
            yield return CloseResult();
            Game.Fishing.Autoplay = false;
            // 길쭉한 고기·잡동사니 결과 화면 (뱀장어는 몸이 S자로 꿈틀, 운동화는 3D 모델이 화면 안에)
            foreach (var (id, len, shot) in new[] { ("eel", 92f, "10c_result_eel"), ("shoe", 27f, "10d_result_shoe"), ("trap", 45f, "10e_result_trap") })
            {
                var s = FishDatabase.Get(id);
                Game.UI.Result.Show(new CaughtFish { speciesId = id, length = len, weight = Mathf.Round(s.WeightGrams(len)), day = 1, zone = "B1" }, s.Exp, false, 0f, 1, false, false, null, _ => { });
                yield return new WaitForSecondsRealtime(1.2f);
                yield return Shot(shot);
                yield return CloseResult();
            }
            yield return new WaitForSeconds(0.3f);

            // 도감/살림망/퀘스트/낚시점 화면을 채워서 찍는다
            string[] sample = { "crucian", "zacco", "bluegill", "kkeuri", "nuchi", "catfish", "bass" };
            for (int i = 0; i < sample.Length; i++)
            {
                var s = FishDatabase.Get(sample[i]);
                for (int k = 0; k <= i % 6; k++)
                {
                    float len = BiteSelector.RollLength(s, default);
                    p.RecordDex(s, len, s.WeightGrams(len), 1);
                    if (p.Data.keepNet.Count < 8) p.Data.keepNet.Add(new CaughtFish { speciesId = s.Id, length = len, weight = Mathf.Round(s.WeightGrams(len)), day = 1, zone = "E1" });
                }
            }
            Game.Quests.DebugSetStory(2); // 이야기 "도감 채우기"를 받은 상태 (도감 7종이라 바로 완료 가능)
            p.NotifyChanged();

            yield return new WaitForSeconds(0.5f);
            Game.UI.OpenExclusive(Game.UI.Dex);
            yield return new WaitForSeconds(0.8f); // 차트 등장 애니메이션이 끝나게
            yield return Shot("11_dex");
            Game.UI.Dex.ShowDetail(FishDatabase.Get("carp"));
            yield return new WaitForSeconds(1.2f);
            yield return Shot("11b_dex_detail");
            Game.UI.Dex.ShowDetail(null);
            Game.UI.OpenExclusive(Game.UI.KeepNet);
            yield return new WaitForSeconds(0.8f); // 차트 등장 애니메이션이 끝나게
            yield return Shot("12_keepnet");
            // 납품 창 (원작 납품소 DELIVERY): 게이지 87%, 월척 붕어 한 마리(도장), 4마리 고른 상태
            float gaugeBefore = p.Data.deliveryGauge;
            p.Data.deliveryGauge = 0.87f;
            var wolcheok = FishDatabase.Get("crucian");
            p.Data.keepNet.Add(new CaughtFish { speciesId = "crucian", length = 34.5f, weight = Mathf.Round(wolcheok.WeightGrams(34.5f)), day = 1, zone = "E1", mapId = "hangang" });
            Game.UI.OpenExclusive(Game.UI.Delivery);
            Game.UI.Delivery.DebugSelect(4);
            yield return new WaitForSeconds(0.4f);
            yield return Shot("12b_delivery");
            Game.UI.Delivery.Close();
            p.Data.deliveryGauge = gaugeBefore;
            // ===== 퀘스트 (원작 13 문서): NPC [퀘스트 목록보기] → 상세 [수락] → 여러 퀘스트 동시 진행 (HUD QUEST LIST · 퀘스트 책) =====
            var quests = Game.Quests;
            string hanUnion = QuestNpcs.UnionOf("hangang");
            Game.UI.QuestTalk.OpenFor(hanUnion);
            yield return new WaitForSeconds(0.4f);
            yield return Shot("13q_npc_quest_list");
            var offer = quests.AvailableAt(hanUnion);
            if (offer.Count > 0)
            {
                Game.UI.QuestTalk.ShowDetail(offer[0]);
                yield return new WaitForSeconds(0.3f);
                yield return Shot("13r_quest_accept");
            }
            // 가이드·일일반복(연락관)과 일반(낚시점)도 받아 두고, 몇 개는 조금 진행시킨다
            foreach (var q in quests.AvailableAt(hanUnion)) quests.Accept(q);
            foreach (var q in quests.AvailableAt(QuestNpcs.ShopOf("hangang"))) quests.Accept(q);
            quests.OnReleased();
            Game.UI.QuestTalk.Close();
            yield return new WaitForSeconds(0.6f);
            yield return Shot("13s_hud_questlist");
            Game.UI.Quest.ShowTab(0);
            Game.UI.OpenExclusive(Game.UI.Quest);
            yield return Shot("13_quest");
            Game.UI.Quest.ShowTab(1);
            yield return Shot("13b_daily");
            Game.UI.Quest.ShowTab(3);
            yield return Shot("13c_achievements");
            // 생태조사 (원작 기록 책 "홍현 생태조사 (16/23)"): 한강 어종 몇 개를 이미 납품해 완성(하나는 보상 받음), 하나는 방금 납품해 알림이 뜬 상태
            var hanEco = System.Linq.Enumerable.ToList(EcoSurvey.OfMap("hangang"));
            for (int i = 0; i < hanEco.Count && i < 5; i++)
            {
                p.Data.deliveries.Add(new DeliveryRecord { mapId = "hangang", speciesId = hanEco[i].Species.Id, count = 1 + i });
                p.Data.achClaims.Add(new AchClaim { id = hanEco[i].Id, doneAt = $"2026/10/{12 + i} 2{i}:1{i}", claimed = i == 0 });
            }
            p.Data.deliveredTotal = Mathf.Max(p.Data.deliveredTotal, 130);
            if (hanEco.Count > 5)
            {
                var fresh = hanEco[5].Species;
                EcoSurvey.OnDelivered(new System.Collections.Generic.List<CaughtFish>
                    { new CaughtFish { speciesId = fresh.Id, length = fresh.MinLen + 5f, weight = Mathf.Round(fresh.WeightGrams(fresh.MinLen + 5f)), day = 1, zone = "E1", mapId = "hangang" } });
            }
            Game.UI.Quest.ShowAchievements(EcoSurvey.CatEco, "hangang");
            yield return new WaitForSeconds(0.4f);
            yield return Shot("13g_eco_survey");
            // 빙고: 첫 줄과 대각선 몇 칸을 채운 상태
            Bingo.EnsureCard();
            foreach (int i in new[] { 0, 1, 2, 3, 6, 18, 24 }) p.Data.bingo.done[i] = true;
            Game.UI.Quest.ShowTab(5);
            yield return new WaitForSeconds(0.3f);
            yield return Shot("13e_bingo");
            p.Log("자동 촬영: 일지 예시 — 한강 둔치에서 첫 출조");
            Game.UI.Quest.ShowTab(6);
            yield return Shot("13f_journal");
            Game.UI.Quest.ShowTab(0);
            // 완료: [보고자] 박주임에게 [완 료] → 가운데 완료 편지 카드 + 시스템 줄 EXP/GOLD, 목록은 다음 이야기로
            var doneStory = quests.Current;
            Game.UI.QuestTalk.OpenFor(hanUnion);
            if (doneStory != null && quests.IsReady(doneStory)) Game.UI.QuestTalk.ShowDetail(doneStory);
            yield return new WaitForSeconds(0.3f);
            yield return Shot("13t_quest_complete_detail");
            if (doneStory != null && quests.Finish(doneStory, hanUnion)) Game.UI.QuestTalk.ShowDetail(null);
            yield return new WaitForSecondsRealtime(0.7f);
            yield return Shot("13u_quest_complete_card");
            Game.UI.QuestTalk.Close();
            yield return new WaitForSecondsRealtime(2.8f); // 카드가 사라지게
            Game.UI.OpenExclusive(Game.UI.Exchange);
            yield return Shot("13d_exchange");
            Game.UI.OpenExclusive(Game.UI.Shop);
            yield return Shot("14_shop");
            Game.UI.Shop.Close();
            Game.UI.OpenExclusive(Game.UI.Help);
            yield return Shot("16_help");
            Game.UI.Help.Close();
            Game.UI.OpenExclusive(Game.UI.Options);
            yield return new WaitForSeconds(0.3f);
            yield return Shot("16b_options");
            Game.UI.Options.Close();

            // 루어 낚시: 스푼 루어를 던지고 감으면서 저킹 ("액션!")
            p.AddItem("bait_lure", 10);
            p.SelectBait("bait_lure");
            Game.Player.Teleport(new Vector3(25f, 1.1f, -29f), 0f);
            Game.CameraRig.Yaw = -10f;
            Game.CameraRig.Pitch = 16f;
            Game.CameraRig.Distance = 6f;
            yield return new WaitForSeconds(0.5f);
            CheckClear("DebugCast");
            Game.Fishing.DebugCast(16f);
            yield return new WaitForSeconds(2.2f);
            Game.Fishing.DebugTwitch();
            yield return new WaitForSeconds(0.15f);
            yield return Shot("05c_lure_action");
            Game.Fishing.ForceIdle();
            p.SelectBait("bait_worm");

            // 낚시 대회: 참가해서 몇 마리 낚은 상태 (HUD 순위표 + 퀘스트 창 대회 탭). 끝나면 시각을 되돌린다
            float clockBefore = Game.Clock.Minutes;
            Game.Clock.Set(1, 12 * 60);
            Game.Tournament.DebugJoin(4, 63.5f, "잉어", 151.2f);
            yield return new WaitForSeconds(0.6f);
            yield return Shot("30_tournament_hud");
            Game.UI.Quest.ShowTab(4);
            Game.UI.OpenExclusive(Game.UI.Quest);
            yield return new WaitForSeconds(0.4f);
            yield return Shot("31_tournament_rank");
            Game.UI.Quest.Close();
            Game.UI.Quest.ShowTab(0);
            p.Data.tournament.day = -1;
            Game.Clock.Set(1, clockBefore);

            // 필드 납품소: 조합 연락관 옆 빨간 기계 + 초록 마름모 + "납품소" 이름표
            var kiosk = Fishing.World.DeliveryKiosk.All.Count > 0 ? Fishing.World.DeliveryKiosk.All[0] : null;
            if (kiosk != null)
            {
                float faceKiosk = kiosk.transform.eulerAngles.y + 180f;
                Game.Player.Teleport(kiosk.transform.position + kiosk.transform.forward * 2.2f, faceKiosk);
                Game.CameraRig.Yaw = faceKiosk + 25f;
                Game.CameraRig.Pitch = 12f;
                Game.CameraRig.Distance = 5f;
                yield return new WaitForSeconds(0.8f);
                yield return Shot("12c_delivery_kiosk");
            }

            // NPC 대화 (낚시점 김사장), 낚싯대 강화 창
            Game.Player.Teleport(Fishing.World.FieldMap.ShopPos + new Vector3(1.2f, 0.1f, 4.5f), 180f);
            Game.CameraRig.Yaw = 200f;
            Game.CameraRig.Distance = 5f;
            yield return new WaitForSeconds(0.6f);
            foreach (var npc in Fishing.Characters.NpcAngler.Talkable)
                if (npc.NpcId == "shop") { Game.UI.TalkTo(npc); break; }
            yield return new WaitForSeconds(0.8f);
            yield return Shot("20_npc_dialog");
            p.Data.gold += 50000;
            p.AddItem(RodUpgrade.ScaleId, 30);
            p.AddItem(RodUpgrade.StarSandId, 2);
            for (int i = 0; i < 4; i++) p.TryUpgrade(GearSlot.Rod, false);
            Game.UI.OpenExclusive(Game.UI.Upgrade);
            yield return new WaitForSeconds(0.8f);
            yield return Shot("21_rod_upgrade");
            Game.UI.Upgrade.Close();
            Game.UI.OpenExclusive(Game.UI.KeepNet);
            yield return new WaitForSeconds(0.2f);
            Game.Toast(p.Dissect(0) ?? "", ToastKind.Good);
            yield return new WaitForSeconds(0.2f);
            Game.UI.OpenExclusive(Game.UI.Inventory);
            yield return new WaitForSeconds(0.3f);
            yield return Shot("21b_inventory");
            Game.UI.Inventory.Close();

            // 의상: 밀짚모자, 선글라스, 체크 남방, 구명조끼, 장화
            p.Data.level = Mathf.Max(p.Data.level, 6);
            foreach (var id in new[] { "hat_straw", "glass_sun", "top_check", "vest_life", "shoes_boots" })
            {
                p.GiveCostume(id);
                p.Wear(CostumeDatabase.Get(id));
            }
            Game.UI.Character.ShowTab(true);
            Game.UI.OpenExclusive(Game.UI.Character);
            Fishing.UI.PortraitCamera.FullBody.Spin = 25f;
            yield return new WaitForSeconds(0.8f);
            yield return Shot("21c_costume");
            Game.UI.Character.Close();
            Game.UI.Character.ShowTab(false);
            // 일반 슬롯: 장비 6칸 이름·미끼 개수가 읽히는지
            Game.UI.OpenExclusive(Game.UI.Character);
            yield return new WaitForSeconds(0.5f);
            yield return Shot("21d_character_gear");
            Game.UI.Character.Close();

            // 지역 지도(M): 어군 하나는 3마리를 낚아 작은 어군으로 만든다
            var shrink = Game.Schools.Schools[1];
            for (int i = 0; i < Fishing.World.FishSchoolManager.CatchesToShrink; i++) Game.Schools.OnCatch(shrink, shrink.SpeciesIds[0]);
            Game.UI.OpenExclusive(Game.UI.AreaMap);
            yield return new WaitForSeconds(0.3f);
            yield return Shot("17_area_map");
            // 히든 어군(금빛) · 포식자 어군(붉은빛)
            var hiddenSchool = Game.Schools.DebugAppear(Fishing.World.SchoolKind.Hidden);
            var predatorSchool = Game.Schools.DebugAppear(Fishing.World.SchoolKind.Predator);
            Game.UI.AreaMap.Refresh();
            yield return new WaitForSeconds(0.4f);
            yield return Shot("17b_hidden_predator");
            if (hiddenSchool != null) hiddenSchool.Hidden = true;
            if (predatorSchool != null) predatorSchool.Hidden = true;
            Game.UI.AreaMap.Close();

            // 배: 낚시 데크 끝에서 고무보트를 꺼내 노을 진 강으로
            Game.Weather.Force(Weather.Clear);
            Game.Clock.Set(1, 17 * 60 + 40);
            p.Data.level = Mathf.Max(p.Data.level, 3);
            p.Data.level = Mathf.Max(p.Data.level, 10);
            if (!p.Owns("boat_speed")) p.Data.ownedGear.Add("boat_speed");
            p.Equip(GearDatabase.GetGear("boat_speed"));
            Game.Player.Teleport(new Vector3(25f, 1.1f, -27.5f), 0f);
            Game.CameraRig.Yaw = 0f;
            Game.CameraRig.Pitch = 14f;
            Game.CameraRig.Distance = 8f;
            yield return new WaitForSeconds(1f);
            // 원작 소환: 먼바다 쪽에서 배가 물보라를 끌며 달려와 물가에서 돌아 선다 → 엔진 START/STOP [Y]
            Game.Boat.InstantLaunch = false;
            Game.Boat.Toggle();
            yield return new WaitForSeconds(1.1f);
            yield return Shot("18a_boat_summon");
            float summonUntil = Time.time + 5f;
            while (Game.Boat.Summoning && Time.time < summonUntil) yield return null;
            yield return new WaitForSeconds(0.8f);
            yield return Shot("18b_boat_engine");
            Game.Boat.InstantLaunch = true;
            Game.Boat.DebugThrottle = 1f;
            Game.Boat.DebugSteer = 0.25f;
            yield return new WaitForSeconds(3.5f);
            yield return Shot("18_boat_drive");
            Game.Boat.DebugThrottle = 0f;
            Game.Boat.DebugSteer = 0f;
            yield return new WaitForSeconds(3.5f);
            Game.CameraRig.Yaw = Game.Player.Yaw + 150f;
            Game.CameraRig.Distance = 6f;
            yield return new WaitForSeconds(1f);
            yield return Shot("19_boat_stop");
            Game.Boat.DebugThrottle = float.NaN;
            Game.Boat.StowToLand();

            // 흐린 낮 (층구름이 하늘을 덮는다)
            Game.Player.Teleport(new Vector3(-70f, 1.1f, -64f), 20f);
            Game.CameraRig.Yaw = 20f;
            Game.Clock.Set(1, 14 * 60);
            Game.Weather.Force(Weather.Cloudy);
            yield return new WaitForSeconds(6f);
            yield return Shot("15b_cloudy_day");

            // 비 오는 밤
            Game.Player.Teleport(new Vector3(-70f, 1.1f, -64f), 20f);
            Game.CameraRig.Yaw = 20f;
            Game.Clock.Set(1, 21 * 60 + 30);
            Game.Weather.Force(Weather.Rain);
            yield return new WaitForSeconds(5f);
            yield return Shot("15_night_rain");

            // ================= 수족관 (대기실) =================
            Game.UI.EnterLobby();
            Game.Clock.Set(2, 11 * 60);
            foreach (var (sid, len) in new[] { ("carp", 64f), ("goldkoi", 70f), ("mandarin", 41f) })
                p.Data.aquarium.Add(new AquaFish { speciesId = sid, length = len, fedAt = Game.Clock.TotalMinutes - (sid == "carp" ? 500f : 0f) });
            p.Data.aquariumDecor.AddRange(new[] { "coral_red", "seaweed", "coral_pink", "shell", "seaweed", "rock" });
            Game.Lobby.Tank.Rebuild();
            Game.UI.OpenExclusive(Game.UI.Aquarium);
            yield return new WaitForSeconds(1.5f);
            yield return Shot("28_aquarium");
            Game.UI.Aquarium.Close();
            yield return new WaitForSeconds(0.3f);
            yield return Shot("29_lobby_aquarium");
            // 대기실 [퀘스트]: 조합 직원 카드 줄(머리 위 !) + 왼쪽 대화 카드
            Game.UI.QuestTalk.OpenLobby();
            yield return new WaitForSeconds(0.5f);
            yield return Shot("29b_lobby_quest");
            Game.UI.QuestTalk.Close();

            // ================= 홍현 앞바다 =================
            p.Data.level = Mathf.Max(p.Data.level, 10);
            Game.Weather.Force(Weather.Clear);
            Game.Clock.Set(2, 15 * 60);
            // 원작 로딩 화면(이름·소개·팁·진행 막대) → 입장하면 날씨 알림과 채널 입장 시스템 줄
            Game.UI.StartCoroutine(Game.UI.EnterWithLoading(Fishing.Data.FishingAreas.Korea.Find(a => a.Id == "honghyeon"), 2));
            yield return new WaitForSecondsRealtime(1.4f);
            yield return Shot("21z_loading");
            while (Game.UI.Loading.Showing) yield return null;
            yield return new WaitForSecondsRealtime(0.6f);
            yield return Shot("21za_weather_notice");
            Game.UI.Help.Close();
            var bw = Fishing.World.HonghyeonDef.Breakwater;
            Game.Player.Teleport(new Vector3(bw.center.x, 1.1f, bw.yMin + 6f), 0f);
            Game.CameraRig.Yaw = -15f;
            Game.CameraRig.Pitch = 12f;
            Game.CameraRig.Distance = 9f;
            yield return new WaitForSeconds(3f);
            yield return Shot("22_honghyeon_day");
            // ESC 메뉴 (원작 2:50)
            Game.UI.OpenExclusive(Game.UI.EscMenu);
            yield return new WaitForSeconds(0.3f);
            yield return Shot("22a_esc_menu");
            Game.UI.EscMenu.Close();
            // 방파제 끝에서 마을 쪽: 해변 비탈, 포구 호안, 다랭이논 지형
            Game.Player.Teleport(new Vector3(bw.center.x, 1.1f, bw.yMax - 6f), 180f);
            Game.CameraRig.Yaw = 200f;
            Game.CameraRig.Pitch = 16f;
            Game.CameraRig.Distance = 12f;
            yield return new WaitForSeconds(1.5f);
            yield return Shot("22b_honghyeon_village");
            // 서쪽 갯바위 섬 위
            var isl = Fishing.World.HonghyeonDef.Islands[0];
            Game.Player.Teleport(new Vector3(isl.x - 3f, 1.1f, isl.z - 6f), 30f);
            Game.CameraRig.Yaw = 40f;
            Game.CameraRig.Pitch = 18f;
            Game.CameraRig.Distance = 10f;
            yield return new WaitForSeconds(1.5f);
            yield return Shot("22c_honghyeon_island");
            Game.Player.Teleport(new Vector3(bw.center.x, 1.1f, bw.yMin + 6f), 0f);

            Game.Clock.Set(2, 18 * 60 + 20);
            Game.Player.Teleport(new Vector3(bw.center.x, 1.1f, bw.yMax - 6f), 330f);
            Game.CameraRig.Yaw = 150f;
            Game.CameraRig.Pitch = 8f;
            Game.CameraRig.Distance = 11f;
            yield return new WaitForSeconds(3f);
            yield return Shot("23_honghyeon_sunset");

            Game.UI.OpenExclusive(Game.UI.AreaMap);
            yield return new WaitForSeconds(0.4f);
            yield return Shot("24_honghyeon_map");
            Game.UI.AreaMap.Close();

            // 통발 (원작 0:42~0:48, 2:20~2:26): 어부 대화 → 설치 확인 → 다 된 통발 수거 → 아이템 획득
            {
                Game.Clock.Set(2, 16 * 60);
                p.Data.level = Mathf.Max(p.Data.level, 12);
                Fishing.Characters.NpcAngler trapFisher = null;
                foreach (var npc in Fishing.Characters.NpcAngler.Talkable)
                    if (npc != null && npc.NpcId == FishTraps.FisherId) { trapFisher = npc; break; }
                if (trapFisher != null)
                {
                    var fisherPos = trapFisher.transform.position;
                    Game.Player.Teleport(fisherPos + trapFisher.transform.forward * 2.2f + trapFisher.transform.right * 0.6f, trapFisher.transform.eulerAngles.y + 180f);
                    yield return new WaitForSeconds(0.6f);
                    Game.UI.TalkTo(trapFisher);
                    yield return new WaitForSeconds(0.8f);
                    yield return Shot("22d_trap_npc");
                    p.Data.traps.Clear();
                    Game.UI.Dialog.Close();
                    Game.UI.Traps.OpenFrom(trapFisher.Look.Name);
                    Game.UI.Traps.DebugConfirm();
                    yield return new WaitForSeconds(0.5f);
                    yield return Shot("22e_trap_install");
                    FishTraps.DebugInstallReady(Fishing.World.FieldMap.Id);
                    Game.UI.Traps.OpenFrom(trapFisher.Look.Name);
                    yield return new WaitForSeconds(0.5f);
                    yield return Shot("22f_trap_harvest");
                    var trapReady = p.Data.traps.Find(FishTraps.Ready);
                    var trapItems = trapReady != null ? FishTraps.Harvest(trapReady, new System.Collections.Generic.HashSet<int> { 0, 1, 2 }) : null;
                    if (trapItems != null && trapItems.Count > 0)
                    {
                        Game.UI.ItemBag.Show(trapItems);
                        yield return new WaitForSeconds(0.5f);
                        yield return Shot("22g_item_bag");
                    }
                    Game.UI.ItemBag.Close();
                    Game.UI.Traps.Close();
                }
                else Debug.LogWarning("[Dev] 통발 어부 없음");
            }

            // 서쪽 갯바위 섬 근처를 배로
            Game.Clock.Set(2, 10 * 60);
            Game.Player.Teleport(new Vector3(bw.center.x - 6f, 1.1f, -42f), 0f);
            yield return new WaitForSeconds(0.5f);
            Game.Boat.InstantLaunch = true;
            Game.Boat.Toggle();
            Game.Boat.DebugThrottle = 1f;
            Game.Boat.DebugSteer = -0.35f;
            Game.CameraRig.Yaw = 0f;
            Game.CameraRig.Distance = 10f;
            yield return new WaitForSeconds(4f);
            yield return Shot("25_honghyeon_boat");
            Game.Boat.DebugThrottle = float.NaN;
            Game.Boat.StowToLand();

            // 방파제 끝에서 감성돔 파이팅
            Game.Clock.Set(2, 17 * 60 + 30);
            Game.Weather.Force(Weather.Cloudy);
            Game.Player.Teleport(new Vector3(bw.xMax - 1.3f, 1.1f, bw.yMax - 9f), 70f);
            Game.CameraRig.Yaw = 70f;
            yield return new WaitForSeconds(1f);
            Game.Fishing.Autoplay = true;
            CheckClear("DebugStartFight");
            Game.Fishing.DebugStartFight("blackseabream", 55f, 16f);
            yield return new WaitForSeconds(2.5f);
            yield return Shot("26_sea_fight");
            var seaFight = Game.Fishing.Fight;
            float seaTimeout = Time.time + 60f;
            while (Game.Fishing.State == FishingState.Fighting && Time.time < seaTimeout) yield return null;
            if (seaFight != null) Debug.Log($"[Dev] 바다 파이팅 종료: {seaFight.Species.Name} {seaFight.Length:0.0}cm, 결과={seaFight.Outcome}");
            yield return WaitResult();
            yield return Shot("27_sea_result");
            yield return CloseResult();
            Game.Fishing.Autoplay = false;

            // ================= 서천 앞바다 (서해, 물때) =================
            Game.Weather.Force(Weather.Clear);
            Game.Clock.Set(3, MinuteAtTide(3, 9 * 60, 15 * 60, true));   // 낮 만조 무렵
            Game.UI.EnterField(Fishing.Data.FishingAreas.Korea.Find(a => a.Id == "seocheon"), 1);
            Game.UI.Help.Close();
            Game.Player.Teleport(new Vector3(8f, 1.1f, -50f), 20f);
            Game.CameraRig.Yaw = 25f;
            Game.CameraRig.Pitch = 14f;
            Game.CameraRig.Distance = 11f;
            yield return new WaitForSeconds(3f);
            yield return Shot("34_seocheon_harbor");

            // 썰물: 춘장대 갯벌이 드러난다
            Game.Clock.Set(3, MinuteAtTide(3, 6 * 60, 18 * 60, false));
            yield return new WaitForSeconds(0.5f);
            Game.Player.Teleport(new Vector3(-110f, 1.2f, -30f), 0f);
            Game.CameraRig.Yaw = 15f;
            Game.CameraRig.Pitch = 16f;
            Game.CameraRig.Distance = 10f;
            yield return new WaitForSeconds(1.5f);
            yield return Shot("35_seocheon_lowtide");

            // 동백정 (오후), 그리고 서해 노을 (해 지기 직전)
            Game.Clock.Set(3, 15 * 60 + 30);
            var cape = Fishing.World.SeocheonDef.CapeBase;
            float capeY = Fishing.World.SeocheonTerrain.HeightAt(cape.x, 2f);
            Game.Player.Teleport(new Vector3(cape.x - 2f, capeY + 0.2f, 2f), 0f);
            Game.CameraRig.Yaw = 170f;
            Game.CameraRig.Pitch = 8f;
            Game.CameraRig.Distance = 9f;
            yield return new WaitForSeconds(1.5f);
            yield return Shot("36_seocheon_pavilion");
            Game.Clock.Set(3, 17 * 60 + 40);
            Game.CameraRig.Yaw = -25f;
            Game.CameraRig.Pitch = 4f;
            yield return new WaitForSeconds(0.8f);
            yield return Shot("36b_seocheon_sunset");

            // 돛단배와 낚시 크루저 (홍원항에서 출발)
            p.Data.level = Mathf.Max(p.Data.level, 14);
            foreach (var (boatId, shot, secs) in new[] { ("boat_sail", "39_seocheon_sail", 5f), ("boat_cruiser", "40_seocheon_cruiser", 6f) })
            {
                if (!p.Data.ownedGear.Contains(boatId)) p.Data.ownedGear.Add(boatId);
                p.Equip(GearDatabase.GetGear(boatId));
                Game.Clock.Set(4, 11 * 60);
                Game.Player.Teleport(new Vector3(20f, 1.1f, -43f), 0f);
                yield return new WaitForSeconds(0.5f);
                Game.Boat.InstantLaunch = true;
                Game.Boat.Toggle();
                Game.Boat.DebugThrottle = 1f;
                Game.Boat.DebugSteer = boatId == "boat_sail" ? 0.25f : -0.1f;
                Game.CameraRig.Yaw = 0f;
                Game.CameraRig.Distance = boatId == "boat_cruiser" ? 16f : 11f;
                yield return new WaitForSeconds(secs);
                yield return Shot(shot);
                Game.Boat.DebugThrottle = float.NaN;
                Game.Boat.StowToLand();
            }

            // 빨간 등대 방파제에서 주꾸미
            Game.Clock.Set(4, 10 * 60);
            var arm = Fishing.World.SeocheonDef.RedArm;
            Game.Player.Teleport(new Vector3(arm.center.x, 1.1f, arm.yMax - 2f), 0f);
            yield return new WaitForSeconds(1f);
            Game.Fishing.Autoplay = true;
            CheckClear("DebugStartFight");
            Game.Fishing.DebugStartFight("octopus", 26f, 12f);
            yield return new WaitForSeconds(2f);
            yield return Shot("37_seocheon_fight");
            var westFight = Game.Fishing.Fight;
            float westTimeout = Time.time + 60f;
            while (Game.Fishing.State == FishingState.Fighting && Time.time < westTimeout) yield return null;
            if (westFight != null) Debug.Log($"[Dev] 서천 파이팅 종료: {westFight.Species.Name} {westFight.Length:0.0}cm, 결과={westFight.Outcome}");
            yield return WaitResult();
            yield return Shot("38_seocheon_result");
            yield return CloseResult();
            Game.Fishing.Autoplay = false;

            // ================= 해안 맵 8곳 (CoastMapDef) =================
            yield return CoastTour();

            // ================= 겨울: 한강 얼음낚시 =================
            int winterDay = Seasons.FirstDayOf(Season.Winter, Game.Clock.Day) + 3;
            Game.Clock.Set(winterDay, 13 * 60);
            Game.Weather.Force(Weather.Rain); // 겨울엔 눈
            Game.UI.EnterField(Fishing.Data.FishingAreas.Korea.Find(a => a.Id == "hangang"), 1);
            Game.UI.Help.Close();
            yield return new WaitForSeconds(1.5f);
            Game.Player.Teleport(new Vector3(-30f, 0.5f, -53.5f), 0f); // 뗏목(콜라이더) 바로 앞 얼음 위
            Game.CameraRig.Yaw = 20f;
            Game.CameraRig.Pitch = 18f;
            Game.CameraRig.Distance = 7f;
            yield return new WaitForSeconds(1.5f);
            Game.Fishing.DebugIceFish();
            yield return new WaitForSeconds(2f);
            yield return Shot("41_winter_ice");
            Game.Fishing.DebugForceBite("smelt", 13.5f);
            Game.Fishing.Autoplay = true;
            float iceTimeout = Time.time + 40f;
            while (Game.Fishing.State != FishingState.Result && Game.Fishing.State != FishingState.Idle && Time.time < iceTimeout) yield return null;
            yield return WaitResult();
            yield return Shot("42_winter_smelt");
            yield return CloseResult();
            Game.Fishing.Autoplay = false;

            Debug.Log("[Dev] 자동 스크린샷 완료");
            Application.Quit();
        }

        /// <summary>새 낚시터 8곳을 차례로 들어가 대표 장면을 찍는다 (위치, 카메라 yaw·pitch·거리, 시각).</summary>
        IEnumerator CoastTour()
        {
            var p = Game.Profile;
            p.Data.level = Mathf.Max(p.Data.level, 40);
            Game.Weather.Force(Weather.Clear);
            var shots = new (string map, string shot, Vector3 pos, float yaw, float camYaw, float pitch, float dist, int minute, Weather weather)[]
            {
                ("yeongheung", "43_yeongheung_harbor", new Vector3(62f, 1.1f, -47f), 0f, 20f, 14f, 14f, 10 * 60, Weather.Clear),
                ("yeongheung", "43b_yeongheung_fog", new Vector3(-22f, 1.1f, -44f), 0f, -10f, 10f, 12f, 6 * 60 + 20, Weather.Cloudy),
                ("taean", "44_taean_beach", new Vector3(-75f, 1.1f, -48f), 0f, -15f, 10f, 12f, 17 * 60 + 20, Weather.Clear),
                ("taean", "44c_taean_swimmers", new Vector3(-77f, 1.1f, -12f), 0f, 5f, 10f, 7f, 12 * 60, Weather.Clear),
                ("taean", "44b_taean_noon", new Vector3(-110f, 1.1f, -60f), 0f, 20f, 18f, 18f, 12 * 60, Weather.Clear),
                ("samcheok", "45_samcheok_chuam", new Vector3(110f, 1.1f, -44f), 60f, 60f, 8f, 20f, 9 * 60, Weather.Clear),
                ("samcheok", "45c_samcheok_ninja", new Vector3(16f, 1.1f, -47f), -60f, -70f, 10f, 6f, 10 * 60, Weather.Clear),
                ("samcheok", "45b_samcheok_night", new Vector3(72f, 1.1f, 20f), 0f, 0f, 8f, 10f, 21 * 60, Weather.Clear),
                ("deungdae", "46_deungdae_lighthouse", new Vector3(48f, 1.1f, -47f), 300f, 290f, 14f, 22f, 11 * 60, Weather.Clear),
                ("deungdae", "46b_deungdae_patrol", new Vector3(38f, 1.1f, -47.5f), 270f, 250f, 8f, 7f, 13 * 60, Weather.Clear),
                ("deungdae", "46c_deungdae_vampire", new Vector3(38f, 1.1f, -47.5f), 270f, 250f, 8f, 7f, 23 * 60, Weather.Clear),
                ("heonhwa", "47_heonhwa_road", new Vector3(-40f, 1.1f, -47f), 90f, 140f, 12f, 16f, 13 * 60, Weather.Clear),
                ("yokji", "48_yokji_farm", new Vector3(-126.5f, 1.4f, 0f), 0f, -20f, 22f, 20f, 15 * 60, Weather.Clear),
                ("yokji", "48b_yokji_bridge", new Vector3(140f, 1.1f, -16f), 40f, 20f, 16f, 18f, 16 * 60, Weather.Clear),
                ("jeju", "54_jeju_harbor", new Vector3(14f, 1.1f, -45f), 180f, 160f, 10f, 13f, 11 * 60, Weather.Clear),
                ("jeju", "54b_jeju_oedolgae", new Vector3(-60f, 1.1f, -44f), 330f, 320f, 12f, 22f, 16 * 60 + 30, Weather.Clear),
                ("cheongjin", "49_cheongjin_port", new Vector3(28f, 1.1f, -47f), 0f, 30f, 16f, 20f, 10 * 60, Weather.Clear),
                ("cheongjin", "49c_cheongjin_swat", new Vector3(18f, 1.1f, -47.5f), 270f, 250f, 8f, 8f, 11 * 60, Weather.Clear),
                ("cheongjin", "49d_cheongjin_night", new Vector3(18f, 1.1f, -47.5f), 90f, 110f, 8f, 8f, 23 * 60, Weather.Clear),
                ("cheongjin", "49b_cheongjin_racks", new Vector3(-60f, 1.1f, -68f), 0f, 200f, 12f, 14f, 14 * 60, Weather.Cloudy),
                ("geomun", "50_geomun_lighthouse", new Vector3(-90f, 1.1f, -45f), 300f, 300f, 12f, 24f, 17 * 60 + 30, Weather.Clear),
                // 해외 거점
                ("halong", "55_halong_bay", new Vector3(4f, 1.1f, -47f), 0f, 10f, 12f, 16f, 9 * 60, Weather.Clear),
                ("halong", "55c_halong_pirate", new Vector3(-3f, 1.1f, -47f), 200f, 200f, 8f, 6f, 11 * 60, Weather.Clear),
                ("halong", "55b_halong_karst", new Vector3(-70f, 1.1f, -46f), 0f, -20f, 8f, 14f, 16 * 60 + 30, Weather.Clear),
                ("halong", "55d_halong_karst_close", new Vector3(-100f, 1.1f, -44f), 0f, -10f, 2f, 10f, 11 * 60, Weather.Clear),
                ("santorini", "56_santorini_cliff", new Vector3(-6f, 1.1f, -42f), 180f, 165f, 16f, 34f, 11 * 60, Weather.Clear),
                ("santorini", "56c_santorini_village", new Vector3(14f, 1.1f, -48f), 270f, 250f, 8f, 7f, 10 * 60, Weather.Clear),
                ("santorini", "56b_santorini_caldera", new Vector3(39f, 1.1f, -28f), 0f, -10f, 6f, 13f, 17 * 60 + 30, Weather.Clear),
                ("nile", "57_nile_bank", new Vector3(6f, 1.1f, -47f), 0f, 10f, 10f, 14f, 10 * 60, Weather.Clear),
                ("nile", "57b_nile_sunset", new Vector3(-2f, 1.1f, -42f), 0f, -15f, 6f, 12f, 17 * 60 + 40, Weather.Clear),
                // 2026-10-19 새 해외 맵 (World/Maps)
                ("angkor", "58_angkor_moat", new Vector3(8f, 1.1f, -47f), 0f, 5f, 8f, 12f, 10 * 60, Weather.Clear),
                ("angkor", "58b_angkor_lotus", new Vector3(-30f, 1.1f, -43f), 20f, 30f, 12f, 8f, 17 * 60 + 30, Weather.Clear),
                ("vinpearl", "59_vinpearl_beach", new Vector3(-20f, 1.1f, -44f), 250f, 255f, 12f, 16f, 10 * 60, Weather.Clear),
                ("vinpearl", "59b_vinpearl_gazebo", new Vector3(32f, 1.4f, 4f), 0f, -30f, 10f, 12f, 16 * 60, Weather.Clear),
                ("donkhone", "60_donkhone_village", new Vector3(-10f, 1.1f, -47f), 270f, 290f, 10f, 14f, 9 * 60, Weather.Clear),
                ("donkhone", "60b_donkhone_rapids", new Vector3(110f, 1.1f, -44f), 20f, 15f, 14f, 14f, 15 * 60, Weather.Clear),
                ("phuket", "61_phuket_bay", new Vector3(-5f, 1.4f, 10f), 0f, 10f, 8f, 14f, 10 * 60, Weather.Clear),
                ("phuket", "61b_phuket_buddha", new Vector3(-5f, 1.4f, 14f), 221f, 221f, 4f, 10f, 16 * 60, Weather.Clear),
                ("bangkok", "62_bangkok_watarun", new Vector3(4f, 1.1f, -47f), 0f, 5f, 12f, 14f, 6 * 60 + 30, Weather.Clear),
                ("bangkok", "62b_bangkok_night", new Vector3(4f, 1.1f, -47f), 0f, 10f, 8f, 12f, 20 * 60, Weather.Clear),
                ("kohsamui", "63_kohsamui_buddha", new Vector3(60f, 1.1f, -46f), 40f, 45f, 8f, 14f, 9 * 60, Weather.Clear),
                ("kohsamui", "63b_kohsamui_pier", new Vector3(-1f, 1.4f, -12f), 0f, 20f, 10f, 12f, 16 * 60, Weather.Clear),
                ("langkawi", "64_langkawi_eagle", new Vector3(-30f, 1.1f, -40f), 230f, 230f, 14f, 18f, 10 * 60, Weather.Clear),
                ("langkawi", "64b_langkawi_skybridge", new Vector3(17f, 1.4f, -2f), 180f, 170f, -8f, 10f, 15 * 60, Weather.Clear),
                ("langkawi", "64c_langkawi_mangrove", new Vector3(-90f, 1.1f, -46f), 270f, 260f, 12f, 10f, 11 * 60, Weather.Clear),
                ("sentosa", "65_sentosa_merlion", new Vector3(-50f, 1.1f, -44f), 240f, 230f, 4f, 14f, 10 * 60, Weather.Clear),
                ("sentosa", "65b_sentosa_skyline", new Vector3(-10f, 1.1f, -46f), 10f, 10f, 2f, 10f, 18 * 60 + 30, Weather.Clear),
                ("telokbatek", "66_telokbatek_village", new Vector3(30f, 1.1f, -47f), 300f, 300f, 10f, 16f, 17 * 60 + 30, Weather.Clear),
                ("tioman", "67_tioman_resort", new Vector3(-6f, 1.4f, 0f), 180f, 180f, 6f, 14f, 10 * 60, Weather.Clear),
                ("tioman", "67b_tioman_beach", new Vector3(50f, 1.1f, -46f), 270f, 250f, 8f, 12f, 15 * 60, Weather.Clear),
                ("tenggol", "68_tenggol_deck", new Vector3(-2f, 1.4f, -20f), 0f, 0f, 14f, 16f, 10 * 60, Weather.Clear),
                ("crete", "69_crete_harbor", new Vector3(6f, 1.1f, -46f), 20f, 30f, 8f, 14f, 10 * 60, Weather.Clear),
                ("crete", "69b_crete_mountains", new Vector3(60f, 1.1f, 23f), 200f, 200f, 4f, 12f, 16 * 60, Weather.Clear),
                ("zakynthos", "70_zakynthos_navagio", new Vector3(-28f, 1.4f, -18f), 150f, 140f, 12f, 16f, 11 * 60, Weather.Clear),
                ("zakynthos", "70b_zakynthos_wreck", new Vector3(-28f, 1.4f, -24f), 110f, 105f, 8f, 12f, 14 * 60, Weather.Clear),
                ("duden", "71_duden_falls", new Vector3(-30f, 1.1f, -44f), 290f, 290f, 6f, 12f, 10 * 60, Weather.Clear),
                ("duden", "71b_duden_sunset", new Vector3(60f, 1.1f, -44f), 280f, 285f, 4f, 12f, 17 * 60 + 40, Weather.Clear),
            };
            string loaded = null;
            foreach (var s in shots)
            {
                if (_tourOnly != null && Array.IndexOf(_tourOnly, s.map) < 0) continue;
                if (loaded != s.map)
                {
                    Game.UI.EnterField(System.Linq.Enumerable.First(Fishing.Data.FishingAreas.All, a => a.Id == s.map), 1);
                    Game.UI.Help.Close();
                    loaded = s.map;
                    yield return new WaitForSeconds(1f);
                }
                Game.Clock.Set(5, s.minute);
                Game.Weather.Force(s.weather);
                var pos = s.pos;
                pos.y = Mathf.Max(pos.y, Fishing.World.FieldMap.Current is Fishing.World.CoastMapDef c ? c.HeightAt(pos.x, pos.z) + 0.1f : pos.y);
                Game.Player.Teleport(pos, s.yaw);
                Game.CameraRig.Yaw = s.camYaw;
                Game.CameraRig.Pitch = s.pitch;
                Game.CameraRig.Distance = s.dist;
                yield return new WaitForSeconds(2.5f);
                yield return Shot(s.shot);
            }
            if (_tourOnly != null) yield break;
            // 덕장 명태 가까이 (코를 엮어 장대에 건 명태 쌍)
            Game.UI.EnterField(Fishing.Data.FishingAreas.Korea.Find(a => a.Id == "cheongjin"), 1);
            Game.UI.Help.Close();
            yield return new WaitForSeconds(1f);
            Game.Clock.Set(5, 13 * 60);
            Game.Weather.Force(Weather.Clear);
            var pollock = GameObject.Find("PollockPair");
            if (pollock != null)
            {
                var pp = pollock.transform.position;
                // 장대 줄 사이(6m 간격) 가운데에 서서 앞줄을 본다
                var at = new Vector3(pp.x + 1.5f, 0f, pp.z + 2.8f);
                at.y = (Fishing.World.FieldMap.Current is Fishing.World.CoastMapDef cm ? cm.HeightAt(at.x, at.z) : 1f) + 0.1f;
                Game.Player.Teleport(at, 200f);
                Game.CameraRig.Yaw = 200f;
                Game.CameraRig.Pitch = 2f;
                Game.CameraRig.Distance = 2.4f;
                yield return new WaitForSeconds(2f);
                yield return Shot("49e_cheongjin_pollock_close");
            }
            // 새 어종 파이팅·결과 (등대섬 방파제에서 방어)
            Game.Clock.Set(5, 8 * 60);
            Game.Weather.Force(Weather.Clear);
            Game.UI.EnterField(Fishing.Data.FishingAreas.Korea.Find(a => a.Id == "deungdae"), 1);
            Game.UI.Help.Close();
            Game.Player.Teleport(new Vector3(45f, 1.1f, -3f), 0f);
            Game.CameraRig.Yaw = 0f;
            yield return new WaitForSeconds(1f);
            Game.Fishing.Autoplay = true;
            CheckClear("DebugStartFight");
            Game.Fishing.DebugStartFight("yellowtail", 86f, 18f);
            yield return new WaitForSeconds(2.5f);
            yield return Shot("51_coast_fight");
            var fight = Game.Fishing.Fight;
            float timeout = Time.time + 60f;
            while (Game.Fishing.State == FishingState.Fighting && Time.time < timeout) yield return null;
            if (fight != null) Debug.Log($"[Dev] 등대섬 파이팅 종료: {fight.Species.Name} {fight.Length:0.0}cm, 결과={fight.Outcome}");
            yield return WaitResult();
            yield return Shot("51b_coast_result");
            yield return CloseResult();
            Game.Fishing.Autoplay = false;
            // 도감 (새 낚시터 탭)
            foreach (var id in new[] { "atkamackerel", "redrockfish", "yellowtail", "squid", "flatfish" })
            {
                var sp = FishDatabase.Get(id);
                p.RecordDex(sp, sp.TrophyLen * 0.9f, sp.WeightGrams(sp.TrophyLen * 0.9f), 5);
            }
            Game.UI.OpenExclusive(Game.UI.Dex);
            Game.UI.Dex.ShowTab("deungdae");
            yield return new WaitForSeconds(0.8f);
            yield return Shot("52_dex_coast");
            Game.UI.Dex.Close();
            Game.UI.OpenExclusive(Game.UI.Areas);
            yield return new WaitForSeconds(0.5f);
            yield return Shot("53_area_list_all");
            // 해외 거점 지도 (동남아 · 지중해 · 아프리카)
            foreach (var hub in new[] { "sea", "med", "africa" })
            {
                Game.UI.Areas.SelectHub(hub);
                yield return new WaitForSeconds(0.4f);
                yield return Shot("53_area_list_" + hub);
            }
            Game.UI.Areas.SelectHub("korea");
            Game.UI.Areas.Close();

            // 나일강 나일농어 파이팅·결과
            Game.Clock.Set(6, 7 * 60);
            Game.UI.EnterField(System.Linq.Enumerable.First(Fishing.Data.FishingAreas.All, a => a.Id == "nile"), 1);
            Game.UI.Help.Close();
            Game.Player.Teleport(new Vector3(10f, 1.1f, -42f), 0f);
            Game.CameraRig.Yaw = 0f;
            yield return new WaitForSeconds(1f);
            Game.Fishing.Autoplay = true;
            // 앞 장면들의 파이팅으로 체력·내구도가 바닥나 있으면 페널티 때문에 오래 걸린다
            Game.Profile.ChangeStamina(Game.Profile.StaminaCap);
            Game.Profile.Data.rodDurability = 100f;
            CheckClear("DebugStartFight");
            Game.Fishing.DebugStartFight("nileperch", 158f, 20f);
            yield return new WaitForSeconds(2.5f);
            yield return Shot("58_nile_fight");
            fight = Game.Fishing.Fight;
            timeout = Time.time + 90f;
            while (Game.Fishing.State == FishingState.Fighting && Time.time < timeout) yield return null;
            if (fight != null) Debug.Log($"[Dev] 나일강 파이팅 종료: {fight.Species.Name} {fight.Length:0.0}cm, 결과={fight.Outcome}");
            yield return WaitResult();
            yield return Shot("58b_nile_result");
            yield return CloseResult();
            Game.Fishing.Autoplay = false;
        }

        /// <summary>바라보는 쪽 16m로 던질 때 궤적에 막는 것이 있으면 로그로 알린다 (연출 장면이 어색하지 않은지 검사).</summary>
        static void CheckClear(string what)
        {
            var from = Game.Player.RodTip.position;
            var to = Game.Player.transform.position + Game.Player.Model.forward * 16f;
            if (FishingController.CastBlocked(from, to, out string blocker))
                Debug.LogWarning($"[Dev] 캐스팅 막힘 ({what}, {Fishing.World.FieldMap.Id} {Game.Player.transform.position}): {blocker}");
        }

        /// <summary>그날 from~to(분) 사이에서 물이 가장 높거나(high) 낮은 시각.</summary>
        static float MinuteAtTide(int day, float from, float to, bool high)
        {
            float best = from, bestLevel = high ? -2f : 2f;
            for (float m = from; m <= to; m += 5f)
            {
                float l = Tide.LevelAt(day * GameClock.MinutesPerDay + m);
                if (high ? l > bestLevel : l < bestLevel) { bestLevel = l; best = m; }
            }
            return best;
        }

        /// <summary>환호 컷이 끝나고 결과 창이 뜬 뒤 물고기가 튀어 오를 때까지 기다린다.</summary>
        IEnumerator WaitResult()
        {
            float until = Time.realtimeSinceStartup + 4f;
            while (!Game.UI.Result.IsOpen && Time.realtimeSinceStartup < until) yield return null;
            yield return new WaitForSecondsRealtime(1.0f);
        }

        /// <summary>결과 창을 닫는다 (살림망에 넣기). 닫히지 않으면 로그로 알린다.</summary>
        IEnumerator CloseResult()
        {
            float until = Time.realtimeSinceStartup + 3f;
            while (Game.UI.Result.IsOpen && Time.realtimeSinceStartup < until)
            {
                Game.UI.Result.ChooseByKey(true);
                yield return null;
            }
            if (Game.UI.Result.IsOpen) Debug.LogError("[Dev] 결과 창이 닫히지 않았습니다.");
        }

        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            var path = Path.Combine(_args.ShotDir, name + ".png");
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"[Dev] 스크린샷: {path} ({_fps:0} FPS, {Fishing.World.FieldMap.Id}, 정적 배칭 {Fishing.World.WorldBatcher.LastBatched}개)");
            yield return null;
            yield return null;
            if (name == _args.StopAfter)
            {
                yield return new WaitForSecondsRealtime(0.5f);
                Debug.Log("[Dev] 자동 스크린샷 완료 (-autoshotStop " + name + ")");
                Application.Quit();
            }
        }
    }
}
