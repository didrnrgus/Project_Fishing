using Fishing.Characters;
using UnityEditor;
using UnityEngine;

namespace Fishing.EditorTools
{
    /// <summary>
    /// 받은 캐릭터(유니티짱·좀비·X Bot)를 Resources/CharacterLibrary.asset에 묶는다. BuildWindows가 부른다. 메뉴: Fishing > Build Character Library
    /// </summary>
    public static class CharacterLibraryBake
    {
        const string LibPath = "Assets/Resources/CharacterLibrary.asset";

        [MenuItem("Fishing/Build Character Library")]
        public static void Run() => Ensure();

        public static void Ensure()
        {
            var lib = AssetDatabase.LoadAssetAtPath<CharacterLibrary>(LibPath);
            if (lib == null)
            {
                lib = ScriptableObject.CreateInstance<CharacterLibrary>();
                AssetDatabase.CreateAsset(lib, LibPath);
            }
            // 머리카락·치마가 흔들리는 판(SpringBone)
            lib.unityChan = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/unity-chan!/Unity-chan! Model/Prefabs/unitychan_dynamic.prefab")
                            ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/unity-chan!/Unity-chan! Model/Prefabs/unitychan.prefab");
            lib.zombie = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Zombie/ZombieRig.prefab")
                         ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Zombie/ZombieRig.fbx");
            lib.xBot = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MixamoCharacters/XBot.fbx");
            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Characters] 유니티짱 {(lib.unityChan != null)}, 좀비 {(lib.zombie != null)}, X Bot {(lib.xBot != null)}");
        }
    }
}
