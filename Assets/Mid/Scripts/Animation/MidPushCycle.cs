using System;
using UnityEngine;
using MidProject.Environment;
namespace MidProject.Animation
{
    /// <summary>12초: 밀기 0~5, 버티기 5~6, 후퇴 6~8, 쉬기/준비 8~12. 에셋 원본은 변경하지 않는다.</summary>
    public sealed class MidPushCycle : MonoBehaviour
    {
        public MeshFilter rock;
        public MidRockRestData restData;
        public Transform chiikawa;
        public float period=12f;
        public float effectiveRadius=.55f;
        const float pushLean=30f;
        [NonSerialized] public float phase,handGap,groundGap;
        [NonSerialized] public Vector3 rockCenter;
        Mesh originalMesh,animatedMesh;Vector3[] rest,normals,world,rotated,outNormals;int[] triangles;
        Transform head;Quaternion headRot;Vector3 headPos;Transform[] arms,legs;Vector3[] armPos,legPos;Quaternion[] armRot,legRot;
        Vector3 chiPos,rockPos,rockScale;Quaternion chiRot,rockRot;
        Vector3[] planted=new Vector3[2],stepFrom=new Vector3[2],stepTo=new Vector3[2];
        int stepping=-1,nextFoot;float stepStarted,stepDuration,lastFootTime;Vector3 lastRoot;double started;bool ready;bool colliderWasEnabled;
        readonly Vector3 facing=new Vector3(.94f,0,-.342f);
        public Vector3[] CurrentRockVertices=>world;
        public void Initialize()
        {
            if(ready)return;if(!rock||!restData||!chiikawa)throw new InvalidOperationException("돌/원본 정점/치이카와 참조 누락");
            originalMesh=rock.sharedMesh;rest=restData.CopyVertices();triangles=restData.CopyTriangles();normals=originalMesh.normals;
            world=new Vector3[rest.Length];rotated=new Vector3[rest.Length];outNormals=new Vector3[rest.Length];
            rockPos=rock.transform.localPosition;rockRot=rock.transform.localRotation;rockScale=rock.transform.localScale;
            chiPos=chiikawa.position;chiRot=chiikawa.rotation;head=chiikawa.Find("Head");headRot=head.localRotation;headPos=head.localPosition;
            arms=new[]{chiikawa.Find("LeftArm"),chiikawa.Find("RightArm")};legs=new[]{chiikawa.Find("LeftLeg"),chiikawa.Find("RightLeg")};
            armPos=new Vector3[2];armRot=new Quaternion[2];legPos=new Vector3[2];legRot=new Quaternion[2];
            for(int i=0;i<2;i++){armPos[i]=arms[i].localPosition;armRot[i]=arms[i].localRotation;legPos[i]=legs[i].localPosition;legRot[i]=legs[i].localRotation;}
            animatedMesh=Instantiate(originalMesh);animatedMesh.name="Mid_Rock_RuntimeMatrix";animatedMesh.MarkDynamic();rock.sharedMesh=animatedMesh;
            // 월드 정점 메시이므로 Transform을 항등으로 두어 이중 이동을 방지한다.
            rock.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);rock.transform.localScale=Vector3.one;
            var collider=rock.GetComponent<Collider>();if(collider){colliderWasEnabled=collider.enabled;collider.enabled=false;}
            ready=true;started=Time.timeAsDouble;Evaluate(0,true);
        }
        void OnEnable(){Initialize();}
        void Update(){Evaluate((float)(Time.timeAsDouble-started));}
        static float Ease(float t){t=Mathf.Clamp01(t);return t*t*t*(10+t*(-15+6*t));}
        public void Evaluate(float elapsed,bool resetFeet=false)
        {
            if(!ready)return;phase=Mathf.Repeat(elapsed,period);
            float u=phase<5?Ease(phase/5):phase<6?1:phase<8?1-Ease((phase-6)/2):0;
            float x=Mathf.Lerp(MidRampSurface.TravelStart,MidRampSurface.TravelEnd,u);
            float distance=(x-MidRampSurface.TravelStart)/Mathf.Cos(MidRampSurface.Angle*Mathf.Deg2Rad);
            var normal=new Vector3(-Mathf.Sin(18*Mathf.Deg2Rad),Mathf.Cos(18*Mathf.Deg2Rad),0);
            var tangent=new Vector3(normal.y,-normal.x,0);var axis=Vector3.Cross(normal,tangent).normalized;
            // +X 진행에는 -Z축 회전. 복귀할 때 이동 거리와 회전각도 함께 되돌아온다.
            var rotation=MidMatrixMath.Rotation(axis,distance/effectiveRadius);
            var rs=MidMatrixMath.Multiply(rotation,MidMatrixMath.Scale(Vector3.one));
            float cy=float.NegativeInfinity;
            for(int i=0;i<rest.Length;i++){var v=MidMatrixMath.Direction(rs,rest[i]);rotated[i]=v;cy=Mathf.Max(cy,MidRampSurface.Height(x+v.x,MidRampSurface.CenterZ+v.z)-v.y);}
            rockCenter=new Vector3(x,cy,MidRampSurface.CenterZ);
            var matrix=MidMatrixMath.Multiply(MidMatrixMath.Translation(rockCenter),rs);groundGap=float.PositiveInfinity;
            for(int i=0;i<rest.Length;i++){world[i]=MidMatrixMath.Point(matrix,rest[i]);outNormals[i]=MidMatrixMath.Direction(rotation,normals[i]).normalized;groundGap=Mathf.Min(groundGap,world[i].y-MidRampSurface.Height(world[i].x,world[i].z));}
            animatedMesh.vertices=world;animatedMesh.normals=outNormals;animatedMesh.RecalculateBounds();
            // 짧은 팔의 접촉 공간을 확보하도록 몸을 기울인다. 승인된 메시 자체는 변형하지 않는다.
            float effort=phase>=5&&phase<6?Mathf.Pow(Mathf.Sin((phase-5)*Mathf.PI),2):0;
            float lean=pushLean+effort;
            var poseUp=(Vector3.up*Mathf.Cos(lean*Mathf.Deg2Rad)-facing*Mathf.Sin(lean*Mathf.Deg2Rad)).normalized;
            chiikawa.rotation=Quaternion.LookRotation(-Vector3.ProjectOnPlane(facing,poseUp).normalized,poseUp);
            float lo=.25f,hi=1.15f;
            for(int j=0;j<10;j++)
            {
                float d=(lo+hi)*.5f;PlaceRoot(d,0);
                float gap=0;for(int i=0;i<2;i++){var sh=Shoulder(i);gap=Mathf.Max(gap,(ClosestVertex(sh)-sh).magnitude);}
                if(gap>.178f)hi=d;else lo=d;
            }
            float contactDistance=(lo+hi)*.5f;
            float away=phase<6?0:phase<6.35f?Ease((phase-6)/.35f):phase<10?1:1-Ease((phase-10)/1.2f);
            lean=Mathf.Lerp(lean,18f,away);
            poseUp=(Vector3.up*Mathf.Cos(lean*Mathf.Deg2Rad)-facing*Mathf.Sin(lean*Mathf.Deg2Rad)).normalized;
            chiikawa.rotation=Quaternion.LookRotation(-Vector3.ProjectOnPlane(facing,poseUp).normalized,poseUp);
            PlaceRoot(contactDistance,.38f*away);
            float contact=1-away;handGap=0;
            head.localRotation=headRot*Quaternion.AngleAxis(10f*contact,Vector3.right);
            head.localPosition=headPos+Vector3.forward*(.14f*contact);
            for(int i=0;i<2;i++)
            {
                var sh=Shoulder(i);var target=ClosestSurface(sh);var dir=(target-sh).normalized;
                var pushRot=Quaternion.FromToRotation(Vector3.down,dir);
                var pushPos=target-pushRot*new Vector3(0,-.09f,0); // 손끝을 표면에 고정하며 팔 길이는 바꾸지 않는다.
                var neutralPos=chiikawa.TransformPoint(armPos[i]);var neutralRot=chiikawa.rotation*armRot[i];
                arms[i].SetPositionAndRotation(Vector3.Lerp(neutralPos,pushPos,contact),Quaternion.Slerp(neutralRot,pushRot,contact));
                if(contact>.999f)handGap=Mathf.Max(handGap,Vector3.Distance(arms[i].TransformPoint(new Vector3(0,-.09f,0)),target));
            }
            MoveFeet(elapsed,resetFeet,phase>=8.5f&&phase<=9.8f||phase>=11.25f);
        }
        void PlaceRoot(float distance,float away)
        {
            var p=rockCenter-facing*(distance+away);p.y=MidRampSurface.Height(p.x,p.z)+.013f;
            // 걷기 중 미세한 체중 이동만 추가하고 얼굴/몸의 비율은 보존한다.
            chiikawa.position=p;
        }
        Vector3 Shoulder(int i)=>chiikawa.TransformPoint(new Vector3(i==0?-.18f:.18f,.43f,-.14f));
        void MoveFeet(float time,bool reset,bool settle)
        {
            var desired=new Vector3[2];
            for(int i=0;i<2;i++){desired[i]=chiikawa.TransformPoint(legPos[i]);desired[i].y=MidRampSurface.Height(desired[i].x,desired[i].z)+.082f;}
            float dt=time-lastFootTime;
            Vector3 velocity=dt>0&&!reset?(chiikawa.position-lastRoot)/dt:Vector3.zero;velocity.y=0;
            float speed=velocity.magnitude;
            if(reset){for(int i=0;i<2;i++)planted[i]=desired[i];stepping=-1;nextFoot=0;}
            // 빠르게 물러날 때 발도 짧고 빠른 걸음으로 따라간다. 예측 착지점으로 뒤처짐을 줄인다.
            if(stepping<0)
            {
                int i=nextFoot;if(FlatDistance(planted[i],desired[i])<.025f)i=1-i;
                if(FlatDistance(planted[i],desired[i])>.025f||(settle&&FlatDistance(planted[i],desired[i])>.002f))
                {
                    stepping=i;stepStarted=time;stepDuration=Mathf.Clamp(.045f/(speed+.08f),.03f,.16f);
                    stepFrom[i]=planted[i];stepTo[i]=desired[i]+Vector3.ClampMagnitude(velocity*(stepDuration+.02f),.055f);
                    nextFoot=1-i;
                }
            }
            for(int i=0;i<2;i++)
            {
                Vector3 p=planted[i];float lift=0;
                if(i==stepping)
                {
                    float t=Mathf.Clamp01((time-stepStarted)/stepDuration);p=Vector3.Lerp(stepFrom[i],stepTo[i],Ease(t));
                    lift=.018f*Mathf.Sin(Mathf.PI*t);
                    if(t>=1){planted[i]=stepTo[i];stepping=-1;}
                }
                // 몸통과 분리될 수 없도록 발의 수평 가동 범위를 제한한다. 메시의 길이/굵기는 그대로 둔다.
                Vector3 offset=p-desired[i];offset.y=0;
                if(offset.magnitude>.055f){p=desired[i]+offset.normalized*.055f;if(i!=stepping)planted[i]=p;}
                p.y=MidRampSurface.Height(p.x,p.z)+.082f+lift;
                legs[i].SetPositionAndRotation(p,Quaternion.identity);
            }
            lastRoot=chiikawa.position;lastFootTime=time;
        }
        static float FlatDistance(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        Vector3 ClosestVertex(Vector3 p){float best=float.PositiveInfinity;var q=p;foreach(var v in world){float d=(v-p).sqrMagnitude;if(d<best){best=d;q=v;}}return q;}
        Vector3 ClosestSurface(Vector3 p)
        {
            float best=float.PositiveInfinity;var result=p;
            for(int k=0;k<triangles.Length;k+=3){var q=ClosestTriangle(p,world[triangles[k]],world[triangles[k+1]],world[triangles[k+2]]);float d=(q-p).sqrMagnitude;if(d<best){best=d;result=q;}}
            return result;
        }
        static Vector3 ClosestTriangle(Vector3 p,Vector3 a,Vector3 b,Vector3 c)
        {
            var ab=b-a;var ac=c-a;var ap=p-a;float d1=Vector3.Dot(ab,ap),d2=Vector3.Dot(ac,ap);if(d1<=0&&d2<=0)return a;
            var bp=p-b;float d3=Vector3.Dot(ab,bp),d4=Vector3.Dot(ac,bp);if(d3>=0&&d4<=d3)return b;
            float vc=d1*d4-d3*d2;if(vc<=0&&d1>=0&&d3<=0)return a+d1/(d1-d3)*ab;
            var cp=p-c;float d5=Vector3.Dot(ab,cp),d6=Vector3.Dot(ac,cp);if(d6>=0&&d5<=d6)return c;
            float vb=d5*d2-d1*d6;if(vb<=0&&d2>=0&&d6<=0)return a+d2/(d2-d6)*ac;
            float va=d3*d6-d5*d4;if(va<=0&&(d4-d3)>=0&&(d5-d6)>=0)return b+(d4-d3)/((d4-d3)+(d5-d6))*(c-b);
            float denom=1/(va+vb+vc);return a+ab*(vb*denom)+ac*(vc*denom);
        }
        void OnDisable()
        {
            if(!ready)return;
            // Play 종료 시 오브젝트 해제 순서가 달라도 남아 있는 부위만 복원한다.
            if(rock)
            {
                rock.sharedMesh=originalMesh;rock.transform.localPosition=rockPos;rock.transform.localRotation=rockRot;rock.transform.localScale=rockScale;
                var collider=rock.GetComponent<Collider>();if(collider)collider.enabled=colliderWasEnabled;
            }
            if(chiikawa)chiikawa.SetPositionAndRotation(chiPos,chiRot);
            if(head){head.localRotation=headRot;head.localPosition=headPos;}
            for(int i=0;i<2;i++)
            {
                if(arms[i]){arms[i].localPosition=armPos[i];arms[i].localRotation=armRot[i];}
                if(legs[i]){legs[i].localPosition=legPos[i];legs[i].localRotation=legRot[i];}
            }
            if(animatedMesh)Destroy(animatedMesh);ready=false;
        }
    }
}

