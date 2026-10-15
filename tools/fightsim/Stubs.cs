// UnityEngine 대체: 파이팅 코드가 쓰는 Mathf·Random만 흉내 낸다 (tools/fightsim 전용, 게임 빌드에는 안 들어간다)
namespace UnityEngine
{
    public static class Mathf
    {
        public static float Abs(float v) => System.Math.Abs(v);
        public static int Abs(int v) => System.Math.Abs(v);
        public static float Min(float a, float b) => a < b ? a : b;
        public static int Min(int a, int b) => a < b ? a : b;
        public static float Max(float a, float b) => a > b ? a : b;
        public static int Max(int a, int b) => a > b ? a : b;
        public static float Clamp(float v, float a, float b) => v < a ? a : v > b ? b : v;
        public static int Clamp(int v, int a, int b) => v < a ? a : v > b ? b : v;
        public static float Clamp01(float v) => Clamp(v, 0f, 1f);
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static float InverseLerp(float a, float b, float v) => a != b ? Clamp01((v - a) / (b - a)) : 0f;
        public static float Exp(float v) => (float)System.Math.Exp(v);
        public static float Pow(float a, float b) => (float)System.Math.Pow(a, b);
        public static float Sin(float v) => (float)System.Math.Sin(v);
        public static float Cos(float v) => (float)System.Math.Cos(v);
        public static float Sqrt(float v) => (float)System.Math.Sqrt(v);
        public static float Sign(float v) => v >= 0f ? 1f : -1f;
        public static int RoundToInt(float v) => (int)System.Math.Round(v);
        public static int FloorToInt(float v) => (int)System.Math.Floor(v);
        public static int CeilToInt(float v) => (int)System.Math.Ceiling(v);
    }

    public static class Random
    {
        static System.Random _r = new System.Random(1234);
        public static void InitState(int seed) => _r = new System.Random(seed);
        public static float value => (float)_r.NextDouble();
        public static float Range(float a, float b) => a + (b - a) * (float)_r.NextDouble();
        public static int Range(int a, int b) => _r.Next(a, b);
    }
}

// 데이터 소스가 참조하는 게임 쪽 타입 (Seasons의 현재 날짜, 레벨 보상의 목장갑 id)
namespace Fishing.Core
{
    public class GameClock { public int Day = 1; }
    public static class Game { public static GameClock Clock = null; }
    public static class PlayerProfile { public const string CottonGlovesId = "acc_cottongloves"; }
}
