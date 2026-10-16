using System.Collections.Generic;
using UnityEngine;

namespace Fishing.Characters
{
    /// <summary>
    /// 받은 휴머노이드 캐릭터 참조 묶음 (Assets/Resources/CharacterLibrary.asset). 빌드할 때 Editor/CharacterLibraryBake가 채운다.
    /// 프리팹이 Resources 밖(unity-chan!, Zombie, MixamoCharacters)에 있어도 여기서 참조하면 빌드에 들어간다.
    /// 어떤 id가 어떤 파일·키·성별인지는 <see cref="ExternalCharacters.Registry"/>.
    /// </summary>
    public class CharacterLibrary : ScriptableObject
    {
        [System.Serializable]
        public class Entry
        {
            public string id;
            public GameObject prefab;
        }

        public List<Entry> entries = new List<Entry>();

        static CharacterLibrary _lib;
        static bool _loaded;

        public static CharacterLibrary Lib
        {
            get
            {
                if (!_loaded)
                {
                    _lib = Resources.Load<CharacterLibrary>("CharacterLibrary");
                    _loaded = true;
                }
                return _lib;
            }
        }

        public static GameObject Get(string id)
        {
            var l = Lib;
            if (l == null || l.entries == null) return null;
            foreach (var e in l.entries)
                if (e.id == id) return e.prefab;
            return null;
        }
    }
}
