using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RobotEscape
{
    public sealed class RobotWalker : MonoBehaviour
    {
        public Vector2 minWalk = new Vector2(-7.4f, -3.4f);
        public Vector2 maxWalk = new Vector2(7.4f, -.8f);
        public float speed = 3.2f, walkCycleDistance = 1.25f;
        public Transform body;
        public SpriteRenderer illustration, assemblyTorso;
        public Sprite[] poses, walkingPoses;
        public Transform[] assemblyLegs;
        public WalkableArea2D walkArea;
        public Vector2 Destination { get; private set; }
        public float TravelledDistance { get; private set; }
        public bool IsMoving { get { return enabled && route.Count > 0; } }
        private readonly List<Vector2> route = new List<Vector2>();
        private Vector3 bodyOrigin;
        private Vector3[] legOrigins;
        private Quaternion[] legRotations;
        private SortingGroup sortingGroup;
        private float stepDistance, artHeight;
        private bool facingLeft;

        private void Awake()
        {
            Destination = transform.position;
            if (walkArea == null) walkArea = GetComponent<WalkableArea2D>();
            sortingGroup = GetComponent<SortingGroup>();
            if (body != null) bodyOrigin = body.localPosition;
            if (illustration != null && illustration.sprite != null)
                artHeight = illustration.sprite.bounds.size.y * illustration.transform.localScale.y;
            if (assemblyLegs != null)
            {
                legOrigins = new Vector3[assemblyLegs.Length]; legRotations = new Quaternion[assemblyLegs.Length];
                for (int i = 0; i < assemblyLegs.Length; i++) if (assemblyLegs[i] != null)
                { legOrigins[i] = assemblyLegs[i].localPosition; legRotations[i] = assemblyLegs[i].localRotation; }
            }
        }

        public void MoveTo(Vector2 point)
        {
            route.Clear();
            Vector2 from = transform.position;
            if (walkArea != null && walkArea.boundary != null && walkArea.boundary.Length >= 3)
            {
                if (walkArea.Contains(point)) walkArea.TryPath(from, point, route);
                else
                {
                    Vector2 stop = walkArea.ConstrainStep(from, point);
                    if (Vector2.Distance(from, stop) > .01f) route.Add(stop);
                }
            }
            else route.Add(new Vector2(Mathf.Clamp(point.x, minWalk.x, maxWalk.x), Mathf.Clamp(point.y, minWalk.y, maxWalk.y)));
            Destination = route.Count > 0 ? route[route.Count - 1] : from;
            if (Vector2.Distance(from, Destination) < .01f) Stop();
        }

        public void Stop() { route.Clear(); Destination = transform.position; stepDistance = 0; }

        private void Update()
        {
            Vector2 before = transform.position, next = before;
            float remaining = Mathf.Max(0, speed) * Time.deltaTime;
            while (route.Count > 0 && remaining > 0)
            {
                Vector2 goal = route[0];
                Vector2 proposed = Vector2.MoveTowards(next, goal, remaining);
                Vector2 allowed = walkArea != null ? walkArea.ConstrainStep(next, proposed) : proposed;
                float moved = Vector2.Distance(next, allowed);
                next = allowed; remaining -= moved;
                if (Vector2.Distance(allowed, proposed) > .000001f)
                { route.Clear(); Destination = next; break; }
                if (Vector2.Distance(next, goal) < .000001f) route.RemoveAt(0); else break;
            }
            transform.position = new Vector3(next.x, next.y, 0);
            float distance = Vector2.Distance(before, next);
            bool stepping = distance > .00001f;
            if (stepping)
            {
                TravelledDistance += distance; stepDistance += distance;
                if (Mathf.Abs(next.x - before.x) > .0001f) facingLeft = next.x < before.x;
            }
            else stepDistance = 0;
            float scale = Mathf.Lerp(1.12f, .8f, Mathf.InverseLerp(minWalk.y, maxWalk.y, next.y));
            transform.localScale = Vector3.one * scale;
            if (sortingGroup != null) sortingGroup.sortingOrder = 100 + Mathf.RoundToInt(-next.y * 20);
            float phase = stepDistance / Mathf.Max(.1f, walkCycleDistance) * Mathf.PI * 2;
            if (body != null) body.localPosition = bodyOrigin + Vector3.up * (stepping ? -.018f * (1 - Mathf.Cos(phase * 2)) : 0);
            if (illustration != null && poses != null && poses.Length > 0)
            {
                illustration.flipX = facingLeft;
                Sprite pose = poses[0];
                if (stepping && walkingPoses != null && walkingPoses.Length > 0)
                    pose = walkingPoses[Mathf.FloorToInt(stepDistance / Mathf.Max(.1f, walkCycleDistance) * walkingPoses.Length) % walkingPoses.Length];
                else if (stepping && poses.Length > 1)
                    pose = poses[1 + Mathf.FloorToInt(stepDistance * 4) % (poses.Length - 1)];
                if (pose != null)
                {
                    illustration.sprite = pose;
                    if (artHeight > 0) illustration.transform.localScale = Vector3.one * artHeight / pose.bounds.size.y;
                }
            }
            if (assemblyTorso != null) assemblyTorso.flipX = facingLeft;
            if (assemblyLegs != null && legOrigins != null)
                for (int i = 0; i < assemblyLegs.Length; i++) if (assemblyLegs[i] != null)
                {
                    float swing = stepping ? Mathf.Sin(phase + i * Mathf.PI) : 0;
                    Vector3 origin = legOrigins[i];
                    if (facingLeft) origin.x = -origin.x;
                    assemblyLegs[i].localPosition = origin + new Vector3(swing * .065f, Mathf.Max(0, swing) * .08f, 0);
                    assemblyLegs[i].localRotation = legRotations[i] * Quaternion.Euler(0, 0, swing * (facingLeft ? -22 : 22));
                }
        }
    }
}
