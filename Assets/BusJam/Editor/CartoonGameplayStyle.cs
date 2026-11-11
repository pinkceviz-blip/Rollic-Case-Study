using UnityEngine;using UnityEditor;
namespace Rollic.BusJam.Editor {
public static class CartoonGameplayStyle {
[MenuItem("Bus Jam/Art/Apply cartoon gameplay style")]
public static void Apply(){var shader=Resources.Load<Shader>("BusJam/Cartoon");foreach(string folder in new[]{"Assets/BusJam/Art/BusFleet/Materials","Assets/BusJam/Art/StationArt/Materials"})foreach(string guid in AssetDatabase.FindAssets("t:Material",new[]{folder})){var mat=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));if(mat.shader.name!="BusJam/Station"&&mat.name!="Asphalt"&&mat.shader!=shader)continue;mat.shader=shader;mat.SetFloat("_Gloss",folder.Contains("BusFleet")?.32f:.08f);if(mat.name=="Asphalt"){mat.color=new Color(.25f,.31f,.43f);mat.SetFloat("_Gloss",0);}if(mat.name=="PavementStone")mat.color=new Color(.78f,.88f,.94f);if(mat.name=="PavementStoneAlternate")mat.color=new Color(.71f,.83f,.91f);EditorUtility.SetDirty(mat);}AssetDatabase.SaveAssets();}
}}
