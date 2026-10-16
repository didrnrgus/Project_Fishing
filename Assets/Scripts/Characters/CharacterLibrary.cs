using UnityEngine;

namespace Fishing.Characters
{
    /// <summary>
    /// 받은 휴머노이드 캐릭터 참조 묶음 (Assets/Resources/CharacterLibrary.asset). 빌드할 때 Editor/CharacterLibraryBake가 채운다.
    /// 프리팹이 Resources 밖(unity-chan!, Zombie, MixamoCharacters)에 있어도 여기서 참조하면 빌드에 들어간다.
    /// - unityChan: Unity-Chan! Model (unity-chan!, 유니티짱 라이선스 — 크레딧에 로고·표기)
    /// - zombie: Modern Zombie Free (Artalasky)
    /// - xBot: Mixamo X Bot 마네킹 (귀신 NPC 몸)
    /// </summary>
    public class CharacterLibrary : ScriptableObject
    {
        public GameObject unityChan;
        public GameObject zombie;
        public GameObject xBot;

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
            if (l == null) return null;
            return id switch
            {
                "unitychan" => l.unityChan,
                "zombie" => l.zombie,
                "ghost" => l.xBot,
                _ => null
            };
        }
    }
}
