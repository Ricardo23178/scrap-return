using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace RobotEscape.Editor
{
    public static class CharacterMotionSetup
    {
        private const string Art = "Assets/Art/CharacterMotion/";
        private static Vector2[] P(params float[] xy)
        {
            var result = new Vector2[xy.Length / 2];
            for (int i = 0; i < result.Length; i++) result[i] = new Vector2(xy[i * 2], xy[i * 2 + 1]);
            return result;
        }

        [MenuItem("Tools/Robot Escape/Apply Character Motion")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode first.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save the open scene first.");
            Sprite[] hero = Import("HeroWalk.png", 4, 2), guard = Import("GuardWalk.png", 4, 2), observe = Import("GuardObserve.png", 4, 1);
            string[] scenes = { "RobotEscapeDemo", "WatchRoom", "ControlRoom", "PrisonEscape" };
            for (int index = 0; index < scenes.Length; index++)
            {
                var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + scenes[index] + ".unity");
                RobotWalker robot = UnityEngine.Object.FindObjectOfType<RobotWalker>();
                if (robot == null) throw new InvalidOperationException("Missing robot in " + scenes[index]);
                robot.walkArea = robot.GetComponent<WalkableArea2D>();
                if (robot.walkArea == null) robot.walkArea = robot.gameObject.AddComponent<WalkableArea2D>();
                robot.walkingPoses = hero; robot.walkCycleDistance = 1.25f;
                if (index < 3) EarlyGround(robot.walkArea, index);
                else PrisonGround(robot, guard, observe);
                SetBounds(robot);
                robot.transform.position = Nearest(robot.walkArea, robot.transform.position);
                foreach (RoomInteractable target in UnityEngine.Object.FindObjectsOfType<RoomInteractable>(true))
                    if (target.standPoint != null && target.kind != ObjectKind.RobotBody &&
                        (target.gameObject.activeInHierarchy || target.kind == ObjectKind.LooseLeg || target.kind == ObjectKind.LooseArm))
                        target.standPoint.position = Nearest(robot.walkArea, target.StandPosition);
                ScrapAssembly assembly = UnityEngine.Object.FindObjectOfType<ScrapAssembly>();
                if (assembly != null && assembly.repairedLegs != null)
                {
                    robot.assemblyLegs = assembly.repairedLegs.transform.Cast<Transform>().ToArray();
                    robot.assemblyTorso = assembly.damagedBody.GetComponent<SpriteRenderer>();
                }
                EditorUtility.SetDirty(robot); EditorUtility.SetDirty(robot.walkArea);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save " + scene.path);
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/Scenes/PrisonEscape.unity");
            Debug.Log("CHARACTER_MOTION_APPLIED");
        }

        private static void EarlyGround(WalkableArea2D area, int index)
        {
            if (index == 0)
                Ground(area, P(-5.5f,-3.25f,6.55f,-3.25f,6.55f,-2.05f,5.5f,-2.05f,4.75f,-2.35f,2.15f,-2.35f,1.65f,-2.5f,-.25f,-2.5f,-2.65f,-2.4f,-3,-2.6f,-5.5f,-2.6f),
                    new Rect(-5.45f,-2.35f,2.65f,1.05f), new Rect(-.05f,-2.25f,1.5f,.6f), new Rect(2.55f,-2.15f,2.05f,.95f));
            else if (index == 1)
                Ground(area, P(-6.6f,-3.45f,6.4f,-3.45f,6.4f,-2,4.5f,-2,3.8f,-2.4f,1.9f,-2.4f,.2f,-2.25f,-1.25f,-2.3f,-2.5f,-2.05f,-4.5f,-2.2f,-6.6f,-2.25f),
                    new Rect(-1.05f,-2,.8f,.6f),new Rect(1.9f,-2.05f,2.3f,1.15f));
            else
                Ground(area, P(-6.3f,-3.55f,6.45f,-3.55f,6.45f,-2,4.45f,-2,3.4f,-2.35f,2.6f,-2.6f,.3f,-2.6f,.15f,-3.05f,-3,-3.05f,-3.25f,-2.4f,-4.3f,-2.4f,-5.5f,-2.55f,-6.3f,-2.55f),
                    new Rect(-6.5f,-2.35f,2.1f,1.7f),new Rect(-3,-2.65f,3.15f,1.05f));
        }

        private static void PrisonGround(RobotWalker robot, Sprite[] guard, Sprite[] observe)
        {
            PrisonChapterController room = UnityEngine.Object.FindObjectOfType<PrisonChapterController>();
            room.walkZones = new WalkableArea2D[4];
            string[] names = { "Cell Foot Ground", "Void Lower Foot Ground", "Void Upper Foot Ground", "Corridor Foot Ground" };
            for (int i = 0; i < names.Length; i++)
            {
                Transform existing = room.transform.Find(names[i]);
                GameObject holder = existing != null ? existing.gameObject : new GameObject(names[i]);
                holder.transform.SetParent(room.transform, false);
                room.walkZones[i] = holder.GetComponent<WalkableArea2D>();
                if (room.walkZones[i] == null) room.walkZones[i] = holder.AddComponent<WalkableArea2D>();
            }
            Ground(room.walkZones[0], P(-6.15f,-3.3f,5.6f,-3.3f,5.6f,-2.6f,-.8f,-2.6f,-.8f,-2.7f,-4.8f,-2.7f,-4.95f,-2.6f,-6.15f,-2.6f),
                new Rect(-5.3f,-2.6f,4.4f,.9f), new Rect(-6.3f,-3.55f,1.1f,.55f), new Rect(.1f,-2.45f,1.8f,.85f));
            Ground(room.walkZones[1], P(-6.2f,-3.7f,6,-3.7f,6,-3.25f,1,-3.25f,.6f,-3.1f,-.6f,-3.1f,-1,-3.25f,-6.2f,-3.25f),
                new Rect(2.1f,-3.02f,4.2f,1.42f));
            Ground(room.walkZones[2], P(-1.2f,1.85f,5.8f,1.85f,5.8f,2.25f,-1.2f,2.25f),
                new Rect(-4,1.9f,2.5f,1));
            Ground(room.walkZones[3], P(-6.1f,-3.5f,-3.3f,-3.45f,.5f,-3.2f,3,-2.85f,6.35f,-2.25f,6.35f,-1.4f,4.6f,-1.5f,3,-2.25f,2.2f,-2.55f,-3.2f,-2.55f,-3.7f,-2.65f,-6.1f,-2.65f),
                new Rect(-3.5f,-2.5f,5.85f,1.8f));
            robot.walkArea.boundary = room.walkZones[0].boundary;
            robot.walkArea.obstacles = room.walkZones[0].obstacles; robot.walkArea.radius = .08f;
            foreach (PrisonTarget target in room.targets)
            {
                int zone = target.area == 0 ? 0 : target.area == 1 ? target.upperLanding ? 2 : 1 : 3;
                Vector2 stand = target.StandPosition;
                if (target.action == PrisonAction.Ladder) stand = new Vector2(0,-3.35f);
                if (target.action == PrisonAction.Magnet || target.action == PrisonAction.Gear) stand = new Vector2(4,-3.45f);
                if (target.area == 1 && target.upperLanding && target.action == PrisonAction.Scenery) stand = new Vector2(-.9f,2);
                if (target.area == 2 && target.action == PrisonAction.Exit) stand = new Vector2(5.7f,-2.1f);
                if (target.standPoint != null) target.standPoint.position = Nearest(room.walkZones[zone],stand);
            }
            room.patrol.guardWalkPoses = guard; room.patrol.observePoses = observe;
            EditorUtility.SetDirty(room); EditorUtility.SetDirty(room.patrol);
        }

        private static void Ground(WalkableArea2D area, Vector2[] boundary, params Rect[] obstacles)
        { area.boundary = boundary; area.obstacles = obstacles; area.radius = .08f; EditorUtility.SetDirty(area); }

        private static void SetBounds(RobotWalker robot)
        {
            robot.minWalk = new Vector2(robot.walkArea.boundary.Min(v=>v.x),robot.walkArea.boundary.Min(v=>v.y));
            robot.maxWalk = new Vector2(robot.walkArea.boundary.Max(v=>v.x),robot.walkArea.boundary.Max(v=>v.y));
        }

        private static Vector2 Nearest(WalkableArea2D area, Vector2 point)
        {
            if (area.Contains(point)) return point;
            Vector2 best = point; float bestDistance = float.PositiveInfinity;
            float minX = area.boundary.Min(p=>p.x), maxX = area.boundary.Max(p=>p.x);
            float minY = area.boundary.Min(p=>p.y), maxY = area.boundary.Max(p=>p.y);
            for (float y = minY; y <= maxY; y += .035f) for (float x = minX; x <= maxX; x += .035f)
            {
                Vector2 candidate = new Vector2(x,y); float distance = (candidate-point).sqrMagnitude;
                if (distance < bestDistance && area.Contains(candidate)) { best=candidate; bestDistance=distance; }
            }
            if (float.IsInfinity(bestDistance)) throw new InvalidOperationException("No legal foot position in " + area.name);
            return best;
        }

        private static Sprite[] Import(string file, int columns, int rows)
        {
            string path = Art + file;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 256; importer.isReadable = true; importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false; importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var factories = new SpriteDataProviderFactories(); factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
            var previous = provider.GetSpriteRects().ToDictionary(s=>s.name,s=>s.spriteID);
            Color32[] pixels = texture.GetPixels32(); var rects = new List<SpriteRect>();
            for (int i = 0; i < columns * rows; i++)
            {
                int x0=i%columns*texture.width/columns,x1=(i%columns+1)*texture.width/columns-1;
                int y0=(rows-1-i/columns)*texture.height/rows,y1=(rows-i/columns)*texture.height/rows-1;
                int left=x1,right=x0,bottom=y1,top=y0;
                for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++) if(pixels[y*texture.width+x].a>150)
                {left=Mathf.Min(left,x);right=Mathf.Max(right,x);bottom=Mathf.Min(bottom,y);top=Mathf.Max(top,y);}
                if(right<=left||top<=bottom)throw new InvalidOperationException("Empty motion frame "+file+" "+i);
                string name=Path.GetFileNameWithoutExtension(file)+i;
                rects.Add(new SpriteRect{name=name,rect=new Rect(left,bottom,right-left+1,top-bottom+1),pivot=new Vector2(.5f,0),alignment=SpriteAlignment.Custom,spriteID=previous.ContainsKey(name)?previous[name]:GUID.Generate()});
            }
            provider.SetSpriteRects(rects.ToArray());
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(s=>new SpriteNameFileIdPair(s.name,s.spriteID)));
            provider.Apply(); importer.isReadable = false; importer.SaveAndReimport();
            var sprites=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(s=>s.name);
            return rects.Select(s=>sprites[s.name]).ToArray();
        }
    }
}
