using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using MidProject.Environment;
using Object=UnityEngine.Object;
namespace MidProject.EditorTools
{
    /// <summary>잔디 언덕과 산책길을 한 표면으로 만들고 정적 구도를 배치하는 제출용 도구.</summary>
    public static class MidLandscapeRefinementBuilder
    {
        const string Root="Assets/Mid";
        const string MeshPath=Root+"/Meshes/Environment/Mid_ContinuousGrassland.asset";
        const string TexturePath=Root+"/Textures/Mid_LandscapeSurface.png";
        const string MatPath=Root+"/Materials/Environment/Mat_Mid_ContinuousGrassland.mat";
        const string PrefabPath=Root+"/Prefabs/Environment/Mid_GrassLandscape.prefab";
        const float X0=-12,X1=16,Z0=-8,Z1=14;
        static readonly Vector3 WaitingShift=new Vector3(3.4f,0,.5f);
        static Vector2 Bezier(Vector2 a,Vector2 b,Vector2 c,Vector2 d,float t)
        {float u=1-t;return u*u*u*a+3*u*u*t*b+3*u*t*t*c+t*t*t*d;}
        static Vector2[] WalkPath()
        {
            var p=new List<Vector2>();
            for(int i=0;i<=32;i++)p.Add(Bezier(new Vector2(-4.0f,-1.4f),new Vector2(-4.45f,-3.7f),new Vector2(-1.5f,-4.65f),new Vector2(.8f,-4.1f),i/32f));
            for(int i=1;i<=32;i++)p.Add(Bezier(new Vector2(.8f,-4.1f),new Vector2(3.1f,-3.9f),new Vector2(4.60f,-3.60f),new Vector2(4.60f,-2.20f),i/32f));
            return p.ToArray();
        }
        static float Distance(Vector2 p,Vector2 a,Vector2 b)
        {var d=b-a;return Vector2.Distance(p,a+Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude)*d);}
        static Texture2D SurfaceTexture()
        {
            const int size=1024;var pixels=new Color32[size*size];var path=WalkPath();
            var grass=new Color(.43f,.53f,.32f);var dirt=new Color(.65f,.57f,.42f);
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float wx=Mathf.Lerp(X0,X1,x/(float)(size-1)),wz=Mathf.Lerp(Z0,Z1,y/(float)(size-1));var p=new Vector2(wx,wz);
                float dist=100;
                for(int k=0;k<path.Length-1;k++)dist=Mathf.Min(dist,Distance(p,path[k],path[k+1]));
                float noise=(Mathf.PerlinNoise(wx*4.3f+32,wz*4.3f+41)-.5f)*.065f;
                float walk=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.18f,.42f,dist+noise));
                float routeZ=MidRampSurface.CenterZ+.045f*Mathf.Sin(wx*2);
                float route=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.21f,.49f,Mathf.Abs(wz-routeZ)+noise)))
                    *Mathf.SmoothStep(0,1,Mathf.InverseLerp(-4.6f,-4.1f,wx))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(-1.75f,-1.15f,wx)));
                float weight=Mathf.Max(walk*.86f,route*.70f);
                float edge=Mathf.SmoothStep(0,1,Mathf.Min(Mathf.Min(wx-X0,X1-wx),Mathf.Min(wz-Z0,Z1-wz))/1.5f);
                float variation=(Mathf.PerlinNoise(wx*1.3f+19,wz*1.3f+31)-.5f)*.035f*edge;
                pixels[y*size+x]=Color.Lerp(grass+new Color(variation,variation,variation,0),dirt,weight);
            }
            var tex=new Texture2D(size,size,TextureFormat.RGB24,false);tex.SetPixels32(pixels);tex.Apply();File.WriteAllBytes(TexturePath,tex.EncodeToPNG());Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(TexturePath,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(TexturePath);importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=1024;importer.mipmapEnabled=true;importer.anisoLevel=4;importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        }
        static Mesh LandMesh()
        {
            const int nx=320,nz=256;var v=new List<Vector3>();var uv=new List<Vector2>();var tr=new List<int>();
            for(int i=0;i<=nx;i++)for(int j=0;j<=nz;j++)
            {float x=Mathf.Lerp(X0,X1,i/(float)nx),z=Mathf.Lerp(Z0,Z1,j/(float)nz);v.Add(new Vector3(x,MidRampSurface.Height(x,z),z));uv.Add(new Vector2(i/(float)nx,j/(float)nz));}
            for(int i=0;i<nx;i++)for(int j=0;j<nz;j++){int a=i*(nz+1)+j,b=a+nz+1;tr.AddRange(new[]{a,a+1,b,a+1,b+1,b});}
            // 시야 밖까지 평지를 연장한다. 같은 경계 정점에 연결해 겹치는 면을 만들지 않는다.
            var border=new List<int>();for(int j=0;j<=nz;j++)border.Add(j);for(int i=1;i<=nx;i++)border.Add(i*(nz+1)+nz);for(int j=nz-1;j>=0;j--)border.Add(nx*(nz+1)+j);for(int i=nx-1;i>0;i--)border.Add(i*(nz+1));
            int outer=v.Count;
            foreach(int b in border){var p=v[b];v.Add(new Vector3(p.x*4,0,p.z*4));uv.Add(uv[b]);}
            for(int i=0;i<border.Count;i++){int j=(i+1)%border.Count;tr.AddRange(new[]{border[i],outer+i,border[j],border[j],outer+i,outer+j});}
            var mesh=new Mesh{name="Mid_ContinuousGrassland",indexFormat=IndexFormat.UInt32};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tr,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var old=AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);if(old){EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(old);return old;}AssetDatabase.CreateAsset(mesh,MeshPath);return mesh;
        }
        static GameObject Find(string name)=>SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t=>t.name==name)?.gameObject;
        static void FootContact(GameObject go,bool hill,float floor)
        {
            float correction=float.NegativeInfinity;
            foreach(var mf in go.GetComponentsInChildren<MeshFilter>())if(mf.name=="LeftLeg"||mf.name=="RightLeg")foreach(var v in mf.sharedMesh.vertices)
            {var p=mf.transform.TransformPoint(v);correction=Mathf.Max(correction,(hill?MidRampSurface.Height(p.x,p.z):floor)-p.y);}
            if(float.IsInfinity(correction))throw new Exception("발 메시 없음");go.transform.position+=Vector3.up*correction;PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
        }
        [MenuItem("Mid/Environment/4. Refine Landscape Layout")]
        public static void Build()
        {
            var scene=SceneManager.GetActiveScene();if(EditorApplication.isPlaying||scene.path!=Root+"/Mid_Landscape.unity"||scene.isDirty)throw new Exception("저장된 Mid_Landscape 편집 상태에서 실행하세요.");
            var texture=SurfaceTexture();var mesh=LandMesh();
            var mat=AssetDatabase.LoadAssetAtPath<Material>(MatPath);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,MatPath);}mat.SetColor("_BaseColor",Color.white);mat.SetTexture("_BaseMap",texture);mat.SetFloat("_Metallic",0);mat.SetFloat("_Smoothness",.08f);EditorUtility.SetDirty(mat);
            var temp=new GameObject("Mid_GrassLandscape");temp.AddComponent<MidRampSurface>();temp.AddComponent<MeshFilter>().sharedMesh=mesh;temp.AddComponent<MeshRenderer>().sharedMaterial=mat;temp.AddComponent<MeshCollider>().sharedMesh=mesh;
            var prefab=PrefabUtility.SaveAsPrefabAsset(temp,PrefabPath);Object.DestroyImmediate(temp);
            var land=Find("Mid_GrassLandscape");if(!land)land=(GameObject)PrefabUtility.InstantiatePrefab(prefab,Find("Environment_Stage04").transform);
            foreach(string old in new[]{"Path_Placeholder","Mid_Ramp","Ground"}){var g=Find(old);if(g)Undo.DestroyObjectImmediate(g);}
            var waiting=Find("WaitingPlace");if(!waiting)
            {
                waiting=new GameObject("WaitingPlace");
                foreach(string name in new[]{"Pavilion","Mid_S02_Table","02_WaitingArea","Hachiware","Pavilion Point Light","Pavilion Spot Light"})
                {var go=Find(name);if(!go)throw new Exception("정자 그룹 누락: "+name);go.transform.SetParent(waiting.transform,true);}
            }
            waiting.transform.position=WaitingShift;
            var rock=Find("Mid_Rock");var rest=AssetDatabase.LoadAssetAtPath<MidRockRestData>(Root+"/Meshes/Environment/Mid_Rock_RestData.asset").CopyVertices();
            rock.transform.position=MidRampSurface.RockCenter(rest,-2.65f,MidRampSurface.CenterZ,rock.transform.rotation);PrefabUtility.RecordPrefabInstancePropertyModifications(rock.transform);
            var chi=Find("Chiikawa");chi.transform.position=new Vector3(-3.90f,0,-.95f);
            var normal=MidRampSurface.Normal(chi.transform.position.x,chi.transform.position.z);
            var direction=Vector3.ProjectOnPlane(rock.transform.position-chi.transform.position,normal).normalized;
            chi.transform.rotation=Quaternion.LookRotation(-direction,normal);FootContact(chi,true,0);
            FootContact(Find("Hachiware"),false,.558f);
            ArrangeSingleFlower();
            foreach(var n in new[]{"RockTravel_Start","RockTravel_End"}){var marker=Find(n);if(marker){float x=n.EndsWith("Start")?MidRampSurface.TravelStart:MidRampSurface.TravelEnd;marker.transform.position=new Vector3(x,MidRampSurface.Height(x,MidRampSurface.CenterZ),MidRampSurface.CenterZ);}}
            var cam=Find("Mid_OverviewCamera").GetComponent<Camera>();cam.transform.position=new Vector3(1.15f,5.5f,-12.8f);cam.transform.LookAt(new Vector3(1.15f,.60f,.0f));cam.fieldOfView=35.5f;cam.aspect=16f/9f;
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Validate();Selection.activeGameObject=waiting;Debug.Log("지형과 정적 배치 갱신 완료");
        }
        public static void ArrangeSingleFlower()
        {
            foreach(var name in new[]{"Flower_2","Flower_3"}){var extra=Find(name);if(extra)Undo.DestroyObjectImmediate(extra);}
            var flower=Find("Flower_1");if(!flower)throw new Exception("기존 Flower_1이 없습니다.");
            Undo.RecordObject(flower.transform,"꽃 한 송이 크기 조정");
            flower.transform.localScale=Vector3.one*1.35f;
            var p=flower.transform.position;p.y=MidRampSurface.Height(p.x,p.z)-.002f;flower.transform.position=p;
            PrefabUtility.RecordPrefabInstancePropertyModifications(flower.transform);
        }
        [MenuItem("Mid/Environment/7. Align Path and Flowers")]
        public static void AlignPathAndFlowers()
        {
            var scene=SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying||scene.path!=Root+"/Mid_Landscape.unity"||scene.isDirty)throw new Exception("저장된 Mid_Landscape 편집 상태에서 실행하세요.");
            SurfaceTexture();
            ArrangeSingleFlower();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("계단 연결 흙길 및 잔디 위 꽃 배치 저장 완료");
        }
        [MenuItem("Mid/Environment/5. Validate Landscape Layout")]
        public static void Validate()
        {
            var sb=new StringBuilder("지형 및 정적 장면 배치 — 검증 기록\n\n");
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);var vertices=mesh.vertices;var tri=mesh.triangles;
            if(vertices.Any(v=>float.IsNaN(v.sqrMagnitude)))throw new Exception("NaN 정점");
            for(int i=0;i<tri.Length;i+=3)if(Vector3.Cross(vertices[tri[i+1]]-vertices[tri[i]],vertices[tri[i+2]]-vertices[tri[i]]).y<=0)throw new Exception("지형 뒤집힌 면/퇴화면");
            sb.AppendLine($"연속 지형: 정점 {vertices.Length}, 삼각형 {tri.Length/3}, 모든 면 위쪽. 단일 표면으로 길과 언덕 처리.");
            var rest=AssetDatabase.LoadAssetAtPath<MidRockRestData>(Root+"/Meshes/Environment/Mid_Rock_RestData.asset").CopyVertices();
            var collider=Find("Mid_GrassLandscape").GetComponent<MeshCollider>();
            foreach(float x in new[]{MidRampSurface.TravelStart,-2.5f,MidRampSurface.TravelEnd})
            {
                var pos=MidRampSurface.RockCenter(rest,x,MidRampSurface.CenterZ,Quaternion.identity);float gap=float.PositiveInfinity,actual=float.PositiveInfinity;
                foreach(var v in rest){var p=pos+v;gap=Mathf.Min(gap,p.y-MidRampSurface.Height(p.x,p.z));if(collider.Raycast(new Ray(new Vector3(p.x,8,p.z),Vector3.down),out var hit,20))actual=Mathf.Min(actual,p.y-hit.point.y);}
                if(Mathf.Abs(gap)>.0001f||Mathf.Abs(actual)>.006f)throw new Exception("돌 접촉 오차");sb.AppendLine($"돌 X={x:F2}: 함수 접촉={gap:F6}, 실제 삼각형 표면 접촉={actual:F6}");
            }
            foreach(string name in new[]{"Chiikawa","Hachiware"})
            {
                var g=Find(name);if(!PrefabUtility.IsPartOfPrefabInstance(g))throw new Exception("캐릭터 Prefab 연결 끊김");sb.AppendLine(name+" 위치="+g.transform.position.ToString("F3")+", 회전="+g.transform.eulerAngles.ToString("F2"));
            }
            sb.AppendLine("경사 18도, 이동 X=-3.20~-1.80 / Z=-1.40. 오르막은 +X, 내리막은 -X. 중앙 법선=(-sin18,cos18,0). Height/Normal/RockCenter를 다음 단계에서도 공유.");
            sb.AppendLine("정자 그룹 이동=(3.40,0,0.50), 내부 상대 위치 유지. 꽃 1송이 배치, 기존 Flower_1 대비 1.35배 크기.");
            sb.AppendLine("산책길 폭: 핵심 0.36m + 가장자리 전이(총 약0.84m). 별도 흙길 판 없음. 애니메이션 추가 없음.");
            File.WriteAllText(Root+"/Mid_LandscapeLayoutGuide.txt",sb.ToString());AssetDatabase.ImportAsset(Root+"/Mid_LandscapeLayoutGuide.txt");Debug.Log(sb.ToString());
        }

    }
}


