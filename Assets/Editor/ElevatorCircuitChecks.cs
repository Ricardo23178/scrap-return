using System;
using RobotEscape;

public static class ElevatorCircuitChecks
{
    private static int checks;
    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception(message);
    }

    private static void Draw(ElevatorCircuit circuit, int[] route, bool backwards)
    {
        circuit.Clear();
        for (int i = 0; i < route.Length; i++)
        {
            int a = route[i], b = route[(i + 1) % route.Length];
            circuit.ToggleEdge(backwards ? b : a, backwards ? a : b);
        }
    }

    [UnityEditor.MenuItem("Tools/Robot Escape/Tests/Verify Elevator Circuit")]
    public static void Run()
    {
        var circuit = new ElevatorCircuit();
        Check(circuit.LampCount == 6 && !circuit.HasLamp(1) && !circuit.HasLamp(6), "Wrong initial lamps");
        Check(!circuit.ToggleEdge(0, 1) && !circuit.ToggleEdge(6, 7) && !circuit.ToggleEdge(-1, 0) && !circuit.ToggleEdge(0, 8), "Missing/invalid socket was connected");
        Check(!circuit.InstallLamp(0) && !circuit.InstallLamp(8), "Duplicate or invalid lamp installation accepted");
        Draw(circuit, ElevatorCircuit.Routes[0], false);
        Check(circuit.MatchedFloor == ElevatorCircuit.Unmatched, "Six lamps reached the tower");
        Draw(circuit, ElevatorCircuit.Routes[1], false);
        Check(circuit.MatchedFloor == 1, "Initial lamps cannot reach floor 1");
        Draw(circuit, ElevatorCircuit.Routes[2], true);
        Check(circuit.MatchedFloor == 0, "Reverse wiring cannot reach floor 0");
        circuit.ToggleEdge(0, 3);
        Check(circuit.MatchedFloor == ElevatorCircuit.Unmatched, "An extra wire was ignored");
        circuit.ToggleEdge(3, 0);
        Check(circuit.MatchedFloor == 0, "Reversed duplicate failed to remove a wire");
        circuit.Clear();
        circuit.SelectNode(0); circuit.SelectNode(3); circuit.SelectNode(7);
        Check(circuit.SelectedNode == 7 && circuit.Edges.Count == 2, "Click-to-connect chain incorrect");
        circuit.SelectNode(7);
        Check(circuit.SelectedNode == -1 && circuit.Edges.Count == 2, "Same-node click did not cancel selection");
        Check(circuit.InstallLamp(1) && circuit.LampCount == 7, "Seventh lamp was not installed");
        Draw(circuit, ElevatorCircuit.Routes[0], true);
        Check(circuit.MatchedFloor == 2, "Seven lamps cannot reach the tower");
        Draw(circuit, ElevatorCircuit.Routes[3], false);
        Check(circuit.MatchedFloor == ElevatorCircuit.Unmatched, "Basement accessible with seven lamps");
        Check(circuit.RemoveLamp(1) && !circuit.RemoveLamp(1) && !circuit.RemoveLamp(0) && circuit.LampCount == 6, "Lamp removal allowed original lamps or duplicated an item");
        foreach (int edge in circuit.Edges) Check(edge / 8 != 1 && edge % 8 != 1, "Dangling edge remains after removal");
        Check(circuit.InstallLamp(6) && circuit.RemoveLamp(6) && circuit.InstallLamp(1), "Wrong-slot placement cannot be corrected");
        Check(circuit.InstallLamp(6) && circuit.LampCount == 8, "Eighth lamp was not installed");
        for (int i = 0; i < ElevatorCircuit.Routes.Length; i++)
        {
            Draw(circuit, ElevatorCircuit.Routes[i], i % 2 == 0);
            Check(circuit.MatchedFloor == ElevatorCircuit.Floors[i], "Exact pattern mismatch at index " + i);
        }
        circuit.Clear();
        Check(circuit.Edges.Count == 0 && circuit.SelectedNode == -1 && circuit.LampCount == 8 && circuit.MatchedFloor == ElevatorCircuit.Unmatched, "Clear erased lamps or retained a solution");
        UnityEngine.Debug.Log("PASS ElevatorCircuit: " + checks + " checks");
    }
}
