using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace RobotEscape.Editor
{
    public static class RobotWalkerStepChecks
    {
        // Run during PlayMode so the real Update uses Unity's current frame budget.
        [UnityEditor.MenuItem("Tools/Robot Escape/Tests/Verify Walking Step Boundaries (Play Mode)")]
        public static void Run()
        {
            if (!Application.isPlaying || Time.deltaTime <= 0) throw new InvalidOperationException("PlayMode frame time is required.");
            var fixture = new GameObject("Tiny waypoint regression");
            var failures = new List<string>();
            try
            {
                var area = fixture.AddComponent<WalkableArea2D>();
                area.boundary = new[] { new Vector2(-1,-1), new Vector2(3,-1), new Vector2(3,1), new Vector2(-1,1) };
                area.obstacles = new Rect[0]; area.radius = .08f;
                var walker = fixture.AddComponent<RobotWalker>();
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                var route = (List<Vector2>)typeof(RobotWalker).GetField("route", flags).GetValue(walker);
                var update = typeof(RobotWalker).GetMethod("Update", flags);
                float budget = walker.speed * Time.deltaTime;
                if (budget >= 1) throw new InvalidOperationException("Wait for a settled frame before waypoint regression.");
                float tiny = Mathf.Min(.000005f, budget * .1f);
                for (int test = 0; test < 2; test++)
                {
                    fixture.transform.position = Vector3.zero; walker.Stop();
                    route.Add(Vector2.right * (test == 0 ? tiny : budget - tiny));
                    route.Add(Vector2.right * 2);
                    update.Invoke(walker, null);
                    if (!walker.IsMoving || route.Count == 0)
                        failures.Add(test == 0 ? "a valid tiny first waypoint cleared the remaining route" : "a tiny frame remainder after a corner cleared the remaining route");
                    if (!area.Contains(fixture.transform.position)) failures.Add("the step left legal ground");
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(fixture); }
            if (failures.Count > 0) throw new InvalidOperationException("Tiny waypoint regression: " + string.Join("; ", failures));
            Debug.Log("RobotWalkerStepChecks: tiny waypoints and frame remainders passed.");
        }
    }
}
