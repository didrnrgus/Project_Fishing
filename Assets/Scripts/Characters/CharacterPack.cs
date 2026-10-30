using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Fishing.Characters
{
    /// <summary>
    /// 추가 캐릭터 폴더: 게임 폴더(ProjectFishing.exe 옆)의 Characters. 빌드에 넣지 않고 폴더에 두기만 하는 캐릭터 (VRoid Hub 등에서 받은 남의 모델).
    /// - 파일.vrm 옆에 같은 이름의 파일.json(정보: 이름·성별·만든 사람·출처·라이선스)이 있는 것만 쓴다. 정보 없는 .vrm은 목록에 안 나오고 캐릭터 창에 노란 글씨로 알린다.
    /// - 성별이 적힌 캐릭터는 NPC 목록(characters.json npcMale/npcFemale)에 저절로 더해진다.
    /// - 에디터에서는 Builds/Windows/Characters (빌드한 게임과 같은 폴더). 저장소에는 올라가지 않는다(.gitignore Builds/).
    /// </summary>
    public static class CharacterPack
    {
        [Serializable]
        public class Entry
        {
            /// <summary>게임에 보일 이름 (비우면 VRM 안의 이름).</summary>
            public string name;
            /// <summary>"female" / "male" (비우면 NPC 목록에 안 넣는다).</summary>
            public string gender;
            public string author;
            /// <summary>받은 곳 주소 (VRoid Hub 모델 페이지 등).</summary>
            public string url;
            /// <summary>라이선스 요약 (예: "CC0", "재배포·개조 허락").</summary>
            public string license;
            /// <summary>크레딧 표기가 필요한지.</summary>
            public bool credit;
            /// <summary>밝기 배수 (0이면 적지 않은 것 = characters.json의 brightness 또는 1).</summary>
            public float brightness;

            [NonSerialized] public string File;
            public bool Female => string.Equals(gender, "female", StringComparison.OrdinalIgnoreCase);
            public bool Male => string.Equals(gender, "male", StringComparison.OrdinalIgnoreCase);
        }

        public static string Folder
        {
            get
            {
                // 빌드: <게임 폴더>/ProjectFishing_Data → <게임 폴더>/Characters, 에디터: <프로젝트>/Assets → <프로젝트>/Builds/Windows/Characters
                var root = Path.GetDirectoryName(Application.dataPath);
#if UNITY_EDITOR
                root = Path.Combine(root, "Builds", "Windows");
#endif
                return Path.Combine(root, "Characters");
            }
        }

        static readonly List<Entry> _entries = new List<Entry>();
        static readonly List<string> _warnings = new List<string>();
        static bool _read;

        /// <summary>정보가 있어 쓸 수 있는 캐릭터 (파일 이름 순).</summary>
        public static IReadOnlyList<Entry> Entries { get { Ensure(); return _entries; } }

        /// <summary>정보 파일이 없거나 못 읽은 .vrm (캐릭터 창에 보여 준다).</summary>
        public static IReadOnlyList<string> Warnings { get { Ensure(); return _warnings; } }

        static void Ensure()
        {
            if (!_read) Reload();
        }

        /// <summary>폴더를 다시 훑는다 (캐릭터 창 "새로고침", CharacterRoster.Reload).</summary>
        public static void Reload()
        {
            _read = true;
            _entries.Clear();
            _warnings.Clear();
            try
            {
                if (!Directory.Exists(Folder)) return;
                var files = Directory.GetFiles(Folder, "*.vrm");
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);
                foreach (var vrm in files)
                {
                    var file = Path.GetFileName(vrm);
                    var json = Path.ChangeExtension(vrm, ".json");
                    if (!System.IO.File.Exists(json))
                    {
                        _warnings.Add($"추가 캐릭터 {file}: 정보 파일 {Path.GetFileName(json)}이 없어 쓰지 않아요.");
                        continue;
                    }
                    try
                    {
                        var e = JsonUtility.FromJson<Entry>(System.IO.File.ReadAllText(json)) ?? new Entry();
                        e.File = file;
                        _entries.Add(e);
                    }
                    catch (Exception ex) { _warnings.Add($"추가 캐릭터 {Path.GetFileName(json)}를 읽지 못했어요 ({ex.Message})."); }
                }
            }
            catch (Exception e) { Debug.LogWarning($"[Dev] 추가 캐릭터 폴더를 읽지 못함: {Folder} ({e.Message})"); }
            foreach (var w in _warnings) Debug.LogWarning("[Dev] " + w);
        }

        /// <summary>파일 이름(.vrm 포함)의 정보, 없으면 null.</summary>
        public static Entry Find(string file)
        {
            if (string.IsNullOrEmpty(file)) return null;
            foreach (var e in Entries)
                if (string.Equals(e.File, file, StringComparison.OrdinalIgnoreCase)) return e;
            return null;
        }

        /// <summary>NPC 목록에 더할 파일들 (그 성별로 적힌 것).</summary>
        public static IEnumerable<string> FilesFor(bool female)
        {
            foreach (var e in Entries)
                if (female ? e.Female : e.Male) yield return e.File;
        }
    }
}
