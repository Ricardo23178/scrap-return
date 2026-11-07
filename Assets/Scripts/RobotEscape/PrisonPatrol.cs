using UnityEngine;

namespace RobotEscape
{
    public sealed class PrisonPatrol : MonoBehaviour
    {
        public enum ObservationPhase { None, Stand, Retrieve, Raise, Hold, Lower }

        public SpriteRenderer guard, guardInCell, warningLamp;
        public Sprite[] guardPoses, guardWalkPoses, observePoses;
        public AudioSource audioSource;
        public float CycleTime { get; private set; }
        public bool NearCell { get { return !Distracted && (CycleTime < 3f || CycleTime >= 12f); } }
        public bool Distracted { get { return distractedRemaining > 0; } }
        public int AlertLevel { get; private set; }
        public ObservationPhase ObservePhase { get; private set; }

        private static readonly Vector2 NearPoint = new Vector2(-5.2f, -2.8f);
        private static readonly Vector2 BendPoint = new Vector2(2.2f, -2.8f);
        private static readonly Vector2 FarPoint = new Vector2(5.2f, -2f);
        private static readonly float FirstLeg = Vector2.Distance(NearPoint, BendPoint);
        private static readonly float RouteLength = FirstLeg + Vector2.Distance(BendPoint, FarPoint);
        private const float WalkStride = 2.8f;
        private float distractedRemaining, warningRemaining, routeDistance, walkedDistance, stepDistance;
        private float observationElapsed, lowerRemaining;
        private bool walking, facingLeft, observationComplete;
        private Vector3 guardScale = Vector3.one, guardInCellScale = Vector3.one;
        private Color lampColor, guardColor, guardInCellColor;
        private AudioClip footstep, knock, bell, metal;

        private void Awake()
        {
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false; audioSource.spatialBlend = 0;
            lampColor = warningLamp != null ? warningLamp.color : new Color(1, .6f, .2f, .2f);
            guardColor = guard != null ? guard.color : Color.white;
            guardInCellColor = guardInCell != null ? guardInCell.color : Color.white;
            if (guard != null) guardScale = guard.transform.localScale;
            if (guardInCell != null) guardInCellScale = guardInCell.transform.localScale;
            footstep = Tone("Metal footstep", 95, .09f, .12f);
            knock = Tone("Bar knock", 320, .18f, .22f);
            bell = Tone("Clock bell", 740, .75f, .18f);
            metal = Tone("Rusty hinge", 170, .2f, .11f);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            CycleTime = Mathf.Repeat(CycleTime + dt, 14f);
            distractedRemaining = Mathf.Max(0, distractedRemaining - dt);
            warningRemaining = Mathf.Max(0, warningRemaining - dt);
            if (warningRemaining <= 0) AlertLevel = 0;
            float moved = TickMotion(dt);
            walking = moved > .00001f;
            if (walking)
            {
                walkedDistance += moved; stepDistance += moved;
                if (stepDistance >= WalkStride * .5f)
                {
                    stepDistance = Mathf.Repeat(stepDistance, WalkStride * .5f);
                    if (audioSource != null) audioSource.PlayOneShot(footstep, NearCell ? .9f : .35f);
                }
            }
            UpdateVisuals();
        }

        public bool ReportNoise()
        {
            if (!NearCell) return false;
            bool caught = AlertLevel > 0 && warningRemaining > 0;
            AlertLevel = caught ? 2 : 1;
            warningRemaining = 4;
            LowerTelescope();
            PlayKnock();
            UpdateVisuals();
            return caught;
        }

        public void Distract(float seconds)
        {
            distractedRemaining = Mathf.Max(distractedRemaining, Mathf.Max(0, seconds));
            warningRemaining = 0; AlertLevel = 0;
            if (Distracted) LowerTelescope();
            if (audioSource != null) audioSource.PlayOneShot(bell);
            UpdateVisuals();
        }

        public void PlayKnock() { if (audioSource != null) audioSource.PlayOneShot(knock); }
        public void PlayMetal() { if (audioSource != null) audioSource.PlayOneShot(metal); }

        public void ResetPatrol()
        {
            CycleTime = distractedRemaining = warningRemaining = 0;
            routeDistance = walkedDistance = stepDistance = observationElapsed = lowerRemaining = 0;
            AlertLevel = 0; ObservePhase = ObservationPhase.None;
            walking = facingLeft = observationComplete = false;
            if (audioSource != null) audioSource.Stop();
            UpdateVisuals();
        }

        private float TickMotion(float dt)
        {
            bool observationWindow = CycleTime >= 6f && CycleTime < 10f;
            if (!observationWindow) observationComplete = false;
            bool canObserve = observationWindow && !Distracted && AlertLevel == 0;
            if (!canObserve) LowerTelescope();
            if (lowerRemaining > 0)
            {
                lowerRemaining = Mathf.Max(0, lowerRemaining - dt);
                if (lowerRemaining <= 0)
                { ObservePhase = ObservationPhase.None; observationElapsed = 0; }
                return 0;
            }
            if (canObserve && routeDistance >= RouteLength - .001f && !observationComplete)
            {
                observationElapsed += dt;
                ObservePhase = observationElapsed < .2f ? ObservationPhase.Stand :
                    observationElapsed < .65f ? ObservationPhase.Retrieve :
                    observationElapsed < 1.2f ? ObservationPhase.Raise : ObservationPhase.Hold;
                if (observationElapsed >= 3.5f) LowerTelescope();
                return 0;
            }
            ObservePhase = ObservationPhase.None; observationElapsed = 0;
            float target = CycleTime < 3 ? 0 : CycleTime < 6 ? RouteLength * (CycleTime - 3) / 3 :
                CycleTime < 10 ? RouteLength : RouteLength * (14 - CycleTime) / 4;
            if (Distracted) target = 0;
            float speed = !Distracted && CycleTime >= 3 && CycleTime < 6 ? RouteLength / 3 : RouteLength / 4;
            float previous = routeDistance;
            // The clock keeps running during interruptions; follow it without snapping to its scheduled position.
            routeDistance = Mathf.MoveTowards(routeDistance, target, speed * dt);
            if (Mathf.Abs(routeDistance - previous) > .00001f) facingLeft = routeDistance < previous;
            return Mathf.Abs(routeDistance - previous);
        }

        private void LowerTelescope()
        {
            if (ObservePhase == ObservationPhase.None || lowerRemaining > 0) return;
            ObservePhase = ObservationPhase.Lower; lowerRemaining = .5f;
            observationComplete = true; walking = false;
        }

        private void UpdateVisuals()
        {
            if (guard != null)
            {
                Vector2 point = routeDistance <= FirstLeg ? Vector2.Lerp(NearPoint, BendPoint, routeDistance / FirstLeg) :
                    Vector2.Lerp(BendPoint, FarPoint, (routeDistance - FirstLeg) / (RouteLength - FirstLeg));
                Vector3 position = guard.transform.localPosition; position.x = point.x; position.y = point.y;
                guard.transform.localPosition = position;
                guard.flipX = facingLeft;
                guard.color = AlertLevel > 0 ? Color.Lerp(guardColor, new Color(1f, .63f, .35f), .25f) : guardColor;
                Sprite pose = null;
                bool newPose = false;
                if ((int)ObservePhase >= (int)ObservationPhase.Retrieve)
                {
                    pose = Pose(observePoses, (int)ObservePhase - (int)ObservationPhase.Retrieve);
                    newPose = pose != null;
                }
                if (pose == null && walking)
                {
                    int frame = guardWalkPoses == null ? 0 : Mathf.FloorToInt(walkedDistance / WalkStride * guardWalkPoses.Length);
                    pose = Pose(guardWalkPoses, frame); newPose = pose != null;
                    if (pose == null && guardPoses != null && guardPoses.Length > 2)
                        pose = guardPoses[1 + Mathf.FloorToInt(walkedDistance / .7f) % (guardPoses.Length - 2)];
                }
                if (pose == null)
                {
                    pose = IdlePose(); newPose = pose != null;
                    if (pose == null) pose = Pose(guardPoses, AlertLevel > 0 && guardPoses != null ? guardPoses.Length - 1 : 0);
                }
                ApplyPose(guard, pose, newPose, guardScale);
            }
            if (guardInCell != null)
            {
                guardInCell.enabled = NearCell || Distracted;
                guardInCell.color = AlertLevel > 0 ? Color.Lerp(guardInCellColor, new Color(1f, .63f, .35f), .25f) : guardInCellColor;
                Sprite pose = IdlePose(); bool newPose = pose != null;
                if (pose == null) pose = Pose(guardPoses, AlertLevel > 0 && guardPoses != null ? guardPoses.Length - 1 : 0);
                ApplyPose(guardInCell, pose, newPose, guardInCellScale);
            }
            if (warningLamp != null)
            {
                float strength = NearCell || Distracted ? .55f : .08f;
                Color color = AlertLevel > 0 ? new Color(1, .38f, .12f, strength + Mathf.Sin(Time.time * 13) * .12f) : lampColor;
                if (AlertLevel == 0) color.a = strength;
                warningLamp.color = color;
            }
        }

        private Sprite IdlePose()
        {
            Sprite pose = Pose(observePoses, 0);
            return pose != null ? pose : Pose(guardWalkPoses, 0);
        }

        private static void ApplyPose(SpriteRenderer renderer, Sprite pose, bool normalize, Vector3 legacyScale)
        {
            if (pose == null) return;
            renderer.sprite = pose;
            if (!normalize) { renderer.transform.localScale = legacyScale; return; }
            float parentHeight = renderer.transform.parent != null ? Mathf.Abs(renderer.transform.parent.lossyScale.y) : 1;
            float scale = 3.4f / Mathf.Max(.0001f, pose.bounds.size.y * parentHeight);
            renderer.transform.localScale = new Vector3(scale, scale, legacyScale.z);
        }

        private static Sprite Pose(Sprite[] poses, int index)
        {
            return poses != null && poses.Length > 0 ? poses[(index % poses.Length + poses.Length) % poses.Length] : null;
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
