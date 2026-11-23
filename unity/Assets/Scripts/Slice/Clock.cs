using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// The day: game time runs at the old game's pace (a minute every 1.25 s, slower while you're talking), starting
    /// on the evening you get out. The sun and moon follow the clock: long warm evenings, dark blue nights you can still
    /// see in, grey early mornings. Other systems ask GameClock what time it is (residents go home and to bed, shops
    /// open and shut).
    /// </summary>
    public class GameClock : MonoBehaviour
    {
        public static GameClock I;
        public const float MinsPerSec = 0.8f;
        public static float Minute = 17 * 60 + 10;   // 17:10, the evening you're let out
        public static int Day;                          // 0 = Monday
        public static float Hour => Minute / 60f;
        public static bool Night => Minute < 6 * 60 + 30 || Minute > 22 * 60;
        static readonly string[] Days = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
        static string text; static int textMin = -1;
        /// <summary>"Mon 17:32": rebuilt only when the minute changes (the HUD asks every frame).</summary>
        public static string Text { get { int m = Day * 1440 + (int)Minute; if (m != textMin || text == null) { textMin = m; text = Days[Day % 7] + " " + ((int)(Minute / 60)).ToString("00") + ":" + ((int)(Minute % 60)).ToString("00"); } return text; } }
        public static float Speed = 1f; // tests can run the clock fast
        public static System.Action<int> NewDay;

        Light sun; float lightT; Color baseSky, baseEq, baseGround, baseFog; float baseRefl = 1f; bool haveBase;
        // indoors (in a building's sound zone) the sky light gives way to a warm room light, whatever the hour
        Color oSky, oEq, oGround; float oRefl; public static float Indoors; PlayerCtl player;
        static readonly Color inSky = new Color(0.8f, 0.7f, 0.6f), inEq = new Color(0.72f, 0.61f, 0.52f), inGround = new Color(0.42f, 0.35f, 0.3f);

        public static void Create(Transform parent) { if (I != null) return; var g = new GameObject("Clock"); g.transform.SetParent(parent, false); I = g.AddComponent<GameClock>(); }

        void Start()
        {
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.type == LightType.Directional) { sun = l; break; }
            baseSky = RenderSettings.ambientSkyColor; baseEq = RenderSettings.ambientEquatorColor; baseGround = RenderSettings.ambientGroundColor;
            baseFog = RenderSettings.fogColor; baseRefl = RenderSettings.reflectionIntensity; haveBase = true;
            Apply();
        }

        void Update()
        {
            float k = MinsPerSec * Speed * (DialogueUI.Open ? 0.4f : 1f);
            Minute += Time.deltaTime * k;
            if (Minute >= 1440f) { Minute -= 1440f; Day++; NewDay?.Invoke(Day); World.Toast(new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" }[Day % 7] + ". A new day.", 0); }
            // light changes slowly: no need to touch it every frame (and every change re-renders the shadows anyway)
            lightT -= Time.deltaTime;
            if (lightT <= 0f) { lightT = Speed > 1f ? 0.05f : 0.5f; Apply(); Resident.WakeAny(); }
            if (player == null) player = FindFirstObjectByType<PlayerCtl>();
            float want = player != null && !player.InCar && GameAudio.I != null && GameAudio.I.InsideAny(player.transform.position + Vector3.up) ? 1f : 0f;
            if (!Mathf.Approximately(Indoors, want)) { Indoors = Mathf.MoveTowards(Indoors, want, Time.deltaTime * 1.5f); Ambient(); }
        }

        void Ambient()
        {
            float t = Mathf.SmoothStep(0f, 1f, Indoors);
            RenderSettings.ambientSkyColor = Color.Lerp(oSky, inSky, t); RenderSettings.ambientEquatorColor = Color.Lerp(oEq, inEq, t);
            RenderSettings.ambientGroundColor = Color.Lerp(oGround, inGround, t); RenderSettings.reflectionIntensity = Mathf.Lerp(oRefl, 0.7f, t);
        }

        /// <summary>Sun (or moon) direction, colour and strength, and the sky light, for the current time.</summary>
        void Apply()
        {
            if (!haveBase) return;
            float h = Hour;
            // sun up 06:30 to 19:30 (a British spring): elevation follows a half sine across the day
            float dayT = Mathf.InverseLerp(6.5f, 19.5f, h);
            bool up = h > 6.5f && h < 19.5f;
            float elev = up ? Mathf.Sin(dayT * Mathf.PI) * 48f : 0f;
            float az = Mathf.Lerp(-100f, 100f, dayT);
            // dusk and dawn: warm low light; night: cool moonlight from high up, faint
            float dusk = up ? Mathf.Clamp01(1f - elev / 14f) : 0f;
            float night = Mathf.Clamp01(h < 6.5f ? (6.5f - h) / 1.2f : h > 19.5f ? (h - 19.5f) / 1.5f : 0f);
            if (sun != null)
            {
                if (up || night < 0.5f)
                {
                    sun.transform.rotation = Quaternion.Euler(Mathf.Max(elev, 4f), az - 35f, 0f);
                    sun.color = Color.Lerp(new Color(1f, 0.95f, 0.86f), new Color(1f, 0.62f, 0.38f), dusk);
                    sun.intensity = Mathf.Lerp(1.35f, 0.55f, dusk) * (1f - night * 0.9f);
                }
                else
                {
                    sun.transform.rotation = Quaternion.Euler(55f, 160f, 0f); // the moon
                    sun.color = new Color(0.55f, 0.65f, 0.9f);
                    sun.intensity = 0.28f;
                }
                sun.shadowStrength = Mathf.Lerp(0.82f, 0.5f, night);
            }
            float dark = Mathf.Lerp(0f, 0.72f, night) + dusk * 0.18f;
            Color nightSky = new Color(0.16f, 0.2f, 0.34f), nightEq = new Color(0.12f, 0.13f, 0.18f), nightGround = new Color(0.06f, 0.06f, 0.07f);
            oSky = Color.Lerp(baseSky, nightSky, dark);
            oEq = Color.Lerp(Color.Lerp(baseEq, new Color(0.75f, 0.5f, 0.38f), dusk * 0.5f), nightEq, dark);
            oGround = Color.Lerp(baseGround, nightGround, dark);
            RenderSettings.fogColor = Color.Lerp(Color.Lerp(baseFog, new Color(0.85f, 0.62f, 0.5f), dusk * 0.5f), new Color(0.08f, 0.1f, 0.15f), dark);
            oRefl = Mathf.Lerp(baseRefl, 0.25f, dark);
            Ambient();
        }

        /// <summary>Jump the clock (tests; sleeping later).</summary>
        public static void Set(float hour) { Minute = Mathf.Repeat(hour * 60f, 1440f); if (I != null) I.Apply(); }
    }
}
