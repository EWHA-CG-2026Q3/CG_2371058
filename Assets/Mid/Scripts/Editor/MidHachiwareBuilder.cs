using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using MidProject.Characters;

namespace MidProject.EditorTools
{
    /// <summary>승인된 치이카와의 저장된 형태를 독립 복제한 뒤 하치와레의 특징만 생성한다.</summary>
    public static class MidHachiwareBuilder
    {
        public const string PrefabPath="Assets/Mid/Prefabs/Hachiware.prefab";
        public const string ReviewPath="Assets/Mid/Mid_CharacterReview.unity";
        const string MeshDir="Assets/Mid/Meshes/Hachiware";
        const string MatDir="Assets/Mid/Materials/Hachiware";
        const string TexturePath="Assets/Mid/Textures/Hachiware/Hachiware_HeadPattern.png";
        static readonly Color Blue=new Color32(116,164,185,255); // 기준색 #74A4B9

        static void Folder(string path)
        {
            if(AssetDatabase.IsValidFolder(path))return;
            int i=path.LastIndexOf('/');Folder(path.Substring(0,i));
            AssetDatabase.CreateFolder(path.Substring(0,i),path.Substring(i+1));
        }

        static Mesh SaveMesh(Mesh source,string name)
        {
            string path=MeshDir+"/"+name+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!mesh){source.name=name;AssetDatabase.CreateAsset(source,path);return source;}
            mesh.Clear();mesh.indexFormat=source.indexFormat;
            mesh.vertices=source.vertices;mesh.triangles=source.triangles;mesh.uv=source.uv;
            mesh.normals=source.normals;mesh.bounds=source.bounds;
            UnityEngine.Object.DestroyImmediate(source);EditorUtility.SetDirty(mesh);return mesh;
        }

        static Material CopyMaterial(Material source,string name)
        {
            string path=MatDir+"/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material){material=new Material(source){name=name};AssetDatabase.CreateAsset(material,path);}
            return material;
        }

        [MenuItem("Mid/Hachiware/1. Generate Independent Prefab %#h")]
        public static void Generate()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before generating the prefab.");
            Folder(MeshDir);Folder(MatDir);Folder("Assets/Mid/Textures/Hachiware");
            bool exists=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var root=PrefabUtility.LoadPrefabContents(exists?PrefabPath:MidChiikawaBuilder.PrefabPath);
            try
            {
                root.name="Hachiware";
                // 치이카와 에셋에 쓰지 않는다. 동일 형태도 별도의 GUID를 가진 메시·재질로 복제한다.
                foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
                {
                    if(AssetDatabase.GetAssetPath(filter.sharedMesh).StartsWith(MeshDir+"/"))continue;
                    var copy=UnityEngine.Object.Instantiate(filter.sharedMesh);
                    filter.sharedMesh=SaveMesh(copy,"Hachiware_"+filter.name);
                }
                foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                    renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>
                        AssetDatabase.GetAssetPath(m).StartsWith(MatDir+"/")?m:
                        CopyMaterial(m,m.name.Replace("Chiikawa","Hachiware"))).ToArray();

                var head=root.transform.Find("Head");
                var white=root.transform.Find("Body").GetComponent<MeshRenderer>().sharedMaterial;
                var blue=CopyMaterial(white,"Mat_Hachiware_Blue");
                blue.SetColor("_BaseColor",Blue);
                blue.SetColor("_Color",Blue);
                blue.SetFloat("_Smoothness",.18f);blue.SetFloat("_Metallic",0);
                blue.SetColor("_EmissionColor",Color.black);blue.DisableKeyword("_EMISSION");
                EditorUtility.SetDirty(blue);

                var pattern=CreateHeadTexture(white.GetColor("_BaseColor"));
                var headMat=CopyMaterial(white,"Mat_Hachiware_HeadPattern");
                headMat.SetTexture("_BaseMap",pattern);headMat.SetTextureScale("_BaseMap",Vector2.one);
                headMat.SetTextureOffset("_BaseMap",Vector2.zero);
                // 흰 영역의 기존 색은 텍스처에 보존하고 파란 영역에 별도 틴트가 곱해지지 않게 한다.
                headMat.SetColor("_BaseColor",Color.white);
                headMat.SetColor("_Color",Color.white);
                EditorUtility.SetDirty(headMat);head.GetComponent<MeshRenderer>().sharedMaterial=headMat;
                for(int side=-1;side<=1;side+=2)
                {
                    string prefix=side<0?"Left":"Right";
                    var ear=head.Find(prefix+"Ear");
                    ear.GetComponent<MeshFilter>().sharedMesh=SaveMesh(CatEar(side),"Hachiware_"+prefix+"Ear");
                    ear.localPosition=new Vector3(side*.228f,.850f-MidChiikawaGeometry.HeadBottom,.025f);
                    ear.localRotation=Quaternion.identity;ear.localScale=Vector3.one*1.04f;
                    ear.GetComponent<MeshRenderer>().sharedMaterial=blue;
                    // 사진의 눈썹은 긴 곡선이 아니라 짧은 타원형 점에 가깝다.
                    var brow=head.Find("Eyebrows/"+prefix+"Eyebrow");
                    var mesh=MidChiikawaGeometry.FaceEllipse("Hachiware_"+prefix+"Eyebrow",
                        new Vector2(side*.160f,.839f-MidChiikawaGeometry.HeadBottom),
                        new Vector2(.0165f,.0105f),.0022f,out var anchor);
                    brow.GetComponent<MeshFilter>().sharedMesh=SaveMesh(mesh,mesh.name);
                    brow.position=head.TransformPoint(anchor);brow.localRotation=Quaternion.identity;
                }
                // 측면 사진처럼 뒤로 뻗다가 끝이 조금 올라가는 둥근 꼬리.
                var tail=root.transform.Find("Tail");
                tail.GetComponent<MeshFilter>().sharedMesh=SaveMesh(CurvedTail(),"Hachiware_Tail");
                tail.localPosition=new Vector3(0,.165f,.155f);
                tail.localRotation=Quaternion.identity;tail.localScale=Vector3.one;
                tail.GetComponent<MeshRenderer>().sharedMaterial=blue;
                MidCharacterEyeSizing.Apply(root.transform);
                PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);AssetDatabase.SaveAssets();
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            Debug.Log("하치와레 독립 Prefab 저장 완료: "+PrefabPath);
        }

        static Texture2D CreateHeadTexture(Color whiteColor)
        {
            const int width=2048,height=1024;
            var texture=new Texture2D(width,height,TextureFormat.RGBA32,true,false);
            var pixels=new Color32[width*height];
            // 원본 머리의 UV와 같은 위도·경도 수식. 앞/뒤 중앙에 흰색이 올라오는 완만한 경계.
            for(int y=0;y<height;y++)
            {
                float latitude=((y+.5f)/height-.5f)*Mathf.PI;
                float s=Mathf.Sin(latitude),c=Mathf.Max(0,Mathf.Cos(latitude));
                float globalY=MidChiikawaGeometry.HeadBottom+MidChiikawaGeometry.HeadEquator+
                    s*(s<0?MidChiikawaGeometry.HeadEquator:MidChiikawaGeometry.HeadUpperHeight);
                float halfWidth=MidChiikawaGeometry.HeadHalfWidth*Mathf.Pow(c,s<0?.65f:.90f);
                for(int x=0;x<width;x++)
                {
                    float longitude=(x+.5f)/width*2*Mathf.PI;
                    float surfaceX=halfWidth*Mathf.Cos(longitude);
                    float boundary=.890f+.100f*Mathf.Exp(-Mathf.Pow(Mathf.Abs(surfaceX)/.125f,1.3f))*
                        Mathf.Pow(Mathf.Abs(Mathf.Sin(longitude)),4);
                    float blend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.0012f,.0012f,globalY-boundary));
                    pixels[y*width+x]=Color.Lerp(whiteColor,Blue,blend);
                }
            }
            texture.SetPixels32(pixels);texture.Apply();
            File.WriteAllBytes(TexturePath,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(TexturePath,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(TexturePath);
            importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.maxTextureSize=2048;importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;
            importer.wrapModeU=TextureWrapMode.Repeat;importer.wrapModeV=TextureWrapMode.Clamp;
            importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        }

        static Mesh CatEar(int side)
        {
            const int rows=64,cols=80;
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            // 정면은 넓은 삼각형: 바깥 변은 거의 세우고 안쪽 변을 길게 기울인다.
            // 위로 갈수록 단면을 부드럽게 보강해 높이는 유지하고 끝의 곡률 반경을 넓힌다.
            for(int row=0;row<=rows;row++)
            {
                float t=(float)row/rows;
                float width=.140f*Mathf.Sqrt(Mathf.Max(0,1-t))*(1+.35f*t*t);
                float baseRound=Mathf.Sqrt(Mathf.Clamp01(t/.08f));
                width*=baseRound;
                float depth=.074f*Mathf.Sqrt(Mathf.Max(0,Mathf.Sin(Mathf.PI*t)))*(1+.25f*t*t);
                for(int col=0;col<=cols;col++)
                {
                    float b=2*Mathf.PI*col/cols;
                    vertices.Add(new Vector3(side*.050f*t+width*Mathf.Cos(b),
                        .330f*t,depth*Mathf.Sin(b)));
                    uv.Add(new Vector2((float)col/cols,(float)row/rows));
                }
            }
            for(int row=0;row<rows;row++)for(int col=0;col<cols;col++)
            {
                int a=row*(cols+1)+col,b=a+cols+1;
                triangles.AddRange(new[]{a,b,a+1,a+1,b,b+1});
            }
            var mesh=new Mesh{name="Hachiware_CatEar",indexFormat=IndexFormat.UInt32};
            mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();
            var normals=mesh.normals;
            for(int row=0;row<=rows;row++)
            {
                int a=row*(cols+1),b=a+cols;var n=(normals[a]+normals[b]).normalized;
                if(row==0)n=Vector3.down;if(row==rows)n=Vector3.up;
                normals[a]=normals[b]=n;
                if(row==0||row==rows)for(int col=0;col<=cols;col++)normals[a+col]=n;
            }
            mesh.normals=normals;mesh.RecalculateBounds();return mesh;
        }

        static Vector3 TailCenter(float t)
        {
            // 뿌리는 몸 안에 겹치고, 중간은 낮아지며 둥근 끝만 위로 향한다.
            var a=Vector3.zero;var b=new Vector3(0,-.028f,.070f);
            var c=new Vector3(0,-.025f,.138f);var d=new Vector3(0,.006f,.175f);
            float u=1-t;return u*u*u*a+3*u*u*t*b+3*u*t*t*c+t*t*t*d;
        }

        static Mesh CurvedTail()
        {
            const int rows=96,cols=48;
            var v=new List<Vector3>();var uv=new List<Vector2>();var tri=new List<int>();
            for(int i=0;i<=rows;i++)
            {
                float s=(float)i/rows,t=Mathf.Clamp01((s-.12f)/.76f);
                var tangent=(TailCenter(Mathf.Min(1,t+.001f))-TailCenter(Mathf.Max(0,t-.001f))).normalized;
                var normal=Vector3.Cross(tangent,Vector3.right).normalized;
                // 가는 뿌리에서 도톰한 끝으로 서서히 굵어지고 마지막은 반구로 마감한다.
                var center=TailCenter(t);float radius=Mathf.Lerp(.032f,.050f,Mathf.SmoothStep(0,1,t));
                if(s<.12f){float q=(.12f-s)/.12f;center-=tangent*(radius*q);radius*=Mathf.Sqrt(1-q*q);}
                if(s>.88f){float q=(s-.88f)/.12f;center+=tangent*(radius*q);radius*=Mathf.Sqrt(Mathf.Max(0,1-q*q));}
                for(int j=0;j<=cols;j++)
                {
                    float a=2*Mathf.PI*j/cols;
                    v.Add(center+radius*(Vector3.right*Mathf.Cos(a)+normal*Mathf.Sin(a)));
                    uv.Add(new Vector2((float)j/cols,s));
                }
            }
            for(int i=0;i<rows;i++)for(int j=0;j<cols;j++)
            {int a=i*(cols+1)+j,b=a+cols+1;tri.AddRange(new[]{a,a+1,b,a+1,b+1,b});}
            var mesh=new Mesh{name="Hachiware_CurvedTail"};
            mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();
            var n=mesh.normals;
            for(int i=0;i<=rows;i++)
            {
                int a=i*(cols+1),b=a+cols;var mean=(n[a]+n[b]).normalized;n[a]=n[b]=mean;
                if(i==0||i==rows)
                {
                    var tangent=i==0?-(TailCenter(.001f)-TailCenter(0)).normalized:(TailCenter(1)-TailCenter(.999f)).normalized;
                    for(int j=0;j<=cols;j++)n[a+j]=tangent;
                }
            }
            mesh.normals=n;mesh.RecalculateBounds();return mesh;
        }

        [MenuItem("Mid/Hachiware/2. Open Pair Review")]
        public static void OpenReview()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before opening review.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save the current scene before opening review.");
            if(!AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath))Generate();
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(ReviewPath))
            {EditorSceneManager.OpenScene(ReviewPath);Front();FocusGame();return;}
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var chi=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MidChiikawaBuilder.PrefabPath),scene);
            var hachi=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),scene);
            chi.transform.position=new Vector3(-.66f,0,0);hachi.transform.position=new Vector3(.66f,0,0);
            RenderSettings.skybox=null;RenderSettings.fog=false;RenderSettings.ambientMode=AmbientMode.Flat;
            RenderSettings.ambientLight=new Color(.70f,.70f,.70f);RenderSettings.ambientIntensity=1;
            Light key=new GameObject("Review_KeyLight").AddComponent<Light>();key.type=LightType.Directional;
            key.transform.rotation=Quaternion.Euler(28,-27,0);key.color=Color.white;key.intensity=.85f;key.shadows=LightShadows.None;
            Light fill=new GameObject("Review_FillLight").AddComponent<Light>();fill.type=LightType.Directional;
            fill.transform.rotation=Quaternion.Euler(18,140,0);fill.color=Color.white;fill.intensity=.30f;fill.shadows=LightShadows.None;
            RenderSettings.sun=key;
            Camera cam=new GameObject("Pair_ReviewCamera").AddComponent<Camera>();cam.tag="MainCamera";
            cam.gameObject.AddComponent<AudioListener>();cam.orthographic=true;cam.orthographicSize=.76f;
            cam.transform.position=new Vector3(0,.59f,-4);cam.transform.rotation=Quaternion.identity;
            cam.nearClipPlane=.05f;cam.farClipPlane=30;cam.clearFlags=CameraClearFlags.SolidColor;
            cam.backgroundColor=new Color(.88f,.89f,.90f);cam.allowHDR=false;cam.allowMSAA=true;
            var data=cam.GetUniversalAdditionalCameraData();data.renderPostProcessing=false;
            data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;data.antialiasingQuality=AntialiasingQuality.High;
            var review=cam.gameObject.AddComponent<MidCharacterPairReview>();
            review.characters=new[]{chi.transform,hachi.transform};
            EditorSceneManager.SaveScene(scene,ReviewPath);AssetDatabase.SaveAssets();FocusGame();
        }

        static void FocusGame()=>EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
        static void View(float angle)
        {
            if(SceneManager.GetActiveScene().path!=ReviewPath)return;
            foreach(var name in new[]{"Chiikawa","Hachiware"})
            {
                var go=GameObject.Find(name);if(!go)continue;
                Undo.RecordObject(go.transform,"Character review angle");go.transform.rotation=Quaternion.Euler(0,angle,0);
            }
            SceneView.RepaintAll();EditorApplication.QueuePlayerLoopUpdate();
        }
        [MenuItem("Mid/Hachiware/View/Front")]public static void Front()=>View(0);
        [MenuItem("Mid/Hachiware/View/Left 45")]public static void Left()=>View(45);
        [MenuItem("Mid/Hachiware/View/Right 45")]public static void Right()=>View(-45);
        [MenuItem("Mid/Hachiware/View/Side")]public static void Side()=>View(90);
        [MenuItem("Mid/Hachiware/View/Back")]public static void Back()=>View(180);


    }
}
