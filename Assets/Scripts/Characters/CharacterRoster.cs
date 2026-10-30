using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Fishing.Characters
{
    /// <summary>
    /// 누구에게 어떤 VRM을 입히는지 (characters.json). 빌드한 뒤에도 파일만 바꿔 갈아입힐 수 있다 — 안내는 docs/guide/09-characters.md.
    /// - 게임 폴더 StreamingAssets/Characters/characters.json: 게임과 함께 나가는 기본 설정 (기본 VRM들과 같은 폴더)
    /// - 문서/짭풍명월/Characters/characters.json: 있으면 적은 항목만 위를 덮는다 (게임을 다시 깔아도 남는다)
    /// 파일 이름은 문서 폴더 → 추가 캐릭터 폴더(<see cref="CharacterPack"/>) → 게임 폴더 순으로 찾는다 → 같은 이름으로 문서 폴더에 넣으면 기본 캐릭터가 바뀐다.
    /// NPC: 이름으로 정한 것(npc)이 있으면 그것, 없으면 성별 목록(npcMale/npcFemale + 추가 캐릭터 중 그 성별)에서 이름으로 하나를 고른다(늘 같은 사람 = 같은 모델).
    /// </summary>
    public static class CharacterRoster
    {
        public const string FileName = "characters.json";

        [Serializable]
        public class Assign
        {
            public string name;
            public string file;
        }

        [Serializable]
        public class Brightness
        {
            public string file;
            public float value = 1f;
        }

        [Serializable]
        public class Config
        {
            public string playerMale;
            public string playerFemale;
            /// <summary>기본 캐릭터에 다른 VRM의 옷을 입힌다 (비우면 제 옷).</summary>
            public string playerMaleOutfit;
            public string playerFemaleOutfit;
            public string[] npcMale;
            public string[] npcFemale;
            public Assign[] npc;
            /// <summary>[옷 바꾸기]가 받아 올 옷 VRM 주소 (.vrm 또는 .vrm이 든 .zip). 받은 파일은 문서 폴더 Characters/outfits에 남는다.</summary>
            public string[] outfitUrls;
            /// <summary>모델 밝기 배수 (1 = 그대로, 0.8 = 20% 어둡게). 흰 머리·흰 옷이라 밤에도 떠 보이는 모델을 낮춘다.</summary>
            public Brightness[] brightness;
        }

        static Config _cfg;
        static readonly List<string> _warnings = new List<string>();

        /// <summary>설정을 읽다가 생긴 문제 (캐릭터 창에 보여 준다).</summary>
        public static IReadOnlyList<string> Warnings { get { Ensure(); return _warnings; } }

        static void Ensure()
        {
            if (_cfg == null) Reload();
        }

        /// <summary>characters.json을 다시 읽는다 (캐릭터 창 "새로고침"). 이미 서 있는 NPC는 맵을 다시 지을 때 바뀐다.</summary>
        public static void Reload()
        {
            _warnings.Clear();
            CharacterPack.Reload();
            _warnings.AddRange(CharacterPack.Warnings);
            _cfg = Read(Path.Combine(VrmCharacters.BuiltInFolder, FileName)) ?? new Config();
            var user = Read(Path.Combine(VrmCharacters.UserFolder, FileName));
            if (user != null)
            {
                if (!string.IsNullOrWhiteSpace(user.playerMale)) _cfg.playerMale = user.playerMale;
                if (!string.IsNullOrWhiteSpace(user.playerFemale)) _cfg.playerFemale = user.playerFemale;
                if (user.playerMaleOutfit != null) _cfg.playerMaleOutfit = user.playerMaleOutfit;
                if (user.playerFemaleOutfit != null) _cfg.playerFemaleOutfit = user.playerFemaleOutfit;
                if (user.npcMale != null && user.npcMale.Length > 0) _cfg.npcMale = user.npcMale;
                if (user.npcFemale != null && user.npcFemale.Length > 0) _cfg.npcFemale = user.npcFemale;
                if (user.outfitUrls != null && user.outfitUrls.Length > 0) _cfg.outfitUrls = user.outfitUrls;
                if (user.npc != null && user.npc.Length > 0)
                {
                    // 이름별 지정은 합친다 (문서 폴더 것이 이긴다)
                    var merged = new List<Assign>(user.npc);
                    if (_cfg.npc != null)
                        foreach (var a in _cfg.npc)
                            if (merged.FindIndex(m => m.name == a.name) < 0) merged.Add(a);
                    _cfg.npc = merged.ToArray();
                }
                if (user.brightness != null && user.brightness.Length > 0)
                {
                    var merged = new List<Brightness>(user.brightness);
                    if (_cfg.brightness != null)
                        foreach (var a in _cfg.brightness)
                            if (merged.FindIndex(m => m.file == a.file) < 0) merged.Add(a);
                    _cfg.brightness = merged.ToArray();
                }
            }
            int packWarnings = _warnings.Count;
            foreach (var f in AllFiles())
                if (IdOf(f) == null) _warnings.Add($"{FileName}에 적힌 \"{f}\" 파일이 없습니다.");
            for (int i = packWarnings; i < _warnings.Count; i++) Debug.LogWarning("[Dev] " + _warnings[i]); // 추가 캐릭터 경고는 CharacterPack이 이미 남겼다
        }

        static Config Read(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                return JsonUtility.FromJson<Config>(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                _warnings.Add($"{path}를 읽지 못했습니다 ({e.Message}). 쉼표·따옴표를 확인하세요.");
                return null;
            }
        }

        static IEnumerable<string> AllFiles()
        {
            if (!string.IsNullOrWhiteSpace(_cfg.playerMale)) yield return _cfg.playerMale;
            if (!string.IsNullOrWhiteSpace(_cfg.playerFemale)) yield return _cfg.playerFemale;
            if (_cfg.npcMale != null) foreach (var f in _cfg.npcMale) yield return f;
            if (_cfg.npcFemale != null) foreach (var f in _cfg.npcFemale) yield return f;
            if (_cfg.npc != null) foreach (var a in _cfg.npc) yield return a.file;
        }

        /// <summary>파일 이름 → VRM id. 문서 폴더에 있으면 user/, 추가 캐릭터(정보 있는 것)면 pack/, 아니면 게임 폴더(builtin/). 다 없으면 null.</summary>
        public static string IdOf(string file)
        {
            if (string.IsNullOrWhiteSpace(file)) return null;
            file = file.Trim();
            if (!file.EndsWith(".vrm", StringComparison.OrdinalIgnoreCase)) file += ".vrm";
            if (File.Exists(Path.Combine(VrmCharacters.UserFolder, file))) return "user/" + file;
            if (CharacterPack.Find(file) is CharacterPack.Entry pack) return "pack/" + pack.File;
            if (File.Exists(Path.Combine(VrmCharacters.BuiltInFolder, file))) return "builtin/" + file;
            return null;
        }

        /// <summary>내 캐릭터 기본 모델 (캐릭터 창에서 아무것도 안 골랐을 때).</summary>
        public static string PlayerDefault(bool female)
        {
            Ensure();
            return IdOf(female ? _cfg.playerFemale : _cfg.playerMale) ?? IdOf(female ? _cfg.playerMale : _cfg.playerFemale) ?? FirstAvailable();
        }

        /// <summary>[옷 바꾸기]가 받아 올 옷 주소들.</summary>
        /// <summary>그 파일(이름만)의 밝기 배수, 적혀 있지 않으면 1. 이미 읽은 모델은 다시 읽어야 바뀐다.</summary>
        public static float BrightnessOf(string file)
        {
            Ensure();
            if (CharacterPack.Find(file) is CharacterPack.Entry pack && pack.brightness > 0f) return Mathf.Clamp(pack.brightness, 0.2f, 1.5f);
            if (_cfg.brightness != null)
                foreach (var b in _cfg.brightness)
                    if (string.Equals(b.file, file, StringComparison.OrdinalIgnoreCase)) return Mathf.Clamp(b.value, 0.2f, 1.5f);
            return 1f;
        }

        public static string[] OutfitUrls { get { Ensure(); return _cfg.outfitUrls ?? System.Array.Empty<string>(); } }

        /// <summary>기본 캐릭터가 입을 다른 VRM의 옷 (없으면 null = 제 옷).</summary>
        public static string PlayerOutfit(bool female)
        {
            Ensure();
            return IdOf(female ? _cfg.playerFemaleOutfit : _cfg.playerMaleOutfit);
        }

        /// <summary>NPC 모델: 이름 지정 → 성별 목록에서 이름으로 고르기 → 다른 성별 목록 → 아무거나.</summary>
        public static string ForNpc(string name, bool female)
        {
            Ensure();
            if (_cfg.npc != null && name != null)
                foreach (var a in _cfg.npc)
                    if (a.name == name)
                    {
                        var id = IdOf(a.file);
                        if (id != null) return id;
                    }
            return Pick(NpcFiles(female), name) ?? Pick(NpcFiles(!female), name) ?? PlayerDefault(female);
        }

        /// <summary>그 성별 NPC가 고를 파일: characters.json 목록 + 추가 캐릭터 중 그 성별.</summary>
        static IEnumerable<string> NpcFiles(bool female)
        {
            var list = female ? _cfg.npcFemale : _cfg.npcMale;
            if (list != null) foreach (var f in list) yield return f;
            foreach (var f in CharacterPack.FilesFor(female)) yield return f;
        }

        /// <summary>characters.json의 npc에 이름으로 정해 둔 사람 (색을 바꾸지 않는다).</summary>
        public static bool IsAssigned(string name)
        {
            Ensure();
            if (_cfg.npc == null || name == null) return false;
            foreach (var a in _cfg.npc)
                if (a.name == name && IdOf(a.file) != null) return true;
            return false;
        }

        static string Pick(IEnumerable<string> files, string name)
        {
            var ids = new List<string>();
            foreach (var f in files)
            {
                var id = IdOf(f);
                if (id != null && !ids.Contains(id)) ids.Add(id);
            }
            if (ids.Count == 0) return null;
            return ids[(int)(StableHash(name ?? "") % (uint)ids.Count)];
        }

        static string FirstAvailable()
        {
            var all = VrmCharacters.List();
            return all.Count > 0 ? all[0] : null;
        }

        /// <summary>실행할 때마다 같은 값 (string.GetHashCode는 실행마다 달라질 수 있다). FNV-1a.</summary>
        public static uint StableHash(string s)
        {
            uint h = 2166136261;
            foreach (char c in s) { h ^= c; h *= 16777619; }
            return h;
        }
    }
}
