using System;
using Godot;

namespace JurassicWorld;

public static partial class LandmarkBuilder
{
    public static void DinosaurBones(Node3D parent,Vector2 centre,Vector2 facing,Func<float,float,float> ground,RandomNumberGenerator rng)
    {
        var bone=Plain(new Color(0.82f,0.76f,0.58f),0.95f);
        for(int skeleton=0;skeleton<5;skeleton++)
        {
            float size=skeleton==0?1.6f:rng.RandfRange(0.55f,1f);
            var at=centre+Vector2.Right.Rotated(skeleton*2.4f)*skeleton*13f;
            var root=new Node3D { Name="DinosaurSkeleton",Position=new Vector3(at.X,ground(at.X,at.Y)+0.2f,at.Y),Rotation=new Vector3(0,skeleton*1.8f,0) }; parent.AddChild(root);
            Solid(root,"Spine",Animal.Tube([new Vector3(0,0.7f,-4)*size,new Vector3(0,1.1f,0)*size,new Vector3(0,0.6f,5)*size,new Vector3(0,0.2f,11)*size],[0.25f*size,0.3f*size,0.18f*size,0.02f],10,bone,true),Transform3D.Identity);
            for(int rib=0;rib<9;rib++) for(int side=-1;side<=1;side+=2)
            {
                float z=-2.8f+rib*0.75f, width=Mathf.Sin((rib+1)/10f*Mathf.Pi)*2.8f;
                Solid(root,"Rib",Animal.Tube([new Vector3(0,0.9f,z)*size,new Vector3(side*width*0.7f,3.3f,z)*size,new Vector3(side*width,2.6f,z)*size,new Vector3(side*width*1.15f,0.2f,z)*size],[0.12f*size,0.1f*size,0.085f*size,0.05f*size],8,bone,true),Transform3D.Identity);
            }
            // Long theropod skull, open eye sockets, teeth and a separate lower jaw.
            Solid(root,"SkullRoof",Animal.Ellipsoid(new Vector3(0.7f,0.28f,1.3f)*size,20,12,bone),new Transform3D(Basis.Identity,new Vector3(0,1.1f,-5f)*size));
            for(int side=-1;side<=1;side+=2)
            {
                Solid(root,"Cheek",Animal.Tube([new Vector3(side*0.6f,1,-4.2f)*size,new Vector3(side*0.6f,0.5f,-4.3f)*size,new Vector3(side*0.45f,0.5f,-6f)*size],[0.12f*size,0.14f*size,0.08f*size],8,bone,true),Transform3D.Identity);
                for(int tooth=0;tooth<9;tooth++)
                    Solid(root,"Tooth",Animal.Tube([new Vector3(side*0.5f,0.95f,-4.1f-tooth*0.2f)*size,new Vector3(side*0.5f,0.67f,-4.1f-tooth*0.2f)*size],[0.055f*size,0.003f],6,bone,true),Transform3D.Identity);
                Solid(root,"HindLeg",Animal.Tube([new Vector3(side*0.7f,1,3)*size,new Vector3(side*2.7f,0.4f,4)*size,new Vector3(side*4.8f,0.15f,2.5f)*size],[0.23f*size,0.15f*size,0.08f*size],10,bone,true),Transform3D.Identity);
            }
        }
    }

    public static void FlowingFalls(Node3D parent,Vector3 toe,float top,Vector3 outward,Func<float,float,float> ground)
    {
        var across=outward.Cross(Vector3.Up).Normalized();
        var mesh=new MeshBuilder();
        for(int r=0;r<=20;r++)
        {
            float t=r/20f;
            for(int c=0;c<2;c++)
            {
                var at=toe+outward*(0.8f+t*t*3f)+across*((c==0?-1:1)*Mathf.Lerp(4f,7f,t));
                at.Y=Mathf.Lerp(top,toe.Y,t);
                mesh.Add(at,outward,Colors.White,new Vector2(c,t));
            }
            if(r>0) mesh.Quad((r-1)*2,(r-1)*2+1,r*2+1,r*2);
        }
        var water=new ShaderMaterial { Shader=GD.Load<Shader>("res://shaders/waterfall.gdshader") };
        parent.AddChild(new MeshInstance3D { Name="FlowingWater",Mesh=mesh.Commit(water) });
        var mist=new CpuParticles3D
        {
            Name="WaterfallMist",Amount=100,Lifetime=2.4f,Position=toe+outward*4f+Vector3.Up,
            EmissionShape=CpuParticles3D.EmissionShapeEnum.Box,EmissionBoxExtents=new Vector3(7,0.5f,2),
            Direction=Vector3.Up,Spread=75,InitialVelocityMin=0.8f,InitialVelocityMax=2f,
            Gravity=new Vector3(0,0.4f,0),ScaleAmountMin=0.2f,ScaleAmountMax=0.65f,
            Mesh=new SphereMesh { Radius=0.3f,Height=0.6f,RadialSegments=8,Rings=4,Material=new StandardMaterial3D { AlbedoColor=new Color(0.82f,0.95f,0.92f,0.12f),Transparency=BaseMaterial3D.TransparencyEnum.Alpha,ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded } },
        };
        parent.AddChild(mist);
    }
}
