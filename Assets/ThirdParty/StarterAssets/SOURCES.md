# Unity Starter Assets – Third Person (동작 클립만)

- 출처: Unity Technologies, [Starter Assets – Third Person](https://assetstore.unity.com/packages/essentials/starter-assets-thirdperson-updates-in-new-charactercontroller-pa-196526) (Asset Store, 무료)
- 라이선스: [Unity Companion License](https://unity.com/legal/licenses/unity-companion-license) — Unity 프로젝트 안에서 쓰고 나눌 수 있다.
- 남긴 것: `Animations/`의 클립 FBX 5개(Stand--Idle, Locomotion--Walk_N, Locomotion--Run_N, Jump--Jump, Jump--InAir)와 그 아바타 원본 `Armature.fbx`. 나머지(컨트롤러 스크립트·로봇 Kyle·샘플 맵·URP 설정)는 쓰지 않아 지웠다.
- 게임은 이 FBX를 바로 쓰지 않고, `Assets/Editor/StarterClipBake.cs`가 Y축 180° 돌려 `Assets/Resources/Animations/StarterAssets/*.anim`으로 구운 클립을 쓴다 (docs/10-animation.md 7장).
