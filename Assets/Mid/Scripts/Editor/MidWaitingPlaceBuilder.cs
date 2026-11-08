using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace MidProject.EditorTools {
/// <summary>기다리는 자리, 두 몫의 식사, 밝은 노을 구도를 재생성할 수 있는 Mid 전용 제작 도구.</summary>
public static class MidWaitingPlaceBuilder {
 const string Root="Assets/Mid", Dir=Root+"/Materials/WaitingPlace", MeshDir=Root+"/Meshes/WaitingPlace";
 static GameObject Find(string n)=>SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t=>t.name==n)?.gameObject;
 static void Folder(string p){if(AssetDatabase.IsValidFolder(p))return;int i=p.LastIndexOf('/');Folder(p.Substring(0,i));AssetDatabase.CreateFolder(p.Substring(0,i),p.Substring(i+1));}
 static Material Mat(string n,Color c,float gloss=.18f){string p=Dir+"/"+n+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,p);}m.SetColor("_BaseColor",c);m.SetFloat("_Smoothness",gloss);m.SetFloat("_Metallic",0);m.SetColor("_EmissionColor",Color.black);m.DisableKeyword("_EMISSION");EditorUtility.SetDirty(m);return m;}
 static Mesh SaveMesh(Mesh m){var p=MeshDir+"/"+m.name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(p);if(old){EditorUtility.CopySerialized(m,old);Object.DestroyImmediate(m);EditorUtility.SetDirty(old);return old;}AssetDatabase.CreateAsset(m,p);return m;}
 // 단면을 회전시켜 윗면, 둥근 테두리, 아랫면이 이어지는 얕은 도자기 접시를 만든다.
 static Mesh Plate(){var profile=new[]{new Vector2(0,.010f),new Vector2(.12f,.010f),new Vector2(.17f,.016f),new Vector2(.202f,.031f),new Vector2(.216f,.034f),new Vector2(.222f,.028f),new Vector2(.216f,.018f),new Vector2(.178f,.006f),new Vector2(.115f,0),new Vector2(0,0)};const int n=96;var v=new List<Vector3>();var t=new List<int>();for(int j=0;j<profile.Length;j++)for(int i=0;i<=n;i++){float a=i*2*Mathf.PI/n;v.Add(new Vector3(profile[j].x*Mathf.Cos(a),profile[j].y,profile[j].x*Mathf.Sin(a)));}for(int j=0;j<profile.Length-1;j++)for(int i=0;i<n;i++){int a=j*(n+1)+i,b=a+n+1;t.AddRange(new[]{a,a+1,b,a+1,b+1,b});}var m=new Mesh{name="Mid_SharedSnackPlate"};m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();return SaveMesh(m);}
 static GameObject Part(Transform p,string n,Mesh mesh,Material mat,Vector3 pos,Vector3 scale){var tr=p.Find(n);var g=tr?tr.gameObject:new GameObject(n);g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localRotation=Quaternion.identity;g.transform.localScale=scale;var f=g.GetComponent<MeshFilter>();if(!f)f=g.AddComponent<MeshFilter>();f.sharedMesh=mesh;var r=g.GetComponent<MeshRenderer>();if(!r)r=g.AddComponent<MeshRenderer>();r.sharedMaterial=mat;return g;}
 static Transform Group(Transform p,string n){var t=p?p.Find(n):Find(n)?.transform;if(!t){t=new GameObject(n).transform;t.SetParent(p,false);}return t;}
 static Mesh Sphere(){var g=GameObject.CreatePrimitive(PrimitiveType.Sphere);var m=g.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(g);return m;}
 static void Chair(GameObject g,string name,Vector3 pos,float yaw,Material wood,Material seat){g.name=name;g.transform.SetPositionAndRotation(pos,Quaternion.Euler(0,yaw,0));g.transform.localScale=Vector3.one;foreach(var r in g.GetComponentsInChildren<Renderer>())r.sharedMaterial=r.name=="Seat"?seat:wood;var s=g.transform.Find("Seat");s.localScale=new Vector3(.60f,.075f,.52f);var back=g.transform.Find("Back");back.localPosition=new Vector3(0,.50f,.23f);back.localScale=new Vector3(.60f,.32f,.055f);foreach(var t in g.GetComponentsInChildren<Transform>())if(t.name.StartsWith("Leg_")){var p=t.localPosition;p.x=Mathf.Sign(p.x)*.23f;p.z=Mathf.Sign(p.z)*.19f;t.localPosition=p;}}
 // 앉은 자세에서 발 윗부분이 몸통 아랫면과 겹치도록 올리고 안쪽으로 넣는다.
 public static void FitSeatedFeet(Transform h)
 {
     foreach(string n in new[]{"LeftLeg","RightLeg"})
     {
         var leg=h.Find(n);Undo.RecordObject(leg,"하치와레 앉은 발 연결 보정");
         var p=leg.localPosition;p.y=.125f;p.z=-.155f;leg.localPosition=p;
         PrefabUtility.RecordPrefabInstancePropertyModifications(leg);
     }
 }
 [MenuItem("Mid/Waiting Place/1. Build Waiting Scene")]
 public static void Build(){var scene=SceneManager.GetActiveScene();if(EditorApplication.isPlaying||scene.isDirty||scene.path!=Root+"/Mid_Landscape.unity")throw new Exception("저장된 Mid_Landscape 편집 상태에서 실행하세요.");Folder(Dir);Folder(MeshDir);Folder(Root+"/Prefabs/WaitingPlace");
 var overview=Find("Mid_OverviewCamera").GetComponent<Camera>();
 var waiting=Find("WaitingPlace").transform;var table=Find("Mid_S02_Table");
 table.transform.position=new Vector3(5.52f,.558f,.35f); // S02 복제본의 부품과 크기는 유지한다.
 var wood=Mat("Mat_Mid_Seat_Wood",new Color(.48f,.30f,.16f));var cushion=Mat("Mat_Mid_Seat_Cream",new Color(.80f,.72f,.52f),.12f);
 Chair(Find("Seat_Hachiware")??Find("Seat_Hachiware_Placeholder"),"Seat_Hachiware",new Vector3(4.55f,.558f,.95f),35,wood,cushion);
 Chair(Find("Seat_Chiikawa_Empty")??Find("Seat_Chiikawa_Empty_Placeholder"),"Seat_Chiikawa_Empty",new Vector3(6.61f,.558f,.36f),90,wood,cushion);
 var h=Find("Hachiware").transform;h.SetPositionAndRotation(new Vector3(4.4926f,.80f,.8681f),Quaternion.Euler(0,35,0));h.Find("Head").localRotation=Quaternion.Euler(0,30,0);FitSeatedFeet(h);PrefabUtility.RecordPrefabInstancePropertyModifications(h);PrefabUtility.RecordPrefabInstancePropertyModifications(h.Find("Head"));
 var meals=Group(waiting,"TwoMeals");meals.position=Vector3.zero;meals.rotation=Quaternion.identity;meals.localScale=Vector3.one;
 var plate=Plate();var ceramic=Mat("Mat_Mid_Plate_Ivory",new Color(.95f,.91f,.80f),.35f);var bread=Mat("Mat_Mid_Bread_Honey",new Color(.77f,.47f,.20f));var score=Mat("Mat_Mid_Bread_Score",new Color(.95f,.76f,.44f));var sphere=Sphere();
 for(int i=0;i<2;i++){var meal=Group(meals,i==0?"Meal_Hachiware_InUse":"Meal_Chiikawa_Reserved");meal.position=new Vector3(i==0?5.04f:6.00f,1.119f,.35f);meal.rotation=Quaternion.identity;Part(meal,"Plate",plate,ceramic,Vector3.zero,Vector3.one);for(int j=0;j<2;j++){var bun=Part(meal,"Bun_"+(j+1),sphere,bread,new Vector3(j==0?-.085f:.075f,.062f,j==0?-.025f:.045f),new Vector3(.15f,.10f,.16f));for(int k=0;k<2;k++)Part(meal,"Bun_"+(j+1)+"_Score_"+k,sphere,score,new Vector3(bun.transform.localPosition.x+(k==0?-.026f:.026f),.108f,bun.transform.localPosition.z),new Vector3(.012f,.009f,.080f));}}
 // 소품은 재실행 시 동일 이름과 경로를 갱신한다. 외부 정자 원본에는 적용하지 않는다.
 PrefabUtility.SaveAsPrefabAsset(meals.gameObject,Root+"/Prefabs/WaitingPlace/Mid_TwoMeals.prefab");
 var stone=Mat("Mat_Mid_Stone_WarmGrey",new Color(.65f,.62f,.56f),.10f);Find("Mid_Rock").GetComponent<Renderer>().sharedMaterial=stone;PrefabUtility.RecordPrefabInstancePropertyModifications(Find("Mid_Rock").GetComponent<Renderer>());
 var original=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Environment/Mat_Mid_ContinuousGrassland.mat");string groundPath=Dir+"/Mat_Mid_EveningGrass.mat";var ground=AssetDatabase.LoadAssetAtPath<Material>(groundPath);if(!ground){ground=new Material(original);AssetDatabase.CreateAsset(ground,groundPath);}var tex=new Texture2D(2,2,TextureFormat.RGB24,false);tex.LoadImage(File.ReadAllBytes(Root+"/Textures/Mid_LandscapeSurface.png"));var colors=tex.GetPixels();for(int i=0;i<colors.Length;i++)colors[i]+=new Color(.055f,.075f,.065f,0);tex.SetPixels(colors);tex.Apply();string tp=Root+"/Textures/Mid_EveningGrassland.png";File.WriteAllBytes(tp,tex.EncodeToPNG());Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(tp,ImportAssetOptions.ForceSynchronousImport);var imp=(TextureImporter)AssetImporter.GetAtPath(tp);imp.wrapMode=TextureWrapMode.Clamp;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.SaveAndReimport();ground.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(tp));EditorUtility.SetDirty(ground);Find("Mid_GrassLandscape").GetComponent<Renderer>().sharedMaterial=ground;PrefabUtility.RecordPrefabInstancePropertyModifications(Find("Mid_GrassLandscape").GetComponent<Renderer>());
 var sun=Find("Directional Light").GetComponent<Light>();sun.transform.rotation=Quaternion.Euler(36,325,0);sun.color=new Color(1,.93f,.82f);sun.intensity=1.15f;sun.shadows=LightShadows.Soft;sun.shadowStrength=.72f;sun.shadowBias=.025f;sun.shadowNormalBias=.12f;RenderSettings.sun=sun;
 RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.72f,.79f,.84f);RenderSettings.ambientEquatorColor=new Color(.66f,.64f,.57f);RenderSettings.ambientGroundColor=new Color(.40f,.44f,.34f);RenderSettings.fog=false;
 var lamp=Find("Pavilion Point Light").GetComponent<Light>();lamp.transform.position=new Vector3(5.5f,2.15f,.6f);lamp.color=new Color(1,.95f,.85f);lamp.intensity=.65f;lamp.range=5;lamp.shadows=LightShadows.None;Find("Pavilion Spot Light").SetActive(false);
 var skyPath=Dir+"/Mat_Mid_BrightEveningSky.mat";var sky=AssetDatabase.LoadAssetAtPath<Material>(skyPath);if(!sky){sky=new Material(Shader.Find("Mid/BrightEveningSky"));AssetDatabase.CreateAsset(sky,skyPath);}RenderSettings.skybox=sky;
 overview.clearFlags=CameraClearFlags.Skybox;overview.fieldOfView=34.5f;overview.transform.position=new Vector3(1.15f,5.35f,-12.8f);overview.transform.LookAt(new Vector3(1.15f,.75f,0));
 var cg=Find("Mid_WaitingCamera");if(!cg)cg=new GameObject("Mid_WaitingCamera");var close=cg.GetComponent<Camera>();if(!close)close=cg.AddComponent<Camera>();close.CopyFrom(overview);close.transform.position=new Vector3(2.90f,2.85f,-3.25f);close.transform.LookAt(new Vector3(5.30f,1.1f,.55f));close.fieldOfView=39;close.enabled=false;
 ShowOverall();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("6단계 기다리는 자리·식사·노을·두 구도 저장 완료");}
 [MenuItem("Mid/Views/1. Overall")]
 public static void ShowOverall()=>Switch("Mid_OverviewCamera");
 [MenuItem("Mid/Views/2. Waiting Place")]
 public static void ShowWaiting()=>Switch("Mid_WaitingCamera");
 static void Switch(string name){foreach(string n in new[]{"Mid_OverviewCamera","Mid_WaitingCamera"}){var g=Find(n);if(!g)continue;g.GetComponent<Camera>().enabled=n==name;g.tag=n==name?"MainCamera":"Untagged";var a=g.GetComponent<AudioListener>();if(a)a.enabled=n==name;}var selected=Find(name);if(selected&&!selected.GetComponent<AudioListener>())selected.AddComponent<AudioListener>();if(!EditorApplication.isPlaying)EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());}

}
}



