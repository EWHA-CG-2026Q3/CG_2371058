using UnityEngine;

namespace MidProject.Animation
{
    /// <summary>기존 밀기 주기의 휴식 구간에만 더하는 작은 인사와 시선 반응.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class MidFriendGreeting : MonoBehaviour
    {
        public MidPushCycle pushCycle;
        public Transform hachiware;
        public Transform chiikawa;
        [Range(0,25)] public float hachiwareHeadTurn=12f;
        [Range(0,35)] public float chiikawaHeadTurnLimit=24f;
        [Range(0,100)] public float armLift=45f;
        [Range(0,20)] public float waveAngle=16f;
        // 기존 팔 메시의 위쪽 끝을 어깨 Pivot으로 사용한다. 계층과 메시를 바꾸지 않는다.
        public Vector3 shoulderInArm=new Vector3(0,.09f,0);
        Transform hHead,arm,cHead;
        Quaternion hHeadRest,armRest,cHeadRest;
        Vector3 armPosition,shoulder,scaledShoulder;
        bool ready;
        public float GreetingWeight { get; private set; }
        public float ReactionWeight { get; private set; }
        public float ShoulderGap=>ready&&arm?Vector3.Distance(arm.localPosition+arm.localRotation*scaledShoulder,shoulder):0;

        void OnEnable()
        {
            if(!pushCycle||!hachiware||!chiikawa){Debug.LogError("인사 동작의 캐릭터 또는 밀기 주기 참조가 없습니다.",this);enabled=false;return;}
            // 생성 당시 이름은 정면 화면 기준이다. -Z를 바라보는 캐릭터의 왼팔은 +X의 RightArm이다.
            hHead=hachiware.Find("Head");arm=hachiware.Find("RightArm");cHead=chiikawa.Find("Head");
            if(!hHead||!arm||!cHead){Debug.LogError("인사 동작의 머리/왼팔 부위를 찾을 수 없습니다.",this);enabled=false;return;}
            hHeadRest=hHead.localRotation;armRest=arm.localRotation;armPosition=arm.localPosition;cHeadRest=cHead.localRotation;
            scaledShoulder=Vector3.Scale(shoulderInArm,arm.localScale);
            shoulder=armPosition+armRest*scaledShoulder;ready=true;
        }
        static float Smooth(float t){t=Mathf.Clamp01(t);return t*t*t*(10f+t*(-15f+6f*t));}
        static float Window(float t,float start,float up,float down,float end)
        {return Smooth((t-start)/(up-start))*(1f-Smooth((t-down)/(end-down)));}
        void LateUpdate()
        {
            if(!ready||!pushCycle||!pushCycle.isActiveAndEnabled)return;
            float t=pushCycle.phase;
            float look=Window(t,8f,8.55f,10.45f,11.15f);
            GreetingWeight=Window(t,8.25f,8.85f,10.25f,10.95f);
            float wave=0f;
            if(t>=8.85f&&t<=10.25f)
            {
                float u=(t-8.85f)/1.4f;
                // 두 번 흔들며 시작/끝의 속도도 0으로 이어 준다.
                wave=waveAngle*Mathf.Sin(4f*Mathf.PI*u)*Mathf.Pow(Mathf.Sin(Mathf.PI*u),2);
            }
            hHead.localRotation=hHeadRest*Quaternion.AngleAxis(hachiwareHeadTurn*look,Vector3.up);
            arm.localRotation=Quaternion.AngleAxis(25f*GreetingWeight,Vector3.up)*Quaternion.AngleAxis(armLift*GreetingWeight-wave,Vector3.forward)*armRest;
            arm.localPosition=shoulder-arm.localRotation*scaledShoulder;

            ReactionWeight=Window(t,8.65f,9.25f,10.3f,11.1f);
            if(ReactionWeight>0)
            {
                // MidPushCycle.Update가 이번 프레임에 계산한 자세 위에만 더한다. 이전 프레임 회전을 누적하지 않는다.
                var direction=chiikawa.InverseTransformDirection(hachiware.position-chiikawa.position);direction.y=0;
                float turn=Vector3.SignedAngle(Vector3.back,direction,Vector3.up);
                turn=Mathf.Clamp(turn,-chiikawaHeadTurnLimit,chiikawaHeadTurnLimit);
                cHead.localRotation=cHead.localRotation*Quaternion.AngleAxis(turn*ReactionWeight,Vector3.up);
            }
        }
        void OnDisable()
        {
            if(!ready)return;
            if(hHead)hHead.localRotation=hHeadRest;
            if(arm){arm.localPosition=armPosition;arm.localRotation=armRest;}
            if(cHead)cHead.localRotation=cHeadRest;
            GreetingWeight=ReactionWeight=0;ready=false;
        }
    }
}
