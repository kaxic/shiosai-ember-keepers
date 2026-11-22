using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// A stage or articulated limb is a single textured mesh. Dimensions are in Unity metres.
internal sealed class EmberMesh
{
    public enum Finish { Plain, Flagstone, Slate, Marble, Basalt, Engraved, Oak, Water }
    readonly List<Vector3> positions=new List<Vector3>(), normals=new List<Vector3>();
    readonly List<Vector4> uv=new List<Vector4>();
    readonly List<Color32> colors=new List<Color32>();
    readonly List<int> indices=new List<int>();
    internal static Vector3 At(float x,float y,float z)=>new Vector3(x,y,z);
    Color paint=Color.white;
    Finish finish;
    float glow;
    public Vector3 originOffset;
    public void Ink(string hex,Finish material=Finish.Plain,float emission=0)
    {
        ColorUtility.TryParseHtmlString("#"+hex,out paint);finish=material;glow=emission;
    }
    public void Tri(Vector3 a,Vector3 b,Vector3 c,Vector3 outside)
    {
        Vector3 n=Vector3.Cross(b-a,c-a);
        if(n.sqrMagnitude<.000000001f)return;
        if(Vector3.Dot(n,outside)<0){var swap=b;b=c;c=swap;n=-n;}
        n.Normalize();
        Vector3 axis=new Vector3(Mathf.Abs(n.x),Mathf.Abs(n.y),Mathf.Abs(n.z));
        void vertex(Vector3 p)
        {
            Vector2 tex=axis.y>=axis.x&&axis.y>=axis.z?new Vector2(p.x,p.z):
                axis.x>axis.z?new Vector2(p.z,p.y):new Vector2(p.x,p.y);
            uv.Add(new Vector4(tex.x,tex.y,(float)finish,glow));indices.Add(positions.Count);
            positions.Add(p+originOffset);normals.Add(n);colors.Add(paint);
        }
        vertex(a);vertex(b);vertex(c);
    }
    public void GradientTri(Vector3 a,Vector3 b,Vector3 c,string shadeA,string shadeB,string shadeC)
    {
        if(Vector3.Cross(b-a,c-a).sqrMagnitude<.000000001f)return;
        if(Vector3.Cross(b-a,c-a).y<0){var point=b;b=c;c=point;string swap=shadeB;shadeB=shadeC;shadeC=swap;}
        int start=colors.Count;Tri(a,b,c,Vector3.up);
        ColorUtility.TryParseHtmlString("#"+shadeA,out var ca);ColorUtility.TryParseHtmlString("#"+shadeB,out var cb);ColorUtility.TryParseHtmlString("#"+shadeC,out var cc);
        colors[start]=ca;colors[start+1]=cb;colors[start+2]=cc;
    }
    public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 normal)
    { Tri(a,b,c,normal);Tri(a,c,d,normal); }
    public void Box(Vector3 pos,Vector3 size,Quaternion rot)
    {
        Vector3 p(float x,float y,float z)=>pos+rot*Vector3.Scale(size,At(x,y,z))*.5f;
        void side(Vector3 a,Vector3 b,Vector3 c,Vector3 d)=>Quad(a,b,c,d,(a+b+c+d)*.25f-pos);
        side(p(-1,1,-1),p(-1,1,1),p(1,1,1),p(1,1,-1));
        side(p(-1,-1,-1),p(1,-1,-1),p(1,-1,1),p(-1,-1,1));
        side(p(-1,-1,-1),p(-1,1,-1),p(1,1,-1),p(1,-1,-1));
        side(p(1,-1,1),p(1,1,1),p(-1,1,1),p(-1,-1,1));
        side(p(-1,-1,1),p(-1,1,1),p(-1,1,-1),p(-1,-1,-1));
        side(p(1,-1,-1),p(1,1,-1),p(1,1,1),p(1,-1,1));
    }
    public void Block(float x,float y,float z,float w,float h,float d,float yaw=0)
    { Box(At(x,y,z),At(w,h,d),Quaternion.Euler(0,yaw,0)); }
    public void Beam(Vector3 a,Vector3 b,float w,float d)
    { Box((a+b)*.5f,At(w,Vector3.Distance(a,b),d),Quaternion.FromToRotation(Vector3.up,b-a)); }
    public void Tube(Vector3 a,Vector3 b,float bottom,float top,int sides=10)
    {
        Vector3 up=(b-a).normalized;
        Vector3 x=Vector3.Cross(up,Mathf.Abs(up.z)>.92f?Vector3.right:Vector3.forward).normalized;
        Vector3 z=Vector3.Cross(up,x);
        Vector3 ray(int j){float t=j*Mathf.PI*2/sides;return x*Mathf.Cos(t)+z*Mathf.Sin(t);}
        for(int i=0;i<sides;i++)
        {
            var r=ray(i);var s=ray(i+1);
            Quad(a+r*bottom,a+s*bottom,b+s*top,b+r*top,r+s);
            Tri(a,a+s*bottom,a+r*bottom,-up);Tri(b,b+r*top,b+s*top,up);
        }
    }
    public void Drum(float x,float y,float z,float r,float s,float h,int sides=10)
    {Tube(At(x,y,z),At(x,y+h,z),r,s,sides);}
    public void Jewel(Vector3 p,Vector3 radius,int sides=8,float twist=0)
    {
        Vector3 tip=p+Vector3.up*radius.y,foot=p-Vector3.up*radius.y;
        Vector3 ray(int i){float a=i*Mathf.PI*2/sides+twist;return At(radius.x*Mathf.Cos(a),0,radius.z*Mathf.Sin(a));}
        for(int i=0;i<sides;i++)
        {
            Vector3 l=ray(i),r=ray(i+1);
            var a=p+l;var b=p+r;var c=p+r*.72f+Vector3.up*radius.y*.48f;var d=p+l*.72f+Vector3.up*radius.y*.48f;
            Tri(foot,b,a,l+r);Quad(a,b,c,d,l+r);Tri(tip,d,c,l+r+Vector3.up);
        }
    }
    public void Ring(Vector3 p,float inner,float outer,int sides=36,float start=0,float end=360)
    {
        for(int i=0;i<sides;i++)
        {
            float a=Mathf.Lerp(start,end,(float)i/sides)*Mathf.Deg2Rad,b=Mathf.Lerp(start,end,(i+1f)/sides)*Mathf.Deg2Rad;
            Vector3 u=At(Mathf.Sin(a),0,Mathf.Cos(a)),v=At(Mathf.Sin(b),0,Mathf.Cos(b));
            Quad(p+u*inner,p+u*outer,p+v*outer,p+v*inner,Vector3.up);
        }
    }
    public void ArchStone(Vector3 pivot,float inner,float outer,float zDepth,float leftDegrees,float rightDegrees)
    {
        Vector3 p(float angle,float radius,float z)
        {float a=angle*Mathf.Deg2Rad;return pivot+At(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,z);}
        Vector3 a=p(leftDegrees,inner,-zDepth*.5f),b=p(leftDegrees,outer,-zDepth*.5f),c=p(rightDegrees,outer,-zDepth*.5f),d=p(rightDegrees,inner,-zDepth*.5f);
        Vector3 offset=Vector3.forward*zDepth;
        Quad(a,b,c,d,Vector3.back);Quad(a+offset,b+offset,c+offset,d+offset,Vector3.forward);
        Vector3 outwards=(b+c)*.5f-pivot;outwards.z=0;
        Quad(b,b+offset,c+offset,c,outwards);Quad(a,a+offset,d+offset,d,-outwards);
        Quad(a,a+offset,b+offset,b,Vector3.Cross(outwards,Vector3.forward));
        Quad(c,c+offset,d+offset,d,Vector3.Cross(Vector3.forward,outwards));
    }
    public Mesh Build(string label)
    {
        var result=new Mesh{name=label};if(positions.Count>65535)result.indexFormat=IndexFormat.UInt32;
        result.SetVertices(positions);result.SetNormals(normals);result.SetColors(colors);
        result.SetUVs(0,uv);result.SetTriangles(indices,0);result.RecalculateBounds();return result;
    }
}
