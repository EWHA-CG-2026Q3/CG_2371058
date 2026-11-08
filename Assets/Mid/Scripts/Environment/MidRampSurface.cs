using UnityEngine;
namespace MidProject.Environment
{
    /// <summary>지형과 향후 행렬 이동이 공유하는 월드 좌표 높이. 정적인 표면 정의.</summary>
    public sealed class MidRampSurface : MonoBehaviour
    {
        public const float StartX=-4.30f, EndX=-1.00f, CenterZ=-1.40f;
        public const float Angle=18f, HalfWidth=.65f;
        public const float TravelStart=-3.20f, TravelEnd=-1.80f;
        public static float Slope=>Mathf.Tan(Angle*Mathf.Deg2Rad);
        public static float Height(float x,float z)
        {
            float h;
            if(x < -4.90f)return 0;
            if(x < -4.00f)
            {
                float t=Mathf.InverseLerp(-4.90f,-4.00f,x);
                float end=.02f+.30f*Slope;
                h=(-2*t*t*t+3*t*t)*end+(t*t*t-t*t)*.90f*Slope;
            }
            else if(x<=-1.35f)h=.02f+(x-StartX)*Slope;
            else if(x<=-.75f)
            {
                float d=x+1.35f;
                h=.02f+(-1.35f-StartX)*Slope+Slope*(d-d*d/1.20f);
            }
            else h=(.02f+3.25f*Slope)*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.75f,1.50f,x)));
            float dz=z-CenterZ;
            float outer=2.65f+.16f*Mathf.Sin(x*1.3f)+(dz>0?.15f:0);
            float shoulder=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(HalfWidth,outer,Mathf.Abs(dz)));
            return Mathf.Max(0,h)*shoulder;
        }
        public static Vector3 Normal(float x,float z)
        {const float d=.001f;return new Vector3(-(Height(x+d,z)-Height(x-d,z))/(2*d),1,-(Height(x,z+d)-Height(x,z-d))/(2*d)).normalized;}
        public static Vector3 RockCenter(Vector3[] restVertices,float x,float z,Quaternion rotation)
        {float y=float.NegativeInfinity;foreach(var p in restVertices){var q=rotation*p;y=Mathf.Max(y,Height(x+q.x,z+q.z)-q.y);}return new Vector3(x,y,z);}
    }
}
