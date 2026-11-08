using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using MidProject.Animation;
using MidProject.Environment;
namespace MidProject.EditorTools
{
 public static class MidPushCycleSetup
 {
 [MenuItem("Mid/Animation/1. Set Up Push Cycle")]
 public static void Setup(){var s=SceneManager.GetActiveScene();if(EditorApplication.isPlaying||s.isDirty||s.path!="Assets/Mid/Mid_Landscape.unity")throw new System.Exception("저장된 Mid_Landscape 편집 상태에서 실행하세요.");
 var g=GameObject.Find("Mid_PushCycle");if(!g)g=new GameObject("Mid_PushCycle");var a=g.GetComponent<MidPushCycle>();if(!a)a=g.AddComponent<MidPushCycle>();
 a.rock=GameObject.Find("Mid_Rock").GetComponent<MeshFilter>();a.chiikawa=GameObject.Find("Chiikawa").transform;a.restData=AssetDatabase.LoadAssetAtPath<MidRockRestData>("Assets/Mid/Meshes/Environment/Mid_Rock_RestData.asset");a.period=12;a.effectiveRadius=.55f;
 EditorUtility.SetDirty(a);EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();Debug.Log("5단계 밀기 반복 설정 저장 완료");}
 }
}
