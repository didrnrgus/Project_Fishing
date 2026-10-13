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

            yield return new WaitForSeconds(3f);
            yield return Shot("01_field_morning");

            // 캐릭터 정면 클로즈업 (한낮)
            Game.Weather.Force(Weather.Clear);
            Game.Clock.Set(1, 12 * 60);
            Game.CameraRig.Yaw = Game.Player.Yaw + 180f;
            Game.CameraRig.Pitch = 8f;
            Game.CameraRig.Distance = 3.2f;
            yield return new WaitForSeconds(2f);
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
            Game.UI.OpenExclusive(Game.UI.Shop);
            yield return Shot("14_shop");
            Game.UI.Shop.Close();
            Game.UI.OpenExclusive(Game.UI.Help);
            yield return Shot("16_help");
            Game.UI.Help.Close();

            // 비 오는 밤
            Game.Player.Teleport(new Vector3(-70f, 1.1f, -64f), 20f);
            Game.CameraRig.Yaw = 20f;
            Game.Clock.Set(1, 21 * 60 + 30);
            Game.Weather.Force(Weather.Rain);
            yield return new WaitForSeconds(5f);
            yield return Shot("15_night_rain");

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
