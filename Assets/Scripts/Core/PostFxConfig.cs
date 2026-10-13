using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace Fishing.Core
{
    /// <summary>
    /// Post Processing Stack v2의 리소스(셰이더·텍스처 묶음)를 빌드에 넣기 위한 참조.
    /// Assets/Resources/PostFxConfig.asset — 메뉴 Fishing/Setup Project나 빌드할 때 에디터가 만든다(ProjectTools.EnsurePostFx).
    /// </summary>
    public class PostFxConfig : ScriptableObject
    {
        public PostProcessResources resources;
    }
}
