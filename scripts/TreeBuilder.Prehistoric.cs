using System.Collections.Generic;
using Godot;

namespace JurassicWorld;

public static partial class TreeBuilder
{
    private static Shape BuildFern(RandomNumberGenerator rng,Material material,bool cycad)
    {
        var mesh=new MeshBuilder(); var branches=new List<Branch>();
        float height=cycad?rng.RandfRange(1.4f,2.5f):rng.RandfRange(4.5f,7.5f);
        float radius=height*(cycad?0.12f:0.045f);
        BuildTrunk(mesh,rng,height,radius,10,_=>new Color(0.26f,0.2f,0.11f));
        int fronds=cycad?15:11;
        for(int f=0;f<fronds;f++)
        {
            float angle=f*Mathf.Tau/fronds+rng.RandfRange(-0.12f,0.12f);
            var outward=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
            var across=outward.Cross(Vector3.Up);
            float length=height*(cycad?1.6f:0.75f)*rng.RandfRange(0.85f,1.1f);
            var root=Vector3.Up*height;
            var end=root+outward*length+Vector3.Up*(length*0.08f);
            BuildLimb(mesh,branches,rng,root,end,0.12f,radius*0.24f,new Color(0.23f,0.35f,0.12f),0.8f);
            for(int leaf=0;leaf<18;leaf++)
            {
                float t=(leaf+1)/20f;
                var mid=root+outward*length*t+Vector3.Up*(Mathf.Sin(t*Mathf.Pi)*length*0.2f-t*t*length*0.08f);
                float wide=length*0.18f*Mathf.Sin(t*Mathf.Pi);
                for(int side=-1;side<=1;side+=2)
                {
                    var tip=mid+across*(side*wide)+outward*length*0.08f;
                    var colour=new Color(0.14f,0.37f+t*0.12f,0.13f).Lightened(rng.RandfRange(0,0.08f));
                    int a=mesh.Add(mid,Vector3.Up,colour),b=mesh.Add(tip,Vector3.Up,colour.Lightened(0.08f)),c=mesh.Add(mid+outward*length*0.065f,Vector3.Up,colour);
                    mesh.Tri(a,b,c); mesh.Tri(c,b,a);
                }
            }
        }
        return new Shape(mesh.Commit(material),radius,height,height+0.1f,branches);
    }
}
