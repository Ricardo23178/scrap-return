using System;
using System.Collections.Generic;
using UnityEngine;

namespace RobotEscape
{
    // Run from an EditMode verification entry point; no NUnit/package dependency.
    public static class WalkableGeometryChecks
    {
        [UnityEditor.MenuItem("Tools/Robot Escape/Tests/Verify Walkable Geometry")]
        public static void Run()
        {
            var host = new GameObject("WalkableGeometryChecks");
            try
            {
                var area = host.AddComponent<WalkableArea2D>();
                var path = new List<Vector2>();
                area.radius = .1f;
                area.boundary = Box(-2, -1, 2, 1);
                area.obstacles = new Rect[0];

                Require(area.Contains(Vector2.zero), "Room center must be walkable.");
                Require(!area.Contains(new Vector2(1.95f, 0)), "Feet must keep their radius from the wall.");
                Near(area.ConstrainStep(Vector2.zero, new Vector2(8, 0)), new Vector2(1.9f, 0),
                    "An outside click must stop at the first wall.");
                path.Add(Vector2.one);
                Require(!area.TryPath(Vector2.zero, new Vector2(3, 0), path) && path.Count == 0,
                    "An invalid target must reject the route and clear stale waypoints.");
                Near(area.ConstrainStep(new Vector2(3, 0), Vector2.zero), new Vector2(3, 0),
                    "An invalid origin must not teleport through a wall.");

                area.boundary = new[]
                {
                    new Vector2(0, 0), new Vector2(6, 0), new Vector2(6, 6), new Vector2(4, 6),
                    new Vector2(4, 2), new Vector2(2, 2), new Vector2(2, 6), new Vector2(0, 6)
                };
                var from = new Vector2(1, 5);
                var target = new Vector2(5, 5);
                Near(area.ConstrainStep(from, target), new Vector2(1.9f, 5),
                    "Legal endpoints must not allow crossing a concave floor's gap.");
                Require(area.TryPath(from, target, path), "A concave floor must allow the available detour.");
                CheckRoute(area, from, target, path);
                Require(path.Exists(p => p.y < 1.91f), "The U-shaped floor route must pass below its notch.");
                // The same floor with opposite vertex winding must behave identically.
                Array.Reverse(area.boundary);
                Require(area.TryPath(from, target, path), "Clockwise polygons must route as well.");
                CheckRoute(area, from, target, path);

                area.boundary = Box(-3, -2, 3, 2);
                area.obstacles = new[] { new Rect(-.4f, -.6f, .8f, 1.2f) };
                from = new Vector2(-2, 0);
                target = new Vector2(2, 0);
                Near(area.ConstrainStep(from, target), new Vector2(-.5f, 0),
                    "A large step must stop at the first rectangular obstacle.");
                Require(!area.Contains(new Vector2(-.45f, 0)), "Obstacle clearance must include the foot radius.");
                Require(area.TryPath(from, target, path), "A valid interaction stand must route around a machine.");
                CheckRoute(area, from, target, path);
                Require(path.Exists(p => Mathf.Abs(p.y) >= .7f), "The machine route must go beyond an obstacle edge.");

                area.obstacles = new[] { new Rect(-.0005f, -2, .001f, 4) };
                Require(area.ConstrainStep(from, target).x < -.099f,
                    "Even a sub-frame thin wall must stop movement without tunneling.");
                Require(!area.TryPath(from, target, path) && path.Count == 0,
                    "A wall separating platforms must not produce a crossing route.");

                area.boundary = Box(0, 0, 6, .28f);
                area.obstacles = new Rect[0];
                area.radius = .08f;
                Require(area.Contains(new Vector2(3, .14f)), "The authored narrow upper floor must fit the feet.");
                Require(area.TryPath(new Vector2(.1f, .14f), new Vector2(5.9f, .14f), path),
                    "A narrow legal strip must remain reachable.");
                Require(path.Count == 1, "A clear strip should use a direct route.");
                Debug.Log("WalkableGeometryChecks: all assertions passed.");
            }
            finally { UnityEngine.Object.DestroyImmediate(host); }
        }

        private static Vector2[] Box(float left, float bottom, float right, float top)
        {
            return new[] { new Vector2(left, bottom), new Vector2(right, bottom),
                new Vector2(right, top), new Vector2(left, top) };
        }

        private static void CheckRoute(WalkableArea2D area, Vector2 from, Vector2 target, List<Vector2> path)
        {
            Require(path.Count > 0, "A successful route must contain the target.");
            Near(path[path.Count - 1], target, "A successful route must end at the requested stand point.");
            Require((path[0] - from).sqrMagnitude > .000001f, "The route must exclude its origin.");
            foreach (Vector2 next in path)
            {
                Near(area.ConstrainStep(from, next), next, "Each waypoint segment must be continuously walkable.");
                for (int i = 0; i <= 100; i++)
                    Require(area.Contains(Vector2.Lerp(from, next, i / 100f)), "Route samples must keep foot clearance.");
                from = next;
            }
        }

        private static void Near(Vector2 actual, Vector2 expected, string message)
        {
            Require((actual - expected).sqrMagnitude < .000004f,
                message + " Expected " + expected + "; received " + actual + ".");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
