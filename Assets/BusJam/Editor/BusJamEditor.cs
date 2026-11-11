using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Rollic.BusJam.Editor
{
    [CustomEditor(typeof(LevelEditorBoard))] public class LevelEditorBoardInspector:UnityEditor.Editor
    {
        int desiredLives=-1;
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Oyuncu hakları (test)",EditorStyles.boldLabel);
            var lives=LivesService.GetState();
            if(desiredLives<0)desiredLives=lives.count;
            EditorGUILayout.LabelField("Mevcut kalan hak",lives.count+" / "+LivesService.Maximum);
            EditorGUILayout.LabelField("Yeni hak",lives.count==LivesService.Maximum?"Dolu":LivesService.Remaining(lives,LivesService.UtcNow));
            desiredLives=EditorGUILayout.IntSlider("Kalan hak",desiredLives,0,LivesService.Maximum);
            if(GUILayout.Button("Hakları uygula"))LivesService.SetForTesting(desiredLives);
            EditorGUILayout.HelpBox("Bu alan bu bilgisayardaki oyuncu kaydını değiştirir. Bölüm verisine kaydedilmez. Değer oyunu kapatıp açınca korunur.",MessageType.Info);
            EditorGUILayout.Space();
            var b=(LevelEditorBoard)target;
            EditorGUILayout.HelpBox("Click cells in Scene view to paint. Right-click removes a cell. Row 0 is the exit. Bus colors: 0 Coral, 1 Blue, 2 Green, 3 Gold, 4 Violet, 5 Pink.",MessageType.Info);
            if(b.level)
            {
                var so=new SerializedObject(b.level);
                so.Update();
                foreach(string field in new[]
                {
                    "title","width","height","seconds","waitingSlots","busCapacity","buses","entrances"
                }
                )EditorGUILayout.PropertyField(so.FindProperty(field),true);
                so.ApplyModifiedProperties();
                string error=b.level.ValidateLevel();
                EditorGUILayout.HelpBox(error??"Counts and grid are valid. Check solvability before submission.",error==null?MessageType.Info:MessageType.Error);
                if(GUILayout.Button("Save level"))
                {
                    EditorUtility.SetDirty(b.level);
                    AssetDatabase.SaveAssets();
                }
                if(GUILayout.Button("Test this level"))
                {
                    if(error!=null)
                    {
                        EditorUtility.DisplayDialog("Invalid level",error,"OK");
                        return;
                    }
                    var g=new GameObject("Bus Jam Preview").AddComponent<BusJamGame>();
                    g.levels=new[]
                    {
                        b.level
                    }
                    ;
                    PlayerPrefs.DeleteKey("Rollic.BusJam.Save.v1");
                    EditorApplication.isPlaying=true;
                }
            }
            if(GUILayout.Button("Create new level"))
            {
                string path=EditorUtility.SaveFilePanelInProject("New level","Level11","asset","Save inside Assets/Resources/BusJamLevels to include in game.","Assets/Resources/BusJamLevels");
                if(!string.IsNullOrEmpty(path))
                {
                    var l=LevelData.CreateDefault(0);
                    AssetDatabase.CreateAsset(l,path);
                    Undo.RecordObject(b,"Select level");
                    b.level=l;
                    EditorUtility.SetDirty(b);
                }
            }
        }
        void OnSceneGUI()
        {
            var b=(LevelEditorBoard)target;
            if(!b.level)return;
            var l=b.level;
            Handles.BeginGUI();
            GUI.Label(new Rect(15,15,500,30),"BUS JAM LEVEL EDITOR — click to paint • right-click to erase");
            Handles.EndGUI();
            for(int y=0;y<l.height;y++)for(int x=0;x<l.width;x++)
            {
                var pos=new Vector3(x,0,-y);
                var p=l.passengers.Find(v=>v.x==x&&v.y==y);
                Handles.color=l.obstacles.Contains(new Vector2Int(x,y))?Color.gray:p!=null?BusJamGame.Palette[p.color]:new Color(.8f,.85f,1);
                Handles.DrawSolidRectangleWithOutline(new[]
                {
                    pos+new Vector3(-.45f,0,-.45f),pos+new Vector3(.45f,0,-.45f),pos+new Vector3(.45f,0,.45f),pos+new Vector3(-.45f,0,.45f)
                }
                ,Handles.color,Color.black);
                Handles.Label(pos+Vector3.up*.1f,x+","+y);
            }
            var e=Event.current;
            if(e.type==EventType.Layout)HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            if(e.type==EventType.MouseDown&&(e.button==0||e.button==1))
            {
                var ray=HandleUtility.GUIPointToWorldRay(e.mousePosition);
                if(new Plane(Vector3.up,Vector3.zero).Raycast(ray,out float distance))
                {
                    var pos=ray.GetPoint(distance);
                    int x=Mathf.RoundToInt(pos.x),y=Mathf.RoundToInt(-pos.z);
                    if(x>=0&&x<l.width&&y>=0&&y<l.height)
                    {
                        Undo.RecordObject(l,"Paint level");
                        l.passengers.RemoveAll(p=>p.x==x&&p.y==y);
                        l.obstacles.Remove(new Vector2Int(x,y));
                        if(e.button==0)
                        {
                            if(b.paintObstacle)l.obstacles.Add(new Vector2Int(x,y));
                            else l.passengers.Add(new PassengerData(x,y,Mathf.Clamp(b.paintColor,0,5)));
                        }
                        EditorUtility.SetDirty(l);
                        e.Use();
                        Repaint();
                    }
                }
            }
        }
    }
    public static class BusJamSetup
    {
        [MenuItem("Bus Jam/Install or open game")]  public static void Install()
        {
            Directory.CreateDirectory("Assets/Resources/BusJamLevels");
            Directory.CreateDirectory("Assets/Scenes");
            for(int i=0;i<LevelData.DefaultLevelCount;i++)
            {
                string path="Assets/Resources/BusJamLevels/Level"+(i+1).ToString("00")+".asset";
                if(!AssetDatabase.LoadAssetAtPath<LevelData>(path))AssetDatabase.CreateAsset(LevelData.CreateDefault(i),path);
            }
            AssetDatabase.SaveAssets();
            var game=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            new GameObject("Bus Jam").AddComponent<BusJamGame>();
            EditorSceneManager.SaveScene(game,"Assets/Scenes/BusJam.unity");
            var editor=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var board=new GameObject("Level editor — select me").AddComponent<LevelEditorBoard>();
            board.level=AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Resources/BusJamLevels/Level01.asset");
            EditorSceneManager.SaveScene(editor,"Assets/Scenes/LevelEditor.unity");
            EditorBuildSettings.scenes=new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/BusJam.unity",true),new EditorBuildSettingsScene("Assets/Scenes/LevelEditor.unity",false)
            }
            ;
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;
            PlayerSettings.companyName="Case Study";
            PlayerSettings.productName="Bus Jam";
            EditorSceneManager.OpenScene("Assets/Scenes/BusJam.unity");
            Validate();
        }
        [MenuItem("Bus Jam/Open level editor")]  public static void OpenEditor()
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            EditorSceneManager.OpenScene("Assets/Scenes/LevelEditor.unity");
            Selection.activeGameObject=UnityEngine.Object.FindAnyObjectByType<LevelEditorBoard>().gameObject;
            SceneView.lastActiveSceneView?.LookAt(new Vector3(2,0,-2),Quaternion.Euler(90,0,0),7);
        }
        [MenuItem("Bus Jam/Validate levels and rules")]  public static void Validate()
        {
            var levels=Resources.LoadAll<LevelData>("BusJamLevels");
            if(levels.Length<LevelData.DefaultLevelCount)throw new Exception("Expected at least ten levels.");
            foreach(var level in levels)
            {
                string error=level.ValidateLevel();
                if(error!=null)throw new Exception(level.name+": "+error);
                var solver=new LevelSolver();
                if(!solver.Solve(level,level.waitingSlots-1,out var solution))throw new Exception(level.name+(solver.LimitReached?": Solver search limit reached.":": No solution within waiting capacity."));
            }
            var test=ScriptableObject.CreateInstance<LevelData>();
            test.width=2;
            test.height=2;
            var back=new PassengerData(0,1,0);
            var blockers=new List<PassengerData>
            {
                back,new PassengerData(0,0,1),new PassengerData(1,1,1)
            }
            ;
            if(PuzzleRules.Path(test,blockers,back)!=null)throw new Exception("Blocked passenger escaped.");
            blockers.RemoveAt(2);
            if(PuzzleRules.Path(test,blockers,back)==null)throw new Exception("Side route not found.");
            UnityEngine.Object.DestroyImmediate(test);
            Debug.Log("BUS JAM VALIDATION PASSED: ten solvable levels including waiting-buffer strategy, color counts, blocked paths and side routes.");
        }
        [MenuItem("Bus Jam/Reset saved progress")] public static void Reset()
        {
            PlayerPrefs.DeleteKey("Rollic.BusJam.Save.v1");
            PlayerPrefs.Save();
        }
    }
}
