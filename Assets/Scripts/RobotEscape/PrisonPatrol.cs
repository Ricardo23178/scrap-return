using UnityEngine;

namespace RobotEscape
{
    public sealed class PrisonPatrol : MonoBehaviour
    {
        public SpriteRenderer guard, guardInCell, warningLamp;
        public Sprite[] guardPoses;
        public AudioSource audioSource;
        public float CycleTime { get; private set; }
        public bool NearCell { get { return !Distracted && (CycleTime < 3f || CycleTime >= 12f); } }
        public bool Distracted { get { return distractedRemaining > 0; } }
        public int AlertLevel { get; private set; }
        private float distractedRemaining, warningRemaining, nextFootstep;
        private Color lampColor, guardColor;
        private AudioClip footstep, knock, bell, metal;

        private void Awake()
        {
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false; audioSource.spatialBlend = 0;
            lampColor = warningLamp != null ? warningLamp.color : new Color(1, .6f, .2f, .2f);
            guardColor = guard != null ? guard.color : Color.white;
            footstep = Tone("Metal footstep", 95, .09f, .12f);
            knock = Tone("Bar knock", 320, .18f, .22f);
            bell = Tone("Clock bell", 740, .75f, .18f);
            metal = Tone("Rusty hinge", 170, .2f, .11f);
        }

        private void Update()
        {
            CycleTime = Mathf.Repeat(CycleTime + Time.deltaTime, 14f);
            distractedRemaining = Mathf.Max(0, distractedRemaining - Time.deltaTime);
            warningRemaining = Mathf.Max(0, warningRemaining - Time.deltaTime);
            if (warningRemaining <= 0) AlertLevel = 0;
            UpdateVisuals();
            bool walking = !Distracted && ((CycleTime >= 3 && CycleTime < 6) || CycleTime >= 10);
            if (walking && Time.time >= nextFootstep)
            {
                nextFootstep = Time.time + .58f;
                if (audioSource != null) audioSource.PlayOneShot(footstep, NearCell ? .9f : .35f);
            }
        }

        public bool ReportNoise()
        {
            if (!NearCell) return false;
            bool caught = AlertLevel > 0 && warningRemaining > 0;
            AlertLevel = caught ? 2 : 1;
            warningRemaining = 4;
            PlayKnock();
            UpdateVisuals();
            return caught;
        }

        public void Distract(float seconds)
        {
            distractedRemaining = Mathf.Max(distractedRemaining, Mathf.Max(0, seconds));
            warningRemaining = 0; AlertLevel = 0;
            if (audioSource != null) audioSource.PlayOneShot(bell);
            UpdateVisuals();
        }

        public void PlayKnock() { if (audioSource != null) audioSource.PlayOneShot(knock); }
        public void PlayMetal() { if (audioSource != null) audioSource.PlayOneShot(metal); }

        public void ResetPatrol()
        {
            CycleTime = distractedRemaining = warningRemaining = 0;
            nextFootstep = Time.time + .5f; AlertLevel = 0;
            if (audioSource != null) audioSource.Stop();
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            float x = CycleTime < 3 ? -5.2f : CycleTime < 6 ? Mathf.Lerp(-5.2f, 5.2f, (CycleTime - 3) / 3) :
                CycleTime < 10 ? 5.2f : Mathf.Lerp(5.2f, -5.2f, (CycleTime - 10) / 4);
            if (Distracted) x = -5.2f;
            if (guard != null)
            {
                Vector3 position = guard.transform.localPosition; position.x = x;
                guard.transform.localPosition = position;
                guard.flipX = !Distracted && CycleTime >= 10;
                guard.color = AlertLevel > 0 ? Color.Lerp(guardColor, new Color(1f, .63f, .35f), .25f) : guardColor;
                if (guardPoses != null && guardPoses.Length > 0)
                {
                    int pose = AlertLevel > 0 ? guardPoses.Length - 1 : Distracted || CycleTime < 3 || (CycleTime >= 6 && CycleTime < 10) ? 0 : 1 + Mathf.FloorToInt(Time.time * 4) % Mathf.Max(1, guardPoses.Length - 2);
                    guard.sprite = guardPoses[Mathf.Clamp(pose, 0, guardPoses.Length - 1)];
                }
            }
            if (guardInCell != null)
            {
                guardInCell.enabled = NearCell || Distracted;
                if (guardPoses != null && guardPoses.Length > 0) guardInCell.sprite = guardPoses[AlertLevel > 0 ? guardPoses.Length - 1 : 0];
            }
            if (warningLamp != null)
            {
                float strength = NearCell || Distracted ? .55f : .08f;
                Color color = AlertLevel > 0 ? new Color(1, .38f, .12f, strength + Mathf.Sin(Time.time * 13) * .12f) : lampColor;
                if (AlertLevel == 0) color.a = strength;
                warningLamp.color = color;
            }
        }

        private static AudioClip Tone(string name, float frequency, float length, float volume)
        {
            const int rate = 22050;
            float[] samples = new float[Mathf.CeilToInt(rate * length)];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / rate, fade = Mathf.Exp(-t * 8 / length);
                samples[i] = (Mathf.Sin(t * frequency * Mathf.PI * 2) + Mathf.Sin(t * frequency * 2.71f * Mathf.PI * 2) * .3f) * fade * volume;
            }
            AudioClip clip = AudioClip.Create(name, samples.Length, 1, rate, false); clip.SetData(samples, 0); return clip;
        }

        private void OnDestroy()
        {
            foreach (AudioClip clip in new[] { footstep, knock, bell, metal }) if (clip != null) Destroy(clip);
        }
    }
}
