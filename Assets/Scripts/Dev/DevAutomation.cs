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

            // 낚시 데크 끝에서 해질녘
            Game.Clock.Set(1, 18 * 60 + 20);
            Game.Player.Teleport(new Vector3(25f, 1.1f, -29f), 0f);
            Game.CameraRig.Yaw = -15f;
            Game.CameraRig.Pitch = 16f;
            Game.CameraRig.Distance = 6f;
            yield return new WaitForSeconds(1.5f);
            Game.Fishing.DebugCast(16f);
            yield return new WaitForSeconds(2.5f);
            yield return Shot("02_waiting_sunset");

            // 파이팅
            Game.Fishing.Autoplay = true;
            Game.Fishing.DebugStartFight("carp", 82f, 20f);
            yield return new WaitForSeconds(2.5f);
            yield return Shot("03_fight");

            bool countered = false;
            var fight = Game.Fishing.Fight;
            fight.Countered += () => countered = true;
            float timeout = Time.time + 25f;
            while (!countered && Time.time < timeout && Game.Fishing.State == FishingState.Fighting) yield return null;
            if (countered) yield return Shot("04_counter");

            timeout = Time.time + 120f;
            while (Game.Fishing.State == FishingState.Fighting && Time.time < timeout) yield return null;
            Debug.Log($"[Dev] 파이팅 종료: 상태={Game.Fishing.State}, 경과={fight.Elapsed:0.0}s, HP={fight.HP:0}/{fight.MaxHP:0}, 결과={fight.Outcome}");
            yield return new WaitForSeconds(0.6f);
            yield return Shot("05_result");
            if (Game.UI.Result.IsOpen) Game.UI.Result.ChooseByKey(true);
            Game.Fishing.Autoplay = false;

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
            yield return Shot("06_dex");
            Game.UI.OpenExclusive(Game.UI.KeepNet);
            yield return Shot("07_keepnet");
            Game.UI.OpenExclusive(Game.UI.Quest);
            yield return Shot("08_quest");
            Game.UI.OpenExclusive(Game.UI.Shop);
            yield return Shot("09_shop");
            Game.UI.Shop.Close();

            // 비 오는 밤
            Game.Player.Teleport(new Vector3(-70f, 1.1f, -64f), 20f);
            Game.CameraRig.Yaw = 20f;
            Game.Clock.Set(1, 21 * 60 + 30);
            Game.Weather.Force(Weather.Rain);
            yield return new WaitForSeconds(5f);
            yield return Shot("10_night_rain");

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
