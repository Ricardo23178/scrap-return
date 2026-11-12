using System.Collections.Generic;

namespace RobotEscape
{
    public sealed class ElevatorCircuit
    {
        public const int SoilSocket = 1;
        public const int TowerSocket = 6;
        public const int Unmatched = int.MinValue;
        public static readonly int[][] Routes = {
            new[] { 0, 1, 2, 5, 3, 7, 4 },
            new[] { 0, 3, 7, 2, 5, 4 },
            new[] { 0, 2, 3, 4, 5, 7 },
            new[] { 0, 3, 6, 1, 4, 7, 2, 5 }
        };
        public static readonly int[] Floors = { 2, 1, 0, -1 };

        private readonly HashSet<int> edges = new HashSet<int>();
        public int InstalledMask { get; private set; }
        public int SelectedNode { get; private set; }
        public IReadOnlyCollection<int> Edges { get { return edges; } }

        public ElevatorCircuit()
        {
            InstalledMask = 189;
            SelectedNode = -1;
        }

        public int LampCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < 8; i++) if (HasLamp(i)) count++;
                return count;
            }
        }

        public bool HasLamp(int node)
        {
            return node >= 0 && node < 8 && (InstalledMask & (1 << node)) != 0;
        }

        public bool InstallLamp(int node)
        {
            if (node < 0 || node >= 8 || HasLamp(node)) return false;
            InstalledMask |= 1 << node;
            CancelSelection();
            return true;
        }

        public bool RemoveLamp(int node)
        {
            if ((node != SoilSocket && node != TowerSocket) || !HasLamp(node)) return false;
            InstalledMask &= ~(1 << node);
            edges.RemoveWhere(edge => edge / 8 == node || edge % 8 == node);
            CancelSelection();
            return true;
        }

        public bool ToggleEdge(int first, int second)
        {
            if (first == second || !HasLamp(first) || !HasLamp(second)) return false;
            int edge = Key(first, second);
            if (!edges.Remove(edge)) edges.Add(edge);
            return true;
        }

        public bool SelectNode(int node)
        {
            if (!HasLamp(node)) return false;
            if (SelectedNode == node) { CancelSelection(); return true; }
            if (SelectedNode >= 0) ToggleEdge(SelectedNode, node);
            SelectedNode = node;
            return true;
        }

        public void CancelSelection() { SelectedNode = -1; }
        public void Clear() { edges.Clear(); CancelSelection(); }

        public int MatchedFloor
        {
            get
            {
                for (int i = 0; i < Routes.Length; i++)
                {
                    int[] route = Routes[i];
                    if (route.Length != edges.Count) continue;
                    bool matches = true;
                    for (int j = 0; j < route.Length; j++)
                    {
                        int first = route[j], second = route[(j + 1) % route.Length];
                        if (!HasLamp(first) || !HasLamp(second) || !edges.Contains(Key(first, second)))
                        { matches = false; break; }
                    }
                    if (matches) return Floors[i];
                }
                return Unmatched;
            }
        }

        private static int Key(int a, int b) { return a < b ? a * 8 + b : b * 8 + a; }
    }
}
