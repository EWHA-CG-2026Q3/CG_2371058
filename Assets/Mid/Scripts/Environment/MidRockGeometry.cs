using System.Collections.Generic;
using UnityEngine;

namespace MidProject.Environment
{
    public static class MidRockGeometry
    {
        public const int Rings = 48, Segments = 80;

        // 저주파 방향 함수만 사용하므로 재생성해도 같은 형태이며 바늘 같은 잡음이 생기지 않는다.
        static Vector3 Shape(Vector3 n)
        {
            float r = 1 + .070f * Mathf.Sin(2.7f*n.x + .6f) * Mathf.Cos(2.2f*n.y - .3f)
                + .045f * Mathf.Sin(3.1f*n.z + 1.1f) + .025f * n.x*n.y;
            return new Vector3(n.x * .62f, n.y * .56f, n.z * .58f) * r;
        }

        public static Mesh Create()
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tr = new List<int>();
            v.Add(Shape(Vector3.down)); uv.Add(new Vector2(.5f, 0));
            // 원형 단면을 아래에서 위까지 반복 생성한다. 이음매는 동일 인덱스를 공유한다.
            for (int ring = 1; ring < Rings; ring++)
            {
                float a = -Mathf.PI/2 + Mathf.PI*ring/Rings;
                for (int j=0;j<Segments;j++)
                {
                    float b = 2*Mathf.PI*j/Segments;
                    v.Add(Shape(new Vector3(Mathf.Cos(a)*Mathf.Cos(b), Mathf.Sin(a), Mathf.Cos(a)*Mathf.Sin(b))));
                    uv.Add(new Vector2((float)j/Segments,(float)ring/Rings));
                }
            }
            int top=v.Count; v.Add(Shape(Vector3.up)); uv.Add(new Vector2(.5f,1));
            for(int j=0;j<Segments;j++)
            {
                int next=(j+1)%Segments;
                tr.AddRange(new[]{0,1+j,1+next});
                for(int r=0;r<Rings-2;r++)
                {
                    int a=1+r*Segments+j,b=1+r*Segments+next,c=a+Segments,d=b+Segments;
                    tr.AddRange(new[]{a,c,b,b,c,d});
                }
                tr.AddRange(new[]{top,1+(Rings-2)*Segments+next,1+(Rings-2)*Segments+j});
            }
            var bounds=new Bounds(v[0],Vector3.zero); foreach(var p in v)bounds.Encapsulate(p);
            float scale=1.20f/Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));
            for(int i=0;i<v.Count;i++)v[i]=(v[i]-bounds.center)*scale;
            var mesh=new Mesh{name="Mid_Rock_RingMesh"};
            mesh.SetVertices(v);mesh.SetTriangles(tr,0);mesh.SetUVs(0,uv);
            mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
    }
}
