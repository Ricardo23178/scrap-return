using System;
using System.Collections.Generic;
using UnityEngine;

namespace RobotEscape
{
    public sealed class WalkableArea2D : MonoBehaviour
    {
        // World-space foot positions; these must not move with the character transform.
        public Vector2[] boundary;
        public Rect[] obstacles;
        public float radius = .08f;

        private float Clearance { get { return Mathf.Max(.00001f, radius); } }

        public bool Contains(Vector2 point)
        {
            if (boundary == null || boundary.Length < 3 || !Finite(point)) return false;
            bool inside = false;
            float clearanceSquared = Clearance * Clearance;
            for (int i = 0, j = boundary.Length - 1; i < boundary.Length; j = i++)
            {
                Vector2 a = boundary[j], b = boundary[i];
                if (PointSegmentDistanceSquared(point, a, b) < clearanceSquared) return false;
                if ((a.y > point.y) != (b.y > point.y) &&
                    point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                    inside = !inside;
            }
            if (!inside) return false;
            if (obstacles != null)
                foreach (Rect obstacle in obstacles)
                {
                    Rect expanded = Expand(obstacle, Clearance);
                    if (point.x >= expanded.xMin && point.x <= expanded.xMax &&
                        point.y >= expanded.yMin && point.y <= expanded.yMax) return false;
                }
            return true;
        }

        public Vector2 ConstrainStep(Vector2 from, Vector2 to)
        {
            if (!Contains(from) || !Finite(to)) return from;
            if (SegmentAllowed(from, to)) return to;
            // Prefix validity is monotonic even when the requested segment exits and re-enters the floor.
            float low = 0, high = 1;
            for (int i = 0; i < 24; i++)
            {
                float middle = (low + high) * .5f;
                if (SegmentAllowed(from, Vector2.Lerp(from, to, middle))) low = middle;
                else high = middle;
            }
            return Vector2.Lerp(from, to, low);
        }

        public bool TryPath(Vector2 from, Vector2 target, List<Vector2> path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            path.Clear();
            if (!Contains(from) || !Contains(target)) return false;
            if ((from - target).sqrMagnitude < .0000000001f) return true;
            if (SegmentAllowed(from, target)) { path.Add(target); return true; }

            var nodes = new List<Vector2> { from, target };
            float winding = 0;
            for (int i = 0; i < boundary.Length; i++)
                winding += Cross(boundary[i], boundary[(i + 1) % boundary.Length]);
            winding = winding >= 0 ? 1 : -1;
            float inset = Clearance + .001f;
            for (int i = 0; i < boundary.Length; i++)
            {
                Vector2 corner = boundary[i];
                Vector2 incoming = (corner - boundary[(i + boundary.Length - 1) % boundary.Length]).normalized;
                Vector2 outgoing = (boundary[(i + 1) % boundary.Length] - corner).normalized;
                Vector2 normal = new Vector2(-incoming.y, incoming.x) * winding;
                Vector2 bisector = (normal + new Vector2(-outgoing.y, outgoing.x) * winding).normalized;
                float projection = Vector2.Dot(bisector, normal);
                if (projection > .0001f) AddNode(nodes, corner + bisector * (inset / projection));
            }
            if (obstacles != null)
                foreach (Rect obstacle in obstacles)
                {
                    Rect expanded = Expand(obstacle, inset);
                    AddNode(nodes, new Vector2(expanded.xMin, expanded.yMin));
                    AddNode(nodes, new Vector2(expanded.xMin, expanded.yMax));
                    AddNode(nodes, new Vector2(expanded.xMax, expanded.yMin));
                    AddNode(nodes, new Vector2(expanded.xMax, expanded.yMax));
                }

            // The scene polygons are small; a visibility graph avoids a grid missing narrow strips.
            var distance = new float[nodes.Count];
            var previous = new int[nodes.Count];
            var visited = new bool[nodes.Count];
            for (int i = 0; i < nodes.Count; i++) { distance[i] = float.PositiveInfinity; previous[i] = -1; }
            distance[0] = 0;
            for (int iteration = 0; iteration < nodes.Count; iteration++)
            {
                int current = -1;
                for (int i = 0; i < nodes.Count; i++)
                    if (!visited[i] && (current < 0 || distance[i] < distance[current])) current = i;
                if (current < 0 || float.IsPositiveInfinity(distance[current])) return false;
                if (current == 1)
                {
                    for (int i = 1; i != 0; i = previous[i]) path.Add(nodes[i]);
                    path.Reverse();
                    return true;
                }
                visited[current] = true;
                for (int next = 0; next < nodes.Count; next++)
                {
                    if (visited[next]) continue;
                    float candidate = distance[current] + Vector2.Distance(nodes[current], nodes[next]);
                    if (candidate < distance[next] && SegmentAllowed(nodes[current], nodes[next]))
                    { distance[next] = candidate; previous[next] = current; }
                }
            }
            return false;
        }

        private void AddNode(List<Vector2> nodes, Vector2 point)
        {
            if (!Contains(point)) return;
            foreach (Vector2 existing in nodes)
                if ((existing - point).sqrMagnitude < .00000001f) return;
            nodes.Add(point);
        }

        private bool SegmentAllowed(Vector2 from, Vector2 to)
        {
            if (!Contains(from) || !Contains(to)) return false;
            float clearanceSquared = Clearance * Clearance;
            for (int i = 0; i < boundary.Length; i++)
                if (SegmentDistanceSquared(from, to, boundary[i], boundary[(i + 1) % boundary.Length]) < clearanceSquared)
                    return false;
            if (obstacles != null)
                foreach (Rect obstacle in obstacles)
                {
                    Rect expanded = Expand(obstacle, Clearance);
                    float enter = 0, exit = 1;
                    if (Clip(from.x, to.x - from.x, expanded.xMin, expanded.xMax, ref enter, ref exit) &&
                        Clip(from.y, to.y - from.y, expanded.yMin, expanded.yMax, ref enter, ref exit)) return false;
                }
            return true;
        }

        private static bool Clip(float origin, float delta, float min, float max, ref float enter, ref float exit)
        {
            if (Mathf.Abs(delta) < .0000001f) return origin >= min && origin <= max;
            float a = (min - origin) / delta, b = (max - origin) / delta;
            enter = Mathf.Max(enter, Mathf.Min(a, b));
            exit = Mathf.Min(exit, Mathf.Max(a, b));
            return enter <= exit;
        }

        private static Rect Expand(Rect rect, float amount)
        {
            return Rect.MinMaxRect(Mathf.Min(rect.xMin, rect.xMax) - amount,
                Mathf.Min(rect.yMin, rect.yMax) - amount, Mathf.Max(rect.xMin, rect.xMax) + amount,
                Mathf.Max(rect.yMin, rect.yMax) + amount);
        }

        private static float SegmentDistanceSquared(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            Vector2 ab = b - a, cd = d - c;
            float denominator = Cross(ab, cd);
            if (Mathf.Abs(denominator) > .00000001f)
            {
                float t = Cross(c - a, cd) / denominator, u = Cross(c - a, ab) / denominator;
                if (t >= 0 && t <= 1 && u >= 0 && u <= 1) return 0;
            }
            return Mathf.Min(Mathf.Min(PointSegmentDistanceSquared(a, c, d), PointSegmentDistanceSquared(b, c, d)),
                Mathf.Min(PointSegmentDistanceSquared(c, a, b), PointSegmentDistanceSquared(d, a, b)));
        }

        private static float PointSegmentDistanceSquared(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 direction = b - a;
            float t = direction.sqrMagnitude > 0 ? Mathf.Clamp01(Vector2.Dot(point - a, direction) / direction.sqrMagnitude) : 0;
            return (point - (a + direction * t)).sqrMagnitude;
        }

        private static float Cross(Vector2 a, Vector2 b) { return a.x * b.y - a.y * b.x; }
        private static bool Finite(Vector2 point)
        {
            return !float.IsNaN(point.x) && !float.IsNaN(point.y) && !float.IsInfinity(point.x) && !float.IsInfinity(point.y);
        }
    }
}
