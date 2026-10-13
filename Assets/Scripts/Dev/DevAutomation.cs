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
    }

    /// <summary>
    /// 빌드한 플레이어를 `-autoshot &lt;폴더&gt;` 인자로 실행하면 주요 장면을 자동으로 연출하고 스크린샷을 찍은 뒤 종료한다.
    /// 세이브 파일은 건드리지 않는다. (Game.SaveDisabled)
    /// </summary>
    public class DevAutomation : MonoBehaviour
    {
        DevArgs _args;

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
            Game.CameraRig.Yaw = Game.Player.Yaw;
            Game.CameraRig.Distance = 7f;

            // 원작 영상처럼 밤의 낚시 데크에서
            Game.Weather.Force(Weather.Clear);
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
            Game.Fishing.DebugCast(15.5f);
            yield return new WaitForSeconds(2.5f);
            yield return Shot("03_waiting");

            // 본신: 찌가 잠기는 챔질 게이지
            Game.Fishing.DebugForceBite("carp", 82f);
            yield return new WaitForSeconds(0.85f);
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
            yield return new WaitForSeconds(1.3f);
            yield return Shot("10_result");
            if (Game.UI.Result.IsOpen) Game.UI.Result.ChooseByKey(true);
            Game.Fishing.Autoplay = false;
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
            p.Data.quest.index = 2;
            p.NotifyChanged();

            yield return new WaitForSeconds(0.5f);
            Game.UI.OpenExclusive(Game.UI.Dex);
            yield return Shot("11_dex");
            Game.UI.OpenExclusive(Game.UI.KeepNet);
            yield return Shot("12_keepnet");
            Game.UI.OpenExclusive(Game.UI.Quest);
            yield return Shot("13_quest");
            Game.UI.Quest.ShowTab(1);
            yield return Shot("13b_daily");
            Game.UI.Quest.ShowTab(3);
            yield return Shot("13c_achievements");
            Game.UI.Quest.ShowTab(0);
            Game.UI.OpenExclusive(Game.UI.Exchange);
            yield return Shot("13d_exchange");
            Game.UI.OpenExclusive(Game.UI.Shop);
            yield return Shot("14_shop");
            Game.UI.Shop.Close();
            Game.UI.OpenExclusive(Game.UI.Help);
            yield return Shot("16_help");
            Game.UI.Help.Close();

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
            yield return new WaitForSeconds(0.3f);
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
            Game.UI.OpenExclusive(Game.UI.Costume);
            Fishing.UI.PortraitCamera.FullBody.Spin = 25f;
            yield return new WaitForSeconds(0.6f);
            yield return Shot("21c_costume");
            Game.UI.Costume.Close();

            // 지역 지도(M): 어군 하나는 3마리를 낚아 작은 어군으로 만든다
            var shrink = Game.Schools.Schools[1];
            for (int i = 0; i < Fishing.World.FishSchoolManager.CatchesToShrink; i++) Game.Schools.OnCatch(shrink, shrink.SpeciesIds[0]);
            Game.UI.OpenExclusive(Game.UI.AreaMap);
            yield return new WaitForSeconds(0.3f);
            yield return Shot("17_area_map");
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
            Game.Boat.Toggle();
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

            // ================= 홍현 앞바다 =================
            p.Data.level = Mathf.Max(p.Data.level, 10);
            Game.Weather.Force(Weather.Clear);
            Game.Clock.Set(2, 15 * 60);
            Game.UI.EnterField(Fishing.Data.FishingAreas.Korea.Find(a => a.Id == "honghyeon"), 2);
            Game.UI.Help.Close();
            var bw = Fishing.World.HonghyeonDef.Breakwater;
            Game.Player.Teleport(new Vector3(bw.center.x, 1.1f, bw.yMin + 6f), 0f);
            Game.CameraRig.Yaw = -15f;
            Game.CameraRig.Pitch = 12f;
            Game.CameraRig.Distance = 9f;
            yield return new WaitForSeconds(3f);
            yield return Shot("22_honghyeon_day");

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

            // 서쪽 갯바위 섬 근처를 배로
            Game.Clock.Set(2, 10 * 60);
            Game.Player.Teleport(new Vector3(bw.center.x - 6f, 1.1f, -42f), 0f);
            yield return new WaitForSeconds(0.5f);
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
            Game.Player.Teleport(new Vector3(bw.center.x, 1.1f, bw.yMax - 3f), 0f);
            yield return new WaitForSeconds(1f);
            Game.Fishing.Autoplay = true;
            Game.Fishing.DebugStartFight("blackseabream", 55f, 16f);
            yield return new WaitForSeconds(2.5f);
            yield return Shot("26_sea_fight");
            var seaFight = Game.Fishing.Fight;
            float seaTimeout = Time.time + 60f;
            while (Game.Fishing.State == FishingState.Fighting && Time.time < seaTimeout) yield return null;
            if (seaFight != null) Debug.Log($"[Dev] 바다 파이팅 종료: {seaFight.Species.Name} {seaFight.Length:0.0}cm, 결과={seaFight.Outcome}");
            yield return new WaitForSeconds(1.3f);
            yield return Shot("27_sea_result");
            if (Game.UI.Result.IsOpen) Game.UI.Result.ChooseByKey(true);
            Game.Fishing.Autoplay = false;

            Debug.Log("[Dev] 자동 스크린샷 완료");
            Application.Quit();
        }

        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            var path = Path.Combine(_args.ShotDir, name + ".png");
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"[Dev] 스크린샷: {path}");
            yield return null;
            yield return null;
        }
    }
}
