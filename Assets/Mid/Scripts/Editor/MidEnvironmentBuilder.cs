using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MidProject.Environment;
using Object = UnityEngine.Object;

namespace MidProject.EditorTools
{
    /// <summary>4단계 제출용 생성 도구. 같은 경로의 에셋을 갱신하고 최종 교체된 블록아웃만 제거한다.</summary>
    public static class MidEnvironmentBuilder
    {
        const string ScenePath="Assets/Mid/Mid_Landscape.unity";
        const string MeshDir="Assets/Mid/Meshes/Environment";
        const string MatDir="Assets/Mid/Materials/Environment";
        const string PrefabDir="Assets/Mid/Prefabs/Environment";
        const string DataPath=MeshDir+"/Mid_Rock_RestData.asset";
        static void Folder(string path)
        {
            if(AssetDatabase.IsValidFolder(path))return;
            int i=path.LastIndexOf('/');Folder(path.Substring(0,i));AssetDatabase.CreateFolder(path.Substring(0,i),path.Substring(i+1));
        }
        static Mesh SaveMesh(Mesh source)
        {
            string path=MeshDir+"/"+source.name+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!mesh){AssetDatabase.CreateAsset(source,path);return source;}
            mesh.Clear();mesh.indexFormat=source.indexFormat;mesh.vertices=source.vertices;mesh.uv=source.uv;
            mesh.subMeshCount=source.subMeshCount;
            for(int s=0;s<source.subMeshCount;s++)mesh.SetTriangles(source.GetTriangles(s),s);
            mesh.normals=source.normals;mesh.bounds=source.bounds;Object.DestroyImmediate(source);EditorUtility.SetDirty(mesh);return mesh;
        }
        static Material Material(string name,Color color,float smoothness=.18f)
        {
            string path=MatDir+"/"+name+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};AssetDatabase.CreateAsset(mat,path);}
            mat.SetColor("_BaseColor",color);mat.SetFloat("_Metallic",0);mat.SetFloat("_Smoothness",smoothness);
            mat.SetColor("_EmissionColor",Color.black);mat.DisableKeyword("_EMISSION");EditorUtility.SetDirty(mat);return mat;
        }
        static GameObject Part(string name,Transform parent,Mesh mesh,Material material,Vector3 position)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;
            go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;return go;
        }

        static Mesh RampMesh(bool path)
        {
            int nx=144,nz=path?12:80;
            var v=new List<Vector3>();var uv=new List<Vector2>();var tr=new List<int>();
            for(int i=0;i<=nx;i++)for(int j=0;j<=nz;j++)
            {
                float x=Mathf.Lerp(path?-4.55f:-4.65f,path?-1.03f:.40f,(float)i/nx);
                float width=path?.63f+.035f*Mathf.Sin(x*3.2f):1.85f;
                float z=MidRampSurface.CenterZ+Mathf.Lerp(-width,width,(float)j/nz);
                float h=MidRampSurface.Height(x,z);
                v.Add(new Vector3(x,h+(path?.002f:-.003f*(1-Mathf.Clamp01(h/.015f))),z));uv.Add(new Vector2(x,z));
            }
            for(int i=0;i<nx;i++)for(int j=0;j<nz;j++)
            {int a=i*(nz+1)+j,b=a+nz+1;tr.AddRange(new[]{a,a+1,b,a+1,b+1,b});}
            if(!path)
            {
                // 외곽을 지면 아래까지 닫아 떠 있는 판처럼 보이지 않게 한다.
                var edge=new List<int>();
                for(int j=0;j<=nz;j++)edge.Add(j);
                for(int i=1;i<=nx;i++)edge.Add(i*(nz+1)+nz);
                for(int j=nz-1;j>=0;j--)edge.Add(nx*(nz+1)+j);
                for(int i=nx-1;i>0;i--)edge.Add(i*(nz+1));
                int bottom=v.Count;v.Add(new Vector3(-2,-.035f,MidRampSurface.CenterZ));uv.Add(Vector2.zero);
                int first=v.Count;
                foreach(int e in edge){var p=v[e];p.y=-.035f;v.Add(p);uv.Add(Vector2.zero);}
                for(int i=0;i<edge.Count;i++)
                {
                    int j=(i+1)%edge.Count;
                    tr.AddRange(new[]{edge[i],first+i,edge[j],edge[j],first+i,first+j,bottom,first+j,first+i});
                }
            }
            var m=new Mesh{name=path?"Mid_Ramp_EarthTrail":"Mid_Ramp_Landform"};
            m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(tr,0);m.RecalculateNormals();m.RecalculateBounds();return m;
        }

        static GameObject SavePrefab(GameObject root)
        {
            string path=PrefabDir+"/"+root.name+".prefab";
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,path);Object.DestroyImmediate(root);return prefab;
        }

        static void ConformExistingPath()
        {
            var path=GameObject.Find("Path_Placeholder");if(!path)return;
            // 기존 길의 중심·각도·폭은 유지하고 새 둔덕과 겹치는 부분만 높이에 맞춘다.
            var v=new List<Vector3>();var uv=new List<Vector2>();var tri=new List<int>();
            const int rows=100,cols=12;
            var pose=Matrix4x4.TRS(new Vector3(-.8f,0,-2.5f),Quaternion.Euler(0,-12,0),Vector3.one);
            for(int i=0;i<=rows;i++)for(int j=0;j<=cols;j++)
            {
                var p=pose.MultiplyPoint3x4(new Vector3(Mathf.Lerp(-2.1f,2.1f,(float)i/rows),0,Mathf.Lerp(-.425f,.425f,(float)j/cols)));
                p.y=MidRampSurface.Height(p.x,p.z)+.005f;v.Add(p);uv.Add(new Vector2((float)i/rows,(float)j/cols));
            }
            for(int i=0;i<rows;i++)for(int j=0;j<cols;j++)
            {int a=i*(cols+1)+j,b=a+cols+1;tri.AddRange(new[]{a,a+1,b,a+1,b+1,b});}
            var mesh=new Mesh{name="Mid_Path_ConformedSurface"};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tri,0);
            mesh.RecalculateNormals();mesh.RecalculateBounds();mesh=SaveMesh(mesh);
            path.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);path.transform.localScale=Vector3.one;
            path.GetComponent<MeshFilter>().sharedMesh=mesh;
            path.GetComponent<MeshRenderer>().sharedMaterial=Material("Mat_Mid_Path_Earth",new Color(.60f,.48f,.32f),.08f);
            foreach(var old in path.GetComponents<Collider>())Object.DestroyImmediate(old);
            path.AddComponent<MeshCollider>().sharedMesh=mesh;
        }
        static GameObject EnsureInstance(GameObject prefab,Transform parent,string name,Vector3 position)
        {
            var old=parent.Find(name);
            var go=old?old.gameObject:(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);
            go.name=name;go.transform.localPosition=position;return go;
        }
        static Transform Group(Transform parent,string name)
        {
            var existing=parent?parent.Find(name):GameObject.Find(name)?.transform;
            if(existing)return existing;
            var root=new GameObject(name).transform;root.SetParent(parent,false);return root;
        }

        [MenuItem("Mid/Environment/1. Build Static Rock Ramp Flowers")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Play를 멈춘 후 생성하세요.");
            for(int i=0;i<SceneManager.sceneCount;i++)
                if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("현재 수정 중인 씬을 먼저 저장하세요. 변경 사항을 버리지 않습니다.");
            if(SceneManager.GetActiveScene().path!=ScenePath)EditorSceneManager.OpenScene(ScenePath);
            if(GameObject.Find("Mid_GrassLandscape")){MidLandscapeRefinementBuilder.Build();return;} // 개선된 지형 배치를 이전 판 형태로 되돌리지 않는다.
            Folder(MeshDir);Folder(MatDir);Folder(PrefabDir);
            var rockMesh=SaveMesh(MidRockGeometry.Create());
            var rest=AssetDatabase.LoadAssetAtPath<MidRockRestData>(DataPath);
            if(!rest){rest=ScriptableObject.CreateInstance<MidRockRestData>();AssetDatabase.CreateAsset(rest,DataPath);}
            rest.Capture(rockMesh);EditorUtility.SetDirty(rest);
            var stone=Material("Mat_Mid_Rock",new Color(.43f,.46f,.49f),.12f);
            var rockRoot=Part("Mid_Rock",null,rockMesh,stone,Vector3.zero);
            rockRoot.AddComponent<MeshCollider>().sharedMesh=rockMesh;
            var rockPrefab=SavePrefab(rockRoot);

            var rampRoot=new GameObject("Mid_Ramp");rampRoot.AddComponent<MidRampSurface>();
            var ground=AssetDatabase.LoadAssetAtPath<Material>("Assets/Mid/Mat_Mid_Ground.mat");
            var grass=Material("Mat_Mid_Ramp_Grass",ground.GetColor("_BaseColor"),.1f);
            var earth=Material("Mat_Mid_Ramp_Earth",new Color(.51f,.39f,.26f),.08f);
            var land=Part("Landform",rampRoot.transform,SaveMesh(RampMesh(false)),grass,Vector3.zero);
            land.AddComponent<MeshCollider>().sharedMesh=land.GetComponent<MeshFilter>().sharedMesh;
            Part("EarthTrail",rampRoot.transform,SaveMesh(RampMesh(true)),earth,Vector3.zero);
            var rampPrefab=SavePrefab(rampRoot);

            var petal=SaveMesh(MidFlowerGeometry.Petal());var center=SaveMesh(MidFlowerGeometry.Center());
            var stem=SaveMesh(MidFlowerGeometry.Stem());var leaf=SaveMesh(MidFlowerGeometry.Leaf());
            var pink=Material("Mat_Mid_Flower_Petal",new Color(.94f,.57f,.65f),.24f);
            var yellow=Material("Mat_Mid_Flower_Center",new Color(.99f,.74f,.26f),.2f);
            var green=Material("Mat_Mid_Flower_Stem",new Color(.25f,.42f,.18f),.12f);
            var flowerRoot=new GameObject("Mid_Flower");
            Part("Stem",flowerRoot.transform,stem,green,Vector3.zero);
            var blossom=new GameObject("Blossom").transform;blossom.SetParent(flowerRoot.transform,false);
            blossom.localPosition=new Vector3(.018f,.34f,0);blossom.localRotation=Quaternion.Euler(-18,0,0);
            for(int i=0;i<5;i++)
            {
                var p=Part("Petal_"+(i+1),blossom,petal,pink,Vector3.zero);
                p.transform.localRotation=Quaternion.Euler(0,0,72*i);
            }
            Part("Center",blossom,center,yellow,new Vector3(0,0,-.023f));
            var left=Part("Leaf_Left",flowerRoot.transform,leaf,green,new Vector3(.007f,.12f,0));
            left.transform.localRotation=Quaternion.Euler(-12,0,55);
            var right=Part("Leaf_Right",flowerRoot.transform,leaf,green,new Vector3(.013f,.19f,0));
            right.transform.localRotation=Quaternion.Euler(8,20,-62);right.transform.localScale=Vector3.one*.8f;
            var flowerPrefab=SavePrefab(flowerRoot);

            var stage=Group(null,"Environment_Stage04");
            EnsureInstance(rampPrefab,stage,"Mid_Ramp",Vector3.zero);
            var rock=EnsureInstance(rockPrefab,stage,"Mid_Rock",Vector3.zero);
            rock.transform.rotation=Quaternion.identity;rock.transform.localScale=Vector3.one;
            rock.transform.position=MidRampSurface.RockCenter(rest.CopyVertices(),-2.65f,MidRampSurface.CenterZ,rock.transform.rotation);
            var flowerGroup=Group(stage,"Path_Flowers");
            var spots=new[]{new Vector3(-.60f,0,-2.87f),new Vector3(-.35f,0,-2.71f),new Vector3(-.84f,0,-3.00f)};
            for(int i=0;i<spots.Length;i++)
            {
                var p=spots[i];p.y=MidRampSurface.Height(p.x,p.z)-.003f;
                var flower=EnsureInstance(flowerPrefab,flowerGroup,"Flower_"+(i+1),p);
                flower.transform.localScale=Vector3.one*(i==0?1:i==1?.80f:.68f);
                flower.transform.rotation=Quaternion.Euler(0,i==0?-12:i==1?18:-28,i==0?0:i==1?-8:9);
            }
            // 해당 임시 형태만 교체한다. 캐릭터·의자·길·이동 기준점은 유지한다.
            var block=GameObject.Find("Blockout_Stage01");
            if(block)foreach(var t in block.GetComponentsInChildren<Transform>(true))
                if(t && new[]{"Ramp_Placeholder","Stone_Placeholder_Diameter1p2","FlowerArea_Placeholder","FlowerHeight_Placeholder"}.Contains(t.name))
                    Undo.DestroyObjectImmediate(t.gameObject);
            ConformExistingPath();
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Validate();
            Selection.activeGameObject=rock;
            EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
            Debug.Log("4단계 정적 돌·경사로·꽃 저장 완료. 이동이나 캐릭터 변경 없음.");
        }

        static void CheckClosed(Mesh mesh,StringBuilder report,bool outward)
        {
            var edges=new Dictionary<ulong,int>();var v=mesh.vertices;var t=mesh.triangles;
            if(v.Any(p=>float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsNaN(p.z)||float.IsInfinity(p.sqrMagnitude)))
                throw new InvalidOperationException("유효하지 않은 정점: "+mesh.name);
            double volume=0;float minArea=float.PositiveInfinity;int inward=0;
            for(int i=0;i<t.Length;i+=3)
            {
                var a=v[t[i]];var b=v[t[i+1]];var c=v[t[i+2]];var n=Vector3.Cross(b-a,c-a);
                minArea=Mathf.Min(minArea,n.magnitude*.5f);volume+=Vector3.Dot(a,Vector3.Cross(b,c))/6.0;
                if(outward && Vector3.Dot(n,(a+b+c)/3)<0)inward++;
                for(int j=0;j<3;j++)
                {
                    int x=t[i+j],y=t[i+(j+1)%3];ulong key=((ulong)Math.Min(x,y)<<32)|(uint)Math.Max(x,y);
                    edges.TryGetValue(key,out int count);edges[key]=count+1;
                }
            }
            int open=edges.Values.Count(n=>n!=2);
            if(open!=0 || minArea<1e-10f || double.IsNaN(volume) || volume<=0 || inward!=0)
                throw new InvalidOperationException($"메시 검사 실패: {mesh.name}, 열린변 {open}, 최소면적 {minArea}, 체적 {volume}, 역면 {inward}");
            report.AppendLine($"{mesh.name}: 정점 {v.Length}, 삼각형 {t.Length/3}, 열린/비다양체 모서리 {open}, 체적 {volume:F6}"+(outward?$", 역방향면 {inward}":""));
        }

        [MenuItem("Mid/Environment/2. Validate Static Geometry")]
        public static void Validate()
        {
            var report=new StringBuilder("4단계 — 돌·경사로·꽃 정적 모델 검증\n\n");
            foreach(var name in new[]{"Mid_Rock_RingMesh","Mid_Ramp_Landform","Mid_Flower_Petal","Mid_Flower_Center","Mid_Flower_Stem","Mid_Flower_Leaf"})
                CheckClosed(AssetDatabase.LoadAssetAtPath<Mesh>(MeshDir+"/"+name+".asset"),report,name.Contains("Rock"));
            var rock=GameObject.Find("Mid_Rock");var rest=AssetDatabase.LoadAssetAtPath<MidRockRestData>(DataPath).CopyVertices();
            foreach(float x in new[]{MidRampSurface.TravelStart,-2.50f,MidRampSurface.TravelEnd})
            {
                Vector3 p=MidRampSurface.RockCenter(rest,x,MidRampSurface.CenterZ,Quaternion.identity);
                float min=float.PositiveInfinity;
                foreach(var v in rest)min=Mathf.Min(min,p.y+v.y-MidRampSurface.Height(p.x+v.x,p.z+v.z));
                if(Mathf.Abs(min)>.00001f)throw new InvalidOperationException("바닥 접촉 검사 실패");
                report.AppendLine($"돌 X={x:F2}: 중심 Y={p.y:F5}, 경사면 최소 간격={min:F7}");
            }
            var mesh=rock.GetComponent<MeshFilter>().sharedMesh;
            if(!rest.SequenceEqual(mesh.vertices))throw new InvalidOperationException("원본 정점 불일치");
            report.AppendLine("\n경사 18도, 중앙 유효 폭 1.70, 이동 구간 X=-3.20~-1.80, Z=-1.40.");
            report.AppendLine("Height와 RockCenter를 공유해 다음 단계 행렬 변환의 접촉 보정에 사용. 원본 정점·삼각형은 Mid_Rock_RestData에 보존.");
            report.AppendLine("돌 최대 지름 1.20. 꽃 기본 높이 약 0.49, 5장 꽃잎은 닫힌 입체 메시. 꽃 3송이 배치.");
            report.AppendLine("캐릭터 Prefab 및 기존 과제/외부 원본은 변경하지 않음. 이번 단계에는 애니메이션 없음.");
            File.WriteAllText("Assets/Mid/Mid_Stage04_Guide.txt",report.ToString());
            AssetDatabase.ImportAsset("Assets/Mid/Mid_Stage04_Guide.txt");Debug.Log(report.ToString());
        }


    }
}
