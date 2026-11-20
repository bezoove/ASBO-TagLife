using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>Marker: this scene uses the free Asset Store packs (UMA characters, Kevin Iglesias locomotion, RCC car).</summary>
    public class FreePackTest : MonoBehaviour
    {
        static int cached = -1;
        public static bool On { get { if (cached < 0) cached = FindFirstObjectByType<FreePackTest>() != null ? 1 : 0; return cached == 1; } }
        public static void Reset() { cached = -1; }
    }

}
