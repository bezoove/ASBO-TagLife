using UnityEngine;

namespace ASBO.Slice
{
    /// <summary>
    /// Small, random, out-of-step movements for people sat down or stood about: looking round, over a shoulder,
    /// scratching an arm, a sip of a drink, a sniff of an armpit, a shrug, rubbing the head after a loss, a few words
    /// with their hands. Played on the controller's "Fidget" layer, added on top of whatever the body is doing (upper
    /// body only), so a seated person stays seated. Each person has their own temper: some fidget a lot, some now and
    /// then, a few sit perfectly still. Nothing plays while they walk, talk, fight or lie hurt.
    /// </summary>
    public class Fidget : MonoBehaviour
    {
        /// <summary>The movements (clip path, how often relative to the others). The editor builds the layer from this list.</summary>
        public static readonly (string path, float often)[] Clips =
        {
            ("Assets/MoCapCentral/MC_Sample/Animations/Idle/MCU_am_Stand_Idle_03_LookAround.FBX", 3f),
            ("Assets/MoCapCentral/MC_Sample/Animations/Idle/MCU_am_Stand_Idle_06_ScratchArm.FBX", 2f),
            ("Assets/MoCapCentral/MC_Sample/Animations/Idle/MCU_am_Stand_Conv_Talk_05_Generic.FBX", 1f),
            ("Assets/MoCapCentral/MC_Sample/Animations/Emotions/MCU_am_Stand_Emotion_Sad_01.FBX", 0.6f),
            ("Assets/MoCapCentral/MC_Sample/Animations/Emotions/MCU_am_Stand_Emotion_ThisGuy_01.FBX", 0.6f),
            ("Assets/MoCapCentral/MC_Sample/Animations/Emotions/MCU_am_Stand_Emotion_Frustrated_01_GrabHead.FBX", 0.5f),
            ("Assets/MoCapCentral/MC_Sample/Animations/Stinky/MCU_am_Stand_Idle_StinkyPits.FBX", 0.3f),
            ("Assets/MoCapCentral/MC_Sample/Animations/Moonshine/MCU_am_Moonshine_01_Drink_Loop.FBX", 1.5f),
            ("Assets/MoCapCentral/MC_Sample/Animations/Idle/MCU_am_Stand_LookAt_05_PointRHand.FBX", 0.5f),
            ("Assets/DoubleL/Demo/Anim/Dialogue_2.anim", 0.8f),
            ("Assets/DoubleL/Demo/Anim/Dialogue_3.anim", 0.8f),
            ("Assets/DoubleL/Demo/Anim/Dialogue_4.anim", 0.8f),
            ("mixamo:MX_LookBack", 1.5f),
            ("Assets/Kevin Iglesias/Human Animations/Animations/Male/Social/Conversation/HumanM@Talk01.fbx", 0.8f),
            ("Assets/MoCapCentral/MC_Sample/Animations/Emotions/MCU_am_Stand_React_Excited_01.FBX", 0.15f),
        };

        Person p; Animator anim; int layer = -2, playing = -1; float next, weight, target, calm, started;
        bool hasSit, hasTalk;

        void OnEnable() { next = Time.time + Random.Range(1.5f, 9f); playing = -1; weight = 0f; target = 0f; }
        void Start() { p = GetComponent<Person>(); calm = Random.value; }

        void Update()
        {
            if (layer == -2)
            {
                if (p == null || p.anim == null || !p.anim.isActiveAndEnabled || p.anim.runtimeAnimatorController == null) return;
                anim = p.anim; layer = anim.GetLayerIndex("Fidget");
                foreach (var q in anim.parameters) { if (q.name == "Sit") hasSit = true; if (q.name == "Talk") hasTalk = true; }
                if (layer < 0 || calm > 0.88f) { if (layer >= 0) anim.SetLayerWeight(layer, 0f); enabled = false; return; } // a few just sit still
            }
            if (anim == null || !anim.isActiveAndEnabled) return;
            bool sat = hasSit && anim.GetBool("Sit");
            bool still = p.state == Person.State.Walking && !p.hurt && (sat || anim.velocity.sqrMagnitude < 0.01f && (!hasTalk || !anim.GetBool("Talk")));
            if (!still) { target = 0f; playing = -1; next = Mathf.Max(next, Time.time + 2f); }
            else if (playing < 0 && Time.time > next)
            {
                // pick one, the common ones more often
                float sum = 0f; foreach (var c in Clips) sum += c.often;
                float r = Random.value * sum; int k = 0;
                for (; k < Clips.Length - 1; k++) { r -= Clips[k].often; if (r <= 0f) break; }
                anim.CrossFadeInFixedTime("F" + k, 0.3f, layer, 0f);
                playing = k; started = Time.time; target = Random.Range(0.35f, 0.95f) * Mathf.Lerp(1f, 0.6f, calm);
            }
            else if (playing >= 0 && Time.time > started + 0.6f && anim.GetCurrentAnimatorStateInfo(layer).IsName("Still"))
            {
                playing = -1; target = 0f; next = Time.time + Random.Range(3f, 11f) * Mathf.Lerp(0.6f, 2.2f, calm);
            }
            weight = Mathf.MoveTowards(weight, target, Time.deltaTime * 1.6f);
            anim.SetLayerWeight(layer, weight);
        }
    }
}
