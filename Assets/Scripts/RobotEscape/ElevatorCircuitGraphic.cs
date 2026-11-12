using UnityEngine;
using UnityEngine.UI;

namespace RobotEscape
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ElevatorCircuitGraphic : Graphic
    {
        [System.NonSerialized] public ElevatorCircuit circuit;
        public int clueIndex = -1;
        public bool showNodes = true;

        public static Vector2 NodePosition(int index, Rect rect)
        {
            float angle = index * Mathf.PI / 4f;
            return rect.center + new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * Mathf.Min(rect.width, rect.height) * (210f / 540f);
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = rectTransform.rect;
            float unit = Mathf.Min(rect.width, rect.height) / 540f;
            if (clueIndex >= 0)
            {
                if (clueIndex >= ElevatorCircuit.Routes.Length) return;
                int[] route = ElevatorCircuit.Routes[clueIndex];
                Color chalk = Color.white;
                for (int i = 0; i < route.Length; i++)
                {
                    Vector2 a = NodePosition(route[i], rect), b = NodePosition(route[(i + 1) % route.Length], rect);
                    Line(mesh, a, b, Mathf.Max(1f, 17f * unit), chalk);
                    if (showNodes) Disc(mesh, a, 20f * unit, 20f * unit, chalk);
                }
                return;
            }
            if (circuit != null)
                foreach (int edge in circuit.Edges)
                {
                    Vector2 a = NodePosition(edge / 8, rect), b = NodePosition(edge % 8, rect);
                    Line(mesh, a, b, 7f * unit, new Color32(38, 40, 27, 255));
                    Line(mesh, a, b, 3f * unit, new Color32(222, 189, 113, 255));
                }
            if (!showNodes) return;
            for (int i = 0; i < 8; i++)
            {
                Vector2 p = NodePosition(i, rect);
                if (circuit != null && circuit.SelectedNode == i)
                    Ring(mesh, p, 29f * unit, 2.5f * unit, new Color32(245, 219, 148, 255));
                bool installed = circuit != null ? circuit.HasLamp(i) : i != 1 && i != 6;
                Lamp(mesh, p, unit, installed);
            }
        }

        private void Lamp(VertexHelper mesh, Vector2 p, float u, bool installed)
        {
            Vector2 foot = p + new Vector2(0, -8f * u);
            Disc(mesh, foot, 24f * u, 13f * u, new Color32(55, 45, 26, 255));
            Disc(mesh, foot, 21f * u, 10f * u, new Color32(165, 132, 76, 255));
            Disc(mesh, foot, 16f * u, 7f * u, new Color32(34, 36, 25, 255));
            if (!installed)
            {
                Disc(mesh, foot, 7f * u, 4f * u, new Color32(9, 14, 10, 255));
                Disc(mesh, foot + Vector2.left * 16f * u, 2.5f * u, 2.5f * u, new Color32(201, 181, 129, 255));
                Disc(mesh, foot + Vector2.right * 16f * u, 2.5f * u, 2.5f * u, new Color32(201, 181, 129, 255));
                return;
            }
            Vector2[] lens = {
                new Vector2(-14, -4), new Vector2(-14, 10), new Vector2(-8, 24), new Vector2(-4, 31),
                new Vector2(0, 34), new Vector2(4, 31), new Vector2(8, 24), new Vector2(14, 10), new Vector2(14, -4)
            };
            int start = mesh.currentVertCount;
            Add(mesh, p + Vector2.up * 8f * u, new Color32(211, 168, 78, 255));
            for (int i = 0; i < lens.Length; i++)
                Add(mesh, p + lens[i] * u, i < 5 ? new Color32(221, 182, 97, 255) : new Color32(137, 97, 37, 255));
            for (int i = 0; i < lens.Length; i++)
            {
                mesh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % lens.Length);
                Line(mesh, p + lens[i] * u, p + lens[(i + 1) % lens.Length] * u, 2.4f * u, new Color32(76, 55, 29, 255));
            }
            Line(mesh, p + new Vector2(-7, 1) * u, p + new Vector2(-3, 23) * u, 2f * u, new Color32(240, 213, 149, 255));
            Disc(mesh, foot, 18f * u, 4f * u, new Color32(170, 132, 68, 255));
        }

        private void Add(VertexHelper mesh, Vector2 p, Color shade)
        {
            UIVertex v = UIVertex.simpleVert;
            v.position = p;
            v.color = shade * color;
            mesh.AddVert(v);
        }

        private void Line(VertexHelper mesh, Vector2 a, Vector2 b, float width, Color shade)
        {
            Vector2 normal = new Vector2(-(b - a).y, (b - a).x).normalized * width * .5f;
            int start = mesh.currentVertCount;
            Add(mesh, a + normal, shade); Add(mesh, b + normal, shade);
            Add(mesh, b - normal, shade); Add(mesh, a - normal, shade);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }

        private void Disc(VertexHelper mesh, Vector2 center, float rx, float ry, Color shade)
        {
            const int count = 24;
            int start = mesh.currentVertCount;
            Add(mesh, center, shade);
            for (int i = 0; i < count; i++)
            {
                float a = i * Mathf.PI * 2f / count;
                Add(mesh, center + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry), shade);
            }
            for (int i = 0; i < count; i++) mesh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % count);
        }

        private void Ring(VertexHelper mesh, Vector2 center, float radius, float width, Color shade)
        {
            for (int i = 0; i < 32; i++)
            {
                float a = i * Mathf.PI / 16f, b = (i + 1) * Mathf.PI / 16f;
                Line(mesh, center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius,
                    center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius, width, shade);
            }
        }
    }
}
