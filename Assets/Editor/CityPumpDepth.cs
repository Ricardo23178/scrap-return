using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RobotEscape.Editor
{
    public static class CityPumpDepth
    {
        private const string Folder = "Assets/Art/Room05City/PumpDepth";
        private const float FrameWidth = 1672f;
        private const float FrameHeight = 941f;

        public static void Add(CityChapterController city)
        {
            if (city == null || city.plaza == null || city.gateOpenArt == null)
                throw new InvalidOperationException("The city backdrops must exist before adding pump depth.");
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/Art/Room05City", "PumpDepth");

            Mesh mesh = CreateMesh();
            Material closed = MaterialFor("Pump Closed", "Plaza.png");
            Material open = MaterialFor("Pump Open", "PlazaGateOpen.png");
            AddPatch(city.plaza.transform, "Pump Depth (Closed)", mesh, closed, -.02f);
            AddPatch(city.gateOpenArt.transform, "Pump Depth (Open)", mesh, open, -.03f);
        }

        private static Mesh CreateMesh()
        {
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();

            // Opaque regions follow the painted silhouette; gaps around the spout stay transparent.
            Polygon(vertices, uv, triangles, 672, 415, 727, 415, 733, 537, 724, 550, 676, 550, 665, 539);
            Polygon(vertices, uv, triangles, 670, 402, 727, 402, 732, 410, 726, 420, 673, 420, 666, 410);
            Polygon(vertices, uv, triangles, 666, 539, 738, 539, 748, 551, 735, 561, 675, 561, 659, 553);
            Polygon(vertices, uv, triangles, 617, 509, 667, 509, 663, 559, 652, 571, 628, 571, 620, 559);
            Polygon(vertices, uv, triangles, 641, 453, 686, 451, 687, 467, 649, 468);
            Polygon(vertices, uv, triangles, 636, 462, 649, 462, 649, 482, 637, 482);
            Polygon(vertices, uv, triangles, 695, 372, 705, 372, 705, 402, 695, 402);
            Polygon(vertices, uv, triangles,
                479, 577, 526, 553, 630, 537, 749, 532, 833, 546, 875, 565,
                887, 585, 873, 603, 827, 624, 737, 640, 613, 646, 527, 631, 481, 610);

            string path = Folder + "/Pump Silhouette.asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                mesh = new Mesh { name = "Pump Silhouette" };
                AssetDatabase.CreateAsset(mesh, path);
            }
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            var colors = new Color[vertices.Count];
            var normals = new Vector3[vertices.Count];
            for (int i = 0; i < vertices.Count; i++)
            {
                colors[i] = Color.white;
                normals[i] = Vector3.back;
            }
            mesh.colors = colors;
            mesh.normals = normals;
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        private static void Polygon(List<Vector3> vertices, List<Vector2> uv, List<int> triangles, params float[] pixels)
        {
            int start = vertices.Count;
            for (int i = 0; i < pixels.Length; i += 2)
            {
                float x = pixels[i] / FrameWidth, y = 1f - pixels[i + 1] / FrameHeight;
                vertices.Add(new Vector3(x * 16f - 8f, y * 9f - 4.5f, 0));
                uv.Add(new Vector2(x, y));
            }
            for (int i = 1; i < pixels.Length / 2 - 1; i++)
            {
                triangles.Add(start);
                triangles.Add(start + i);
                triangles.Add(start + i + 1);
            }
        }

        private static Material MaterialFor(string name, string file)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Room05City/" + file);
            Shader shader = Shader.Find("Sprites/Default");
            if (texture == null || shader == null)
                throw new InvalidOperationException("Pump depth requires the original city texture and sprite shader.");
            string path = Folder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.mainTexture = texture;
            material.color = Color.white;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void AddPatch(Transform parent, string name, Mesh mesh, Material material, float z)
        {
            Transform existing = parent.Find(name);
            GameObject patch = existing != null ? existing.gameObject : new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            patch.transform.SetParent(null, false);
            patch.transform.position = new Vector3(0, 0, z);
            patch.transform.rotation = Quaternion.identity;
            patch.transform.localScale = Vector3.one;
            // Gate-open art is scaled; preserve the mesh's world-space mapping when attaching it.
            patch.transform.SetParent(parent, true);
            patch.GetComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = patch.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = 100 + Mathf.RoundToInt(-(4.5f - 615f * 9f / FrameHeight) * 20f);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            EditorUtility.SetDirty(patch);
        }
    }
}
