using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using MidProject.Animation;
using MidProject.Environment;
namespace MidProject.EditorTools
{
    public static class MidRockSizeSetup
    {
        [MenuItem("Mid/Environment/Use 75 Percent Rock")]
        public static void Apply()
        {
            var scene=SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying||scene.path!="Assets/Mid/Mid_Landscape.unity")return;
            const string folder="Assets/Mid/Meshes/Environment/";
            var source=AssetDatabase.LoadAssetAtPath<Mesh>(folder+"Mid_Rock_RingMesh.asset");
            var cycle=Object.FindFirstObjectByType<MidPushCycle>();
            if(!source||!cycle)throw new System.InvalidOperationException("돌 원본과 밀기 주기가 필요합니다.");
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(folder+"Mid_Rock_75Percent.asset");
            if(!mesh){mesh=Object.Instantiate(source);AssetDatabase.CreateAsset(mesh,folder+"Mid_Rock_75Percent.asset");}
            var vertices=source.vertices;
            for(int i=0;i<vertices.Length;i++)vertices[i]*=.75f;
            mesh.vertices=vertices;mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
            var rest=AssetDatabase.LoadAssetAtPath<MidRockRestData>(folder+"Mid_Rock_75Percent_RestData.asset");
            if(!rest){rest=ScriptableObject.CreateInstance<MidRockRestData>();AssetDatabase.CreateAsset(rest,folder+"Mid_Rock_75Percent_RestData.asset");}
            rest.Capture(mesh);EditorUtility.SetDirty(rest);
            Undo.RecordObject(cycle,"돌 크기 75퍼센트");cycle.restData=rest;cycle.effectiveRadius=.55f*.75f;EditorUtility.SetDirty(cycle);
            Undo.RecordObject(cycle.rock,"돌 메시 크기");cycle.rock.sharedMesh=mesh;
            PrefabUtility.RecordPrefabInstancePropertyModifications(cycle.rock);
            var collider=cycle.rock.GetComponent<MeshCollider>();
            if(collider){Undo.RecordObject(collider,"돌 충돌 메시");collider.sharedMesh=mesh;PrefabUtility.RecordPrefabInstancePropertyModifications(collider);}
            var tr=cycle.rock.transform;Undo.RecordObject(tr,"돌 바닥 접촉");
            tr.position=MidRampSurface.RockCenter(vertices,tr.position.x,tr.position.z,tr.rotation);
            PrefabUtility.RecordPrefabInstancePropertyModifications(tr);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
    }
}
