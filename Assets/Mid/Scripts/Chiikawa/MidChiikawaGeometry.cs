using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MidProject.Characters
{
    /// <summary>치이카와의 곡면과 얼굴 표면을 직접 생성한다. 정면은 로컬 -Z이다.</summary>
    public static class MidChiikawaGeometry
    {
        public const float HeadBottom = 0.335f;
        public const float HeadEquator = 0.365f;
        public const float HeadUpperHeight = 0.417f;
        public const float HeadHalfWidth = 0.480f;
        public const float HeadHalfDepth = 0.420f;

        // 아래쪽 단면을 넓게 남겨 둥근 볼과 완만하고 넓은 턱을 만든다.
        static Vector3 HeadPoint(float latitude, float longitude)
        {
            float s=Mathf.Sin(latitude), c=Mathf.Max(0,Mathf.Cos(latitude));
            float y=HeadEquator+s*(s<0?HeadEquator:HeadUpperHeight);
            float a=HeadHalfWidth*Mathf.Pow(c,s<0?.65f:.90f);
            float b=HeadHalfDepth*Mathf.Pow(c,s<0?.68f:1.0f);
            float zsign=Mathf.Sin(longitude);
            float z=b*Mathf.Sign(zsign)*Mathf.Pow(Mathf.Abs(zsign),zsign<0?.90f:1f);
            return new Vector3(a*Mathf.Cos(longitude),y,z);
        }

        public static Mesh Head() => RoundedSurface("Chiikawa_Head",HeadPoint,128,80);

        public static Mesh Body() => RoundedSurface("Chiikawa_Body",(lat,lon)=>
        {
            float s=Mathf.Sin(lat),c=Mathf.Max(0,Mathf.Cos(lat));
            float width=.316f*Mathf.Pow(c,s<0?.44f:.90f)*(s<0?Mathf.Lerp(1,.92f,-s):1);
            float depth=.233f*Mathf.Pow(c,s<0?.60f:1);
            return new Vector3(width*Mathf.Cos(lon),.310f+s*(s<0?.225f:.190f),depth*Mathf.Sin(lon));
        },96,64);

        // 발 윗부분은 몸통 안에 묻히고, 바닥 쪽은 짧고 둥글게 마무리한다.
        public static Mesh Leg(string name) => RoundedSurface(name,(lat,lon)=>
        {
            float s=Mathf.Sin(lat), c=Mathf.Max(0,Mathf.Cos(lat));
            float r=Mathf.Pow(c,s<0?.60f:1f);
            return new Vector3(.05512f*r*Mathf.Cos(lon),.082f*s,.0689f*r*Mathf.Sin(lon));
        },64,48);

        public static Mesh Ellipsoid(string name, Vector3 radius, int columns=64,int rows=40)
            => RoundedSurface(name,(a,b)=>new Vector3(radius.x*Mathf.Cos(a)*Mathf.Cos(b),radius.y*Mathf.Sin(a),radius.z*Mathf.Cos(a)*Mathf.Sin(b)),columns,rows);

        // 길게 늘인 타원 대신 짧은 몸체와 둥근 반구 끝을 가진 팔을 만든다.
        public static Mesh RoundedArm(string name) => RoundedSurface(name,(lat,lon)=>
        {
            float y=lat/(Mathf.PI*.5f)*.090f;
            float cap=Mathf.Max(0,Mathf.Abs(y)-.040f)/.050f;
            float r=Mathf.Sqrt(Mathf.Max(0,1-cap*cap));
            return new Vector3(.05088f*r*Mathf.Cos(lon),y,.0636f*r*Mathf.Sin(lon));
        },64,64);

        static Mesh RoundedSurface(string name,Func<float,float,Vector3> sample,int cols,int rows)
        {
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();
            vertices.Add(sample(-Mathf.PI*.5f,0));uv.Add(new Vector2(.5f,0));
            for(int r=1;r<rows;r++)
            {
                float v=(float)r/rows;
                for(int c=0;c<=cols;c++)
                { vertices.Add(sample((v-.5f)*Mathf.PI,c==cols?0:2*Mathf.PI*c/cols));uv.Add(new Vector2((float)c/cols,v)); }
            }
            int top=vertices.Count;vertices.Add(sample(Mathf.PI*.5f,0));uv.Add(new Vector2(.5f,1));
            for(int c=0;c<cols;c++){indices.Add(0);indices.Add(1+c);indices.Add(2+c);}
            for(int r=0;r<rows-2;r++)for(int c=0;c<cols;c++)
            {
                int a=1+r*(cols+1)+c,b=a+cols+1;
                indices.Add(a);indices.Add(b);indices.Add(a+1);
                indices.Add(a+1);indices.Add(b);indices.Add(b+1);
            }
            int last=1+(rows-2)*(cols+1);
            for(int c=0;c<cols;c++){indices.Add(top);indices.Add(last+c+1);indices.Add(last+c);}
            Mesh mesh=Finish(name,vertices,indices,uv);
            // UV 절개선 양쪽의 법선을 평균하여 뒤쪽에 조명 이음새가 생기지 않게 한다.
            var n=mesh.normals;
            for(int r=0;r<rows-1;r++){int a=1+r*(cols+1),b=a+cols;Vector3 nn=(n[a]+n[b]).normalized;n[a]=n[b]=nn;}
            n[0]=Vector3.down;n[top]=Vector3.up;mesh.normals=n;
            return mesh;
        }

        /// <summary>머리 로컬 XY 위치에 대응하는 정면 곡면. 얼굴과 머리가 같은 수식을 공유한다.</summary>
        public static Vector3 FacePoint(float x,float localY,float offset=0)
        {
            float d=localY-HeadEquator;
            float s=Mathf.Clamp(d/(d<0?HeadEquator:HeadUpperHeight),-.99999f,.99999f);
            float c=Mathf.Sqrt(1-s*s);
            float a=HeadHalfWidth*Mathf.Pow(c,d<0?.65f:.90f);
            float b=HeadHalfDepth*Mathf.Pow(c,d<0?.68f:1f);
            float ratio=Mathf.Clamp(x/a,-.99999f,.99999f);
            return new Vector3(x,localY,-b*Mathf.Pow(1-ratio*ratio,.45f)-offset);
        }

        public static Mesh FaceEllipse(string name, Vector2 center, Vector2 radii,float offset, out Vector3 anchor)
        {
            anchor=FacePoint(center.x,center.y,offset);
            var v=new List<Vector3>{Vector3.zero};var uv=new List<Vector2>{new Vector2(.5f,.5f)};var tri=new List<int>();
            const int rings=12,segments=64;
            for(int r=1;r<=rings;r++)for(int c=0;c<segments;c++)
            {
                float a=2*Mathf.PI*c/segments,rr=(float)r/rings;
                var p=center+new Vector2(Mathf.Cos(a)*radii.x*rr,Mathf.Sin(a)*radii.y*rr);
                v.Add(FacePoint(p.x,p.y,offset)-anchor);uv.Add(new Vector2(.5f+.5f*rr*Mathf.Cos(a),.5f+.5f*rr*Mathf.Sin(a)));
            }
            for(int c=0;c<segments;c++){tri.Add(0);tri.Add(1+(c+1)%segments);tri.Add(1+c);}
            for(int r=0;r<rings-1;r++)for(int c=0;c<segments;c++)
            {
                int a=1+r*segments+c,b=1+r*segments+(c+1)%segments,cc=a+segments,d=b+segments;
                tri.Add(a);tri.Add(b);tri.Add(cc);tri.Add(b);tri.Add(d);tri.Add(cc);
            }
            return Finish(name,v,tri,uv);
        }

        public static Vector2[] Bezier(params Vector2[] controls)
        {
            if((controls.Length-1)%3!=0)throw new ArgumentException("Cubic curves require 3n+1 points");
            var points=new List<Vector2>();
            for(int seg=0;seg<(controls.Length-1)/3;seg++)for(int i=seg==0?0:1;i<=24;i++)
            {
                float t=i/24f,u=1-t;int k=seg*3;
                points.Add(u*u*u*controls[k]+3*u*u*t*controls[k+1]+3*u*t*t*controls[k+2]+t*t*t*controls[k+3]);
            }
            return points.ToArray();
        }

        public static Mesh FaceStroke(string name,Vector2[] points,float width,float offset,out Vector3 anchor)
        {
            anchor=FacePoint(points[0].x,points[0].y,offset);
            var verts=new List<Vector3>();var tri=new List<int>();var uv=new List<Vector2>();
            float radius=width*.5f;
            for(int i=0;i<points.Length;i++)
            {
                Vector2 tangent=(points[Mathf.Min(i+1,points.Length-1)]-points[Mathf.Max(i-1,0)]).normalized;
                Vector2 across=new Vector2(-tangent.y,tangent.x)*radius;
                foreach(var p in new[]{points[i]+across,points[i]-across}){verts.Add(FacePoint(p.x,p.y,offset)-anchor);uv.Add(Vector2.zero);}
                if(i>0){int a=2*(i-1);tri.Add(a);tri.Add(a+2);tri.Add(a+1);tri.Add(a+1);tri.Add(a+2);tri.Add(a+3);}
            }
            // 둥근 끝을 위한 작은 원판. 면 전체가 같은 곡면을 따른다.
            foreach(var p in new[]{points[0],points[points.Length-1]})
            {
                int c=verts.Count;verts.Add(FacePoint(p.x,p.y,offset)-anchor);uv.Add(Vector2.zero);
                const int seg=20;
                for(int j=0;j<seg;j++){float a=2*Mathf.PI*j/seg;var q=p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius;verts.Add(FacePoint(q.x,q.y,offset)-anchor);uv.Add(Vector2.zero);}
                for(int j=0;j<seg;j++){tri.Add(c);tri.Add(c+1+(j+1)%seg);tri.Add(c+1+j);}
            }
            // 리본의 XY 면적 부호를 검사해 모든 삼각형을 정면(-Z)으로 통일한다.
            for(int i=0;i<tri.Count;i+=3)
                if(Vector3.Cross(verts[tri[i+1]]-verts[tri[i]],verts[tri[i+2]]-verts[tri[i]]).z>0)
                {int swap=tri[i+1];tri[i+1]=tri[i+2];tri[i+2]=swap;}
            return Finish(name,verts,tri,uv);
        }

        static Mesh Finish(string name,List<Vector3> v,List<int> tri,List<Vector2> uv)
        {
            var m=new Mesh{name=name,indexFormat=v.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16};
            m.SetVertices(v);m.SetTriangles(tri,0);m.SetUVs(0,uv);m.RecalculateNormals();m.RecalculateBounds();return m;
        }
    }
}
