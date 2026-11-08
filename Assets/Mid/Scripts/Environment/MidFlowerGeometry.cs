using System;
using System.Collections.Generic;
using UnityEngine;

namespace MidProject.Environment
{
    /// <summary>돌과 별도로 꽃잎의 두께·굽힘, 꽃 중심, 줄기의 단면을 정의한다.</summary>
    public static class MidFlowerGeometry
    {
        static Mesh ClosedRings(string name, int rows, int sides, Func<float,float,Vector3> surface)
        {
            var v=new List<Vector3>{surface(0,0)};var uv=new List<Vector2>{Vector2.zero};var tr=new List<int>();
            for(int i=1;i<rows;i++)for(int j=0;j<sides;j++)
            {v.Add(surface((float)i/rows,2*Mathf.PI*j/sides));uv.Add(new Vector2((float)j/sides,(float)i/rows));}
            int top=v.Count;v.Add(surface(1,0));uv.Add(Vector2.one);
            for(int j=0;j<sides;j++)
            {
                int n=(j+1)%sides;tr.AddRange(new[]{0,1+j,1+n});
                for(int r=0;r<rows-2;r++)
                {int a=1+r*sides+j,b=1+r*sides+n,c=a+sides,d=b+sides;tr.AddRange(new[]{a,c,b,b,c,d});}
                tr.AddRange(new[]{top,1+(rows-2)*sides+n,1+(rows-2)*sides+j});
            }
            var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tr,0);
            mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }

        public static Mesh Petal() => ClosedRings("Mid_Flower_Petal",32,32,(t,a)=>
        {
            float round=(t<=0 || t>=1)?0:Mathf.Sqrt(Mathf.Max(0,Mathf.Sin(Mathf.PI*t)));
            return new Vector3(.056f*round*Mathf.Cos(a), .150f*t,
                -.020f*Mathf.Sin(Mathf.PI*t)+.012f*round*Mathf.Sin(a));
        });

        public static Mesh Center() => ClosedRings("Mid_Flower_Center",24,40,(t,a)=>
        {
            float lat=(t-.5f)*Mathf.PI;
            return new Vector3(.045f*Mathf.Cos(lat)*Mathf.Cos(a),.045f*Mathf.Sin(lat),.022f*Mathf.Cos(lat)*Mathf.Sin(a));
        });

        public static Mesh Stem() => ClosedRings("Mid_Flower_Stem",48,16,(t,a)=>
        {
            float radius=.009f*Mathf.Min(1,Mathf.Sqrt(Mathf.Max(0,Mathf.Sin(Mathf.PI*t)))*4);
            return new Vector3(.018f*Mathf.Sin(t*Mathf.PI*.5f)+radius*Mathf.Cos(a),
                .34f*t,radius*Mathf.Sin(a));
        });

        public static Mesh Leaf() => ClosedRings("Mid_Flower_Leaf",24,24,(t,a)=>
        {
            float r=Mathf.Sin(Mathf.PI*t);
            return new Vector3(.035f*r*Mathf.Cos(a),.13f*t,-.025f*r+.005f*r*Mathf.Sin(a));
        });
    }
}
