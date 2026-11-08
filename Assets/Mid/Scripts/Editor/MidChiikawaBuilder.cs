using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using MidProject.Characters;

namespace MidProject.EditorTools
{
    /// <summary>중간 프로젝트 제출용 절차적 캐릭터 생성 도구. 같은 경로의 에셋을 갱신하여 중복을 방지한다.</summary>
    public static class MidChiikawaBuilder
    {
        public const string PrefabPath="Assets/Mid/Prefabs/Chiikawa.prefab";
        public const string ReviewPath="Assets/Mid/Mid_ChiikawaReview.unity";
        const string MeshDir="Assets/Mid/Meshes/Chiikawa";
        const string MatDir="Assets/Mid/Materials/Chiikawa";
        static Material white,ink,highlight,cheek,stripe;

        static void Folder(string path)
        {
            if(AssetDatabase.IsValidFolder(path))return;
            int i=path.LastIndexOf('/');Folder(path.Substring(0,i));AssetDatabase.CreateFolder(path.Substring(0,i),path.Substring(i+1));
        }
        static Material Material(string name,Color color,float smooth=.18f)
        {
            string path=MatDir+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smooth);m.SetFloat("_Metallic",0);
            m.SetFloat("_SpecularHighlights",0);m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            m.SetColor("_EmissionColor",Color.black);m.DisableKeyword("_EMISSION");EditorUtility.SetDirty(m);return m;
        }
        static Mesh SaveMesh(Mesh generated)
        {
            string path=MeshDir+"/"+generated.name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing)
            {
                // 같은 에셋의 CPU 데이터와 GPU 버퍼를 함께 갱신하고 기존 GUID를 유지한다.
                existing.Clear();existing.indexFormat=generated.indexFormat;
                existing.vertices=generated.vertices;existing.triangles=generated.triangles;
                existing.uv=generated.uv;existing.normals=generated.normals;existing.bounds=generated.bounds;
                UnityEngine.Object.DestroyImmediate(generated);EditorUtility.SetDirty(existing);return existing;
            }
            AssetDatabase.CreateAsset(generated,path);return generated;
        }
        static Transform Group(string name,Transform parent)
        {var g=new GameObject(name);g.transform.SetParent(parent,false);return g.transform;}
        static Transform Part(string name,Transform parent,Mesh m,Material mat,Vector3 pos, bool decal=false)
        {
            var t=Group(name,parent);t.localPosition=pos;t.gameObject.AddComponent<MeshFilter>().sharedMesh=SaveMesh(m);
            var r=t.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=mat;
            if(decal){r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;}
            return t;
        }
        static Transform Ellipse(string name,Transform head,Vector2 center,Vector2 radii,float offset,Material mat)
        {var mesh=MidChiikawaGeometry.FaceEllipse("Chiikawa_"+name,center,radii,offset,out var anchor);return Part(name,head,mesh,mat,anchor,true);}
        static Transform Stroke(string name,Transform parent,Vector2[] points,float width,float offset,Material mat)
        {var mesh=MidChiikawaGeometry.FaceStroke("Chiikawa_"+name,points,width,offset,out var anchor);return Part(name,parent,mesh,mat,anchor,true);}
        static Vector2 XY(float x,float globalY)=>new Vector2(x,globalY-MidChiikawaGeometry.HeadBottom);

        [MenuItem("Mid/Chiikawa/1. Generate or Update Prefab")]
        public static void Generate()
        {
            Folder(MeshDir);Folder(MatDir);Folder("Assets/Mid/Prefabs");
            white=Material("Mat_Chiikawa_White",new Color(.94f,.94f,.925f));
            ink=Material("Mat_Chiikawa_Ink",new Color(.012f,.010f,.014f));
            highlight=Material("Mat_Chiikawa_Highlight",Color.white);
            cheek=Material("Mat_Chiikawa_Coral",new Color(.96f,.60f,.66f));
            stripe=Material("Mat_Chiikawa_CheekLines",new Color(.10f,.025f,.040f));
            var root=new GameObject("Chiikawa");
            try
            {
                Part("Body",root.transform,MidChiikawaGeometry.Body(),white,Vector3.zero);
                var head=Part("Head",root.transform,MidChiikawaGeometry.Head(),white,new Vector3(0,MidChiikawaGeometry.HeadBottom,0));
                for(int side=-1;side<=1;side+=2)
                {
                    string s=side<0?"Left":"Right";
                    var ear=Part(s+"Ear",head,MidChiikawaGeometry.Ellipsoid("Chiikawa_"+s+"Ear",new Vector3(.067f,.088f,.060f)),white,new Vector3(side*.225f,1.1135f-MidChiikawaGeometry.HeadBottom,.030f));
                    ear.localRotation=Quaternion.Euler(0,0,-side*17);
                    var arm=Part(s+"Arm",root.transform,MidChiikawaGeometry.Ellipsoid("Chiikawa_"+s+"Arm",new Vector3(.048f,.090f,.060f)),white,new Vector3(side*.324f,.310f,-.012f));
                    arm.localRotation=Quaternion.Euler(0,0,side*30);
                    Part(s+"Leg",root.transform,MidChiikawaGeometry.Leg("Chiikawa_"+s+"Leg"),white,new Vector3(side*.170f,.110f,-.010f));
                    float ex=side*.166f;
                    var eye=Ellipse(s+"Eye",head,XY(ex,.700f),new Vector2(.0685f,.074f),.0021f,ink);
                    var hi=Ellipse(s+"Eye_HighlightLarge",head,XY(ex-.008f,.725f),new Vector2(.0275f,.0235f),.0036f,highlight);hi.SetParent(eye,true);
                    var lo=Ellipse(s+"Eye_HighlightSmall",head,XY(ex+.001f,.670f),new Vector2(.0255f,.012f),.0036f,highlight);lo.SetParent(eye,true);
                }
                var brows=Group("Eyebrows",head);
                for(int side=-1;side<=1;side+=2)
                {
                    float x=side*.160f;
                    Stroke((side<0?"Left":"Right")+"Eyebrow",brows,MidChiikawaGeometry.Bezier(XY(x-.027f,.817f),XY(x-.012f,.824f),XY(x+.013f,.822f),XY(x+.027f,.821f)),.013f,.0022f,ink);
                }
                var mouth=Group("Mouth",head);
                Stroke("OmegaMouth",mouth,MidChiikawaGeometry.Bezier(
                    XY(-.075f,.567f),XY(-.066f,.536f),XY(-.025f,.538f),XY(0,.562f),
                    XY(.025f,.538f),XY(.052f,.535f),XY(.073f,.566f)),.014f,.0023f,ink);
                Stroke("MouthCenter",mouth,MidChiikawaGeometry.Bezier(XY(0,.564f),XY(-.001f,.579f),XY(.001f,.587f),XY(.001f,.595f)),.014f,.0024f,ink);
                Ellipse("MouthTopMark",mouth,XY(.001f,.596f),new Vector2(.018f,.0065f),.0025f,ink);
                Ellipse("LowerMouthMark",mouth,XY(0,.503f),new Vector2(.022f,.0075f),.0022f,ink);
                var cheeks=Group("Cheeks",head);
                for(int side=-1;side<=1;side+=2)
                {
                    string s=side<0?"Left":"Right";float x=side*.292f;
                    var blush=Ellipse(s+"Cheek",cheeks,XY(x,.603f),new Vector2(.088f,.060f),.0015f,cheek);
                    for(int i=0;i<4;i++)
                    {
                        float dx=(i-1.5f)*.033f;float half=(i==0||i==3)?.021f:.028f;
                        var line=Stroke(s+"BlushLine_"+(i+1),cheeks,MidChiikawaGeometry.Bezier(XY(x+dx-.007f,.603f-half),XY(x+dx-.003f,.601f),XY(x+dx+.003f,.606f),XY(x+dx+.009f,.603f+half)),.010f,.0030f,stripe);
                        line.SetParent(blush,true);
                    }
                }
                Part("Tail",root.transform,MidChiikawaGeometry.Ellipsoid("Chiikawa_Tail",new Vector3(.043f,.050f,.050f)),white,new Vector3(0,.145f,.226f));
                ApplyAppearanceRefinement(root.transform);
                PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);AssetDatabase.SaveAssets();
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }

        // 기존 프리팹을 열어 필요한 부위만 갱신한다. 계층, 재질과 오브젝트 식별자는 유지한다.
        [MenuItem("Mid/Chiikawa/4. Refine Existing Appearance")]
        public static void RefineExistingAppearance()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before editing the prefab.");
            var root=PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                ApplyAppearanceRefinement(root.transform);
                PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
                AssetDatabase.SaveAssets();
            }
            finally {PrefabUtility.UnloadPrefabContents(root);}
        }

        static void UpdatePart(Transform part,Mesh mesh,Vector3 position,Quaternion rotation)
        {
            part.GetComponent<MeshFilter>().sharedMesh=SaveMesh(mesh);
            part.localPosition=position;part.localRotation=rotation;
        }
        static void UpdateFace(Transform part,Transform head,Mesh mesh,Vector3 anchor)
        {
            part.GetComponent<MeshFilter>().sharedMesh=SaveMesh(mesh);
            part.position=head.TransformPoint(anchor);
        }
        static void RefinedStroke(Transform head,string path,Vector2[] points,float width,float offset)
        {
            var part=head.Find(path);
            var mesh=MidChiikawaGeometry.FaceStroke("Chiikawa_"+part.name,points,width,offset,out var anchor);
            UpdateFace(part,head,mesh,anchor);
        }
        static void RefinedEllipse(Transform head,string path,Vector2 center,Vector2 radii,float offset)
        {
            var part=head.Find(path);
            var mesh=MidChiikawaGeometry.FaceEllipse("Chiikawa_"+part.name,center,radii,offset,out var anchor);
            UpdateFace(part,head,mesh,anchor);
        }
        const float facialStrokeWidth=.022f; // 입·눈썹 선 굵기
        const float blushStrokeWidth=.02f; // 볼터치 줄무늬만 별도로 조절
        const float browYOffset=.02f; // 눈썹만 이동

        static void ApplyAppearanceRefinement(Transform root)
        {
            // 볼터치 줄무늬는 입·눈썹과 별도 굵기를 사용한다.
            // 얼굴 위치 미세 조정: 양수는 위, 음수는 아래. 머리 곡면에 다시 밀착시킨다.
            const float eyeYOffset=.01f;  // 눈과 눈동자 하이라이트 위치
            Vector2 EyePoint(float x,float y)=>XY(x,y+eyeYOffset);
            var head=root.Find("Head");
            for(int side=-1;side<=1;side+=2)
            {
                string s=side<0?"Left":"Right";
                float eyeX=side*.166f;
                RefinedEllipse(head,s+"Eye",EyePoint(eyeX,.700f),new Vector2(.0685f,.074f),.0021f);
                RefinedEllipse(head,s+"Eye/"+s+"Eye_HighlightLarge",EyePoint(eyeX-.008f,.725f),new Vector2(.0275f,.0235f),.0036f);
                RefinedEllipse(head,s+"Eye/"+s+"Eye_HighlightSmall",EyePoint(eyeX+.001f,.670f),new Vector2(.0255f,.012f),.0036f);
                ApplyBrowPose(head,side);
                UpdatePart(head.Find(s+"Ear"),MidChiikawaGeometry.Ellipsoid("Chiikawa_"+s+"Ear",new Vector3(.0781f,.070f,.0642f)),
                    new Vector3(side*.225f,1.088f-MidChiikawaGeometry.HeadBottom,.030f),Quaternion.Euler(0,0,-side*6));
                UpdatePart(root.Find(s+"Arm"),MidChiikawaGeometry.RoundedArm("Chiikawa_"+s+"Arm"),
                    new Vector3(side*.312f,.310f,-.012f),Quaternion.Euler(0,0,side*30));
                ApplyArmPose(root,side);
                UpdatePart(root.Find(s+"Leg"),MidChiikawaGeometry.Leg("Chiikawa_"+s+"Leg"),
                    new Vector3(side*.088f,.082f,-.010f),Quaternion.identity);
                ApplyBlushLines(head,side);
            }
            // 꼬리 지름의 약 절반이 몸통 뒤 곡면에 겹치도록 배치한다.
            UpdatePart(root.Find("Tail"),MidChiikawaGeometry.Ellipsoid("Chiikawa_Tail",Vector3.one*.0505f),
                new Vector3(0,.145f,.180f),Quaternion.identity);
            ApplySimpleMouth(head);
            MidCharacterEyeSizing.Apply(root);
        }

        static void ApplyBlushLines(Transform head,int side)
        {
            string s=side<0?"Left":"Right";
            float x=side*.292f;
            for(int i=0;i<4;i++)
            {
                float dx=(i-1.5f)*.033f, half=(i==0||i==3)?.016f:.021f;
                RefinedStroke(head,"Cheeks/"+s+"Cheek/"+s+"BlushLine_"+(i+1),
                    MidChiikawaGeometry.Bezier(XY(x+dx-.005f,.603f-half),XY(x+dx-.002f,.602f),XY(x+dx+.002f,.605f),XY(x+dx+.006f,.603f+half)),blushStrokeWidth,.003f);
            }
        }

        static void ApplySimpleMouth(Transform head)
        {
            const float mouthYOffset=-.02f;
            Vector2 MouthPoint(float x,float y)=>XY(x,y+mouthYOffset);
            RefinedStroke(head,"Mouth/OmegaMouth",MidChiikawaGeometry.Bezier(
                MouthPoint(-.076f,.620f),MouthPoint(-.066f,.590f),MouthPoint(-.026f,.590f),MouthPoint(0,.614f),
                MouthPoint(.026f,.590f),MouthPoint(.061f,.590f),MouthPoint(.076f,.620f)),facialStrokeWidth,.0023f);
            RefinedStroke(head,"Mouth/MouthCenter",MidChiikawaGeometry.Bezier(
                MouthPoint(0,.614f),MouthPoint(0,.624f),MouthPoint(0,.636f),MouthPoint(0,.646f)),facialStrokeWidth,.0024f);
            RefinedEllipse(head,"Mouth/LowerMouthMark",MouthPoint(0,.554f),new Vector2(.022f,.009f),.0022f);
            // 세로선 끝만 약간 도톰한 원형으로 마감한다. 가로로 긴 코 모양은 만들지 않는다.
            const float mouthTipRadius=.0125f;
            RefinedEllipse(head,"Mouth/MouthTopMark",MouthPoint(0,.646f),Vector2.one*mouthTipRadius,.0025f);
            head.Find("Mouth/MouthCenter").gameObject.SetActive(true);
            head.Find("Mouth/MouthTopMark").gameObject.SetActive(true);
            head.Find("Mouth/LowerMouthMark").gameObject.SetActive(true);
        }

        [MenuItem("Mid/Chiikawa/7. Update Mouth and Blush Only")]
        public static void UpdateMouthAndBlushOnly()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before editing the prefab.");
            var root=PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var head=root.transform.Find("Head");
                ApplyBlushLines(head,-1);ApplyBlushLines(head,1);ApplySimpleMouth(head);
                PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);AssetDatabase.SaveAssets();
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }

        static void ApplyBrowPose(Transform head,int side)
        {
            const float browInnerLiftDegrees=8f; // 바깥쪽 끝을 유지하고 안쪽 끝만 살짝 올린다.
            float browX=side*.160f;
            float slope=Mathf.Tan(browInnerLiftDegrees*Mathf.Deg2Rad);
            Vector2 Point(float dx,float y)=>XY(browX+dx,y+browYOffset+(.026f-side*dx)*slope);
            RefinedStroke(head,"Eyebrows/"+(side<0?"Left":"Right")+"Eyebrow",MidChiikawaGeometry.Bezier(
                Point(-.026f,.817f),Point(-.012f,.821f),Point(.012f,.820f),Point(.026f,.821f)),facialStrokeWidth,.0022f);
        }

        [MenuItem("Mid/Chiikawa/6. Update Eyebrows Only")]
        public static void UpdateEyebrowsOnly()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before editing the prefab.");
            var root=PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var head=root.transform.Find("Head");
                ApplyBrowPose(head,-1);ApplyBrowPose(head,1);
                PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);AssetDatabase.SaveAssets();
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }

        static void ApplyArmPose(Transform root,int side)
        {
            const float armLiftDegrees=5f; // 기존 자세에서 손끝을 올리는 추가 각도
            var arm=root.Find((side<0?"Left":"Right")+"Arm");
            var originalRotation=Quaternion.Euler(0,0,side*30f);
            var shoulderInArm=new Vector3(0,.090f,0); // 몸통에 붙은 팔 위쪽 끝
            var shoulder=new Vector3(side*.312f,.310f,-.012f)+originalRotation*shoulderInArm;
            var rotation=Quaternion.Euler(0,0,side*(30f+armLiftDegrees));
            // 기존 부위 Pivot·계층은 유지하면서 어깨 위치가 고정되도록 중심을 보정한다.
            arm.localRotation=rotation;
            arm.localPosition=shoulder-rotation*shoulderInArm;
        }

        [MenuItem("Mid/Chiikawa/5. Update Arm Pose Only")]
        public static void UpdateArmPoseOnly()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before editing the prefab.");
            var root=PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                ApplyArmPose(root.transform,-1);ApplyArmPose(root.transform,1);
                PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);AssetDatabase.SaveAssets();
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }

        [MenuItem("Mid/Chiikawa/2. Open Appearance Review")]
        public static void OpenReview()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before opening review.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Unsaved scene present. Save your scene before opening review.");
            if(!AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath))Generate();
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(ReviewPath)) {EditorSceneManager.OpenScene(ReviewPath);Front();return;}
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),scene);
            RenderSettings.skybox=null;RenderSettings.fog=false;RenderSettings.ambientMode=AmbientMode.Flat;
            RenderSettings.ambientLight=new Color(.70f,.70f,.70f);RenderSettings.ambientIntensity=1;
            var ground=new GameObject("Review_Ground");var mf=ground.AddComponent<MeshFilter>();
            var m=new Mesh{name="Chiikawa_ReviewGround"};m.vertices=new[]{new Vector3(-200,-.002f,-200),new Vector3(-200,-.002f,200),new Vector3(200,-.002f,200),new Vector3(200,-.002f,-200)};m.triangles=new[]{0,1,2,0,2,3};m.RecalculateNormals();m.RecalculateBounds();mf.sharedMesh=SaveMesh(m);
            ground.AddComponent<MeshRenderer>().sharedMaterial=Material("Mat_Chiikawa_ReviewGround",new Color(.78f,.80f,.82f));
            Light key=new GameObject("Review_KeyLight").AddComponent<Light>();key.type=LightType.Directional;key.transform.rotation=Quaternion.Euler(28,-27,0);key.color=Color.white;key.intensity=.85f;key.shadows=LightShadows.None;RenderSettings.sun=key;
            Light fill=new GameObject("Review_FillLight").AddComponent<Light>();fill.type=LightType.Directional;fill.transform.rotation=Quaternion.Euler(18,140,0);fill.color=Color.white;fill.intensity=.30f;fill.shadows=LightShadows.None;
            Camera cam=new GameObject("Chiikawa_ReviewCamera").AddComponent<Camera>();cam.tag="MainCamera";cam.gameObject.AddComponent<AudioListener>();cam.orthographic=true;cam.orthographicSize=.70f;cam.nearClipPlane=.05f;cam.farClipPlane=30;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.88f,.89f,.90f);cam.allowHDR=false;cam.allowMSAA=true;
            var data=cam.GetUniversalAdditionalCameraData();data.renderPostProcessing=false;data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;data.antialiasingQuality=AntialiasingQuality.High;
            cam.gameObject.AddComponent<MidCharacterOrbitCamera>();
            Front();EditorSceneManager.SaveScene(scene,ReviewPath);AssetDatabase.SaveAssets();
            Selection.activeGameObject=scene.GetRootGameObjects().First(g=>g.name=="Chiikawa");
            EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
        }
        [MenuItem("Mid/Chiikawa/3. Enable Mouse Orbit")]
        public static void EnableMouseOrbit()
        {
            if(EditorApplication.isPlaying || SceneManager.GetActiveScene().path!=ReviewPath)return;
            var camera=GameObject.Find("Chiikawa_ReviewCamera");if(!camera)return;
            if(!camera.GetComponent<MidCharacterOrbitCamera>())Undo.AddComponent<MidCharacterOrbitCamera>(camera);
            Front();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }

        static void View(float yaw,float elevation=0)
        {
            if(SceneManager.GetActiveScene().path!=ReviewPath && !string.IsNullOrEmpty(SceneManager.GetActiveScene().path))return;
            var cam=GameObject.Find("Chiikawa_ReviewCamera");if(!cam)return;
            Vector3 target=new Vector3(0,.60f,0);cam.transform.position=target+Quaternion.Euler(elevation,yaw,0)*new Vector3(0,0,-3);
            cam.transform.LookAt(target);SceneView.RepaintAll();
        }
        [MenuItem("Mid/Chiikawa/View/Front")] public static void Front()=>View(0);
        [MenuItem("Mid/Chiikawa/View/Left Three Quarter")] public static void LeftThreeQuarter()=>View(35);
        [MenuItem("Mid/Chiikawa/View/Right Three Quarter")] public static void RightThreeQuarter()=>View(-35);
        [MenuItem("Mid/Chiikawa/View/Side")] public static void Side()=>View(90);
        [MenuItem("Mid/Chiikawa/View/Back")] public static void Back()=>View(180);
    }
}
