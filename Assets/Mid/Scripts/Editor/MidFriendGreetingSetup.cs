using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using MidProject.Animation;
namespace MidProject.EditorTools
{
 public static class MidFriendGreetingSetup
 {
  [MenuItem("Mid/Animation/2. Set Up Friend Greeting")]
  public static void Setup()
  {
   var scene=SceneManager.GetActiveScene();
   if(EditorApplication.isPlaying||scene.path!="Assets/Mid/Mid_Landscape.unity")throw new System.InvalidOperationException("Mid_Landscape의 편집 상태에서 실행하세요.");
   var cycle=Object.FindFirstObjectByType<MidPushCycle>();
   if(!cycle)throw new System.InvalidOperationException("기존 돌 밀기 주기가 필요합니다.");
   var greeting=cycle.GetComponent<MidFriendGreeting>();if(!greeting)greeting=Undo.AddComponent<MidFriendGreeting>(cycle.gameObject);
   Undo.RecordObject(greeting,"하치와레 인사 연결");
   greeting.pushCycle=cycle;greeting.chiikawa=cycle.chiikawa;greeting.hachiware=GameObject.Find("Hachiware").transform;
   greeting.armLift=45f;greeting.waveAngle=16f;
   EditorUtility.SetDirty(greeting);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  }
 }
}
