using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Fishing.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace Fishing.Characters
{
    /// <summary>
    /// 대기실 [옷 바꾸기]: 내 캐릭터에게 다른 VRM의 옷을 무작위로 입힌다 (VrmOutfit). 고른 옷은 세이브(SaveData.outfit)에 남아 다음에 켜도 그대로.
    /// - 옷 후보 = 쓸 수 있는 VRM 전부(게임 폴더 + 문서 폴더 + 받은 옷) 중 지금 모델·지금 옷이 아닌 것.
    /// - characters.json의 outfitUrls에 아직 받지 않은 주소가 있으면 그중 하나를 웹에서 받아(.vrm 또는 .vrm이 든 .zip) 문서/짭풍명월/Characters/outfits에 두고 입힌다.
    ///   받기에 실패하면 가진 것 중에서 고른다. 한 번 받은 옷은 다시 받지 않는다(후보에 들어간다).
    /// 옷 조각(…_CLOTH 재질)이 없는 VRM(VRoid가 아닌 모델)은 입어도 그대로라 고른 뒤 다시 고른다.
    /// </summary>
    public static class OutfitChanger
    {
        public static bool Busy { get; private set; }

        public static void Randomize()
        {
            if (Busy || Game.UI == null || Game.Profile == null) return;
            Game.UI.StartCoroutine(Run());
        }

        static IEnumerator Run()
        {
            Busy = true;
            var p = Game.Profile;
            var look = p.BuildLook();
            string id = null;

            var pending = new List<string>();
            foreach (var url in CharacterRoster.OutfitUrls)
                if (!string.IsNullOrWhiteSpace(url) && !File.Exists(LocalPath(url))) pending.Add(url);
            if (pending.Count > 0)
            {
                var url = pending[Random.Range(0, pending.Count)];
                Game.Toast("새 옷을 받아 오는 중…");
                string got = null;
                yield return Download(url, path => got = path);
                if (got != null) id = "user/outfits/" + Path.GetFileName(got);
                else Game.Toast("옷을 받지 못해 가진 옷 중에서 고릅니다.", ToastKind.Warn);
            }

            if (id == null)
            {
                var pool = new List<string>();
                foreach (var v in VrmCharacters.List())
                    if (v != look.Vrm && v != look.Outfit) pool.Add(v);
                // 옷 조각이 없는 모델은 빼고 고른다 (읽어 봐야 안다 → 하나씩 읽어 본다)
                while (pool.Count > 0)
                {
                    var pick = pool[Random.Range(0, pool.Count)];
                    pool.Remove(pick);
                    if (VrmCharacters.LoadNow(pick) && VrmOutfit.HasCloth(VrmCharacters.Template(pick))) { id = pick; break; }
                }
            }

            if (id != null)
            {
                p.SetOutfit(id);
                Game.Toast($"옷을 갈아입었습니다: {System.IO.Path.GetFileNameWithoutExtension(id).Replace('_', ' ')}", ToastKind.Good);
                Game.Sound?.Coin();
            }
            else Game.Toast("입을 수 있는 다른 옷이 없습니다.", ToastKind.Warn);
            Busy = false;
        }

        /// <summary>주소 → 받아 둘 파일 (outfits/주소 끝 이름.vrm).</summary>
        static string LocalPath(string url)
        {
            string name = Path.GetFileNameWithoutExtension(new System.Uri(url).AbsolutePath);
            foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            if (string.IsNullOrEmpty(name)) name = "outfit_" + CharacterRoster.StableHash(url);
            return Path.Combine(VrmCharacters.OutfitFolder, name + ".vrm");
        }

        static IEnumerator Download(string url, System.Action<string> done)
        {
            string dest = LocalPath(url);
            using (var req = UnityWebRequest.Get(url))
            {
                req.timeout = 60;
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[Dev] 옷 받기 실패: {url} ({req.error})");
                    done(null);
                    yield break;
                }
                try
                {
                    Directory.CreateDirectory(VrmCharacters.OutfitFolder);
                    var data = req.downloadHandler.data;
                    if (data.Length > 4 && data[0] == 'P' && data[1] == 'K')
                    {
                        // .zip: 안의 첫 .vrm만 꺼낸다
                        using (var zip = new ZipArchive(new MemoryStream(data), ZipArchiveMode.Read))
                        {
                            ZipArchiveEntry entry = null;
                            foreach (var e in zip.Entries)
                                if (e.FullName.EndsWith(".vrm", System.StringComparison.OrdinalIgnoreCase)) { entry = e; break; }
                            if (entry == null) throw new IOException("zip 안에 .vrm이 없습니다");
                            using (var s = entry.Open())
                            using (var f = File.Create(dest)) s.CopyTo(f);
                        }
                    }
                    else if (data.Length > 4 && data[0] == 'g' && data[1] == 'l' && data[2] == 'T' && data[3] == 'F') File.WriteAllBytes(dest, data);
                    else throw new IOException("VRM 파일이 아닙니다");
                    Debug.Log($"[Dev] 옷 받음: {url} → {dest} ({data.Length / 1024}KB)");
                    done(dest);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[Dev] 옷 받기 실패: {url} ({e.Message})");
                    if (File.Exists(dest)) File.Delete(dest);
                    done(null);
                }
            }
        }
    }
}
