using UnityEngine;
namespace MidProject.Animation
{
    /// <summary>열벡터 방식. p' = T R S p (크기→회전→이동). 행렬의 각 열은 변환된 기저/원점이다.</summary>
    public static class MidMatrixMath
    {
        public static Matrix4x4 Translation(Vector3 t){var m=Matrix4x4.identity;m.m03=t.x;m.m13=t.y;m.m23=t.z;return m;}
        public static Matrix4x4 Scale(Vector3 s){var m=Matrix4x4.identity;m.m00=s.x;m.m11=s.y;m.m22=s.z;return m;}
        public static Matrix4x4 Rotation(Vector3 axis,float radians)
        {
            var a=axis.normalized;float c=Mathf.Cos(radians),s=Mathf.Sin(radians),v=1-c;var m=Matrix4x4.identity;
            m.m00=c+a.x*a.x*v;m.m01=a.x*a.y*v-a.z*s;m.m02=a.x*a.z*v+a.y*s;
            m.m10=a.y*a.x*v+a.z*s;m.m11=c+a.y*a.y*v;m.m12=a.y*a.z*v-a.x*s;
            m.m20=a.z*a.x*v-a.y*s;m.m21=a.z*a.y*v+a.x*s;m.m22=c+a.z*a.z*v;return m;
        }
        public static Matrix4x4 Multiply(Matrix4x4 a,Matrix4x4 b)
        {var c=new Matrix4x4();for(int col=0;col<4;col++)for(int row=0;row<4;row++)for(int k=0;k<4;k++)c[row,col]+=a[row,k]*b[k,col];return c;}
        public static Vector3 Point(Matrix4x4 m,Vector3 p)=>new Vector3(m.m00*p.x+m.m01*p.y+m.m02*p.z+m.m03,m.m10*p.x+m.m11*p.y+m.m12*p.z+m.m13,m.m20*p.x+m.m21*p.y+m.m22*p.z+m.m23);
        public static Vector3 Direction(Matrix4x4 m,Vector3 p)=>new Vector3(m.m00*p.x+m.m01*p.y+m.m02*p.z,m.m10*p.x+m.m11*p.y+m.m12*p.z,m.m20*p.x+m.m21*p.y+m.m22*p.z);
    }
}
