using System;
using System.Collections.Generic;
using Godot;

namespace JurassicWorld;

/// <summary>Articulated 3D dinosaur with species-specific silhouette, scales, jaws, claws and weapons.</summary>
public abstract partial class Dinosaur : Animal
{
    public abstract DinosaurKind Kind { get; }
    public override AnimalStats Stats => _stats ??= DinosaurCatalog.Stats(Kind);
    private AnimalStats? _stats;
    public override string DisplayName => Kind == DinosaurKind.Tyrannosaurus ? "T. rex" : Kind.ToString();
    public override string YoungName => DisplayName + " hatchling";
    public RaptorColour Colour { get; private set; }
    private Node3D _frame = null!, _body = null!, _neck = null!, _head = null!, _jaw = null!, _tail = null!;
    private readonly List<Node3D> _legs = [], _shins = [], _arms = [];
    private ShaderMaterial _skin = null!;
    private StandardMaterial3D _stripe = null!;
    private static readonly Dictionary<(DinosaurKind Kind,string Part,Vector3 Radii),ArrayMesh> PartMeshes = [];
    private float _clock, _cycle;
    private bool FourLegs => Kind is DinosaurKind.Ankylosaurus or DinosaurKind.Stegosaurus or DinosaurKind.Triceratops;
    private float H => Stats.BodyHeight;

    public virtual void SetColour(RaptorColour colour)
    {
        Colour = colour;
        if (_skin is null || Kind != DinosaurKind.Velociraptor) return;
        Color hide = colour switch
        {
            RaptorColour.Blue => new Color(0.2f, 0.26f, 0.27f),
            RaptorColour.Echo => new Color(0.35f, 0.25f, 0.15f),
            RaptorColour.Charlie => new Color(0.21f, 0.29f, 0.12f),
            _ => new Color(0.14f, 0.27f, 0.23f),
        };
        _skin.SetShaderParameter("hide_colour", hide);
        _skin.SetShaderParameter("raptor_variant", (int)colour);
        _stripe.AlbedoColor = hide.Darkened(0.3f);
        foreach (var node in FindChildren("EchoScar*", "MeshInstance3D", true, false)) ((Node3D)node).Visible = colour == RaptorColour.Echo;
    }

    public override void _Ready()
    {
        Color colour = Kind switch
        {
            DinosaurKind.Tyrannosaurus => new Color(0.36f, 0.32f, 0.21f),
            DinosaurKind.Ankylosaurus => new Color(0.34f, 0.4f, 0.24f),
            DinosaurKind.Stegosaurus => new Color(0.33f, 0.46f, 0.32f),
            DinosaurKind.Triceratops => new Color(0.47f, 0.34f, 0.23f),
            DinosaurKind.Gallimimus => new Color(0.65f, 0.47f, 0.25f),
            DinosaurKind.Pachycephalosaurus => new Color(0.42f, 0.49f, 0.27f),
            _ => new Color(0.3f, 0.37f, 0.38f),
        };
        _skin = DinosaurMaterials.Skin(colour, H, Kind == DinosaurKind.Velociraptor);
        _stripe = new StandardMaterial3D { AlbedoColor = colour.Darkened(0.45f), Roughness = 0.95f };
        var belly = new StandardMaterial3D { AlbedoColor = colour.Lightened(0.3f), Roughness = 1f };
        var horn = new StandardMaterial3D { AlbedoColor = new Color(0.8f, 0.76f, 0.59f), Roughness = 0.7f };
        var claw = new StandardMaterial3D { AlbedoColor = new Color(0.09f, 0.08f, 0.065f), Roughness = 0.6f };
        _frame = Pivot(this, "Frame", Vector3.Zero);
        float hip = FourLegs ? H * 0.43f : H * 0.52f;
        _body = Pivot(_frame, "Body", new Vector3(0, hip, 0));
        var bulk = new Vector3(Stats.BodyRadius * (FourLegs ? 1.12f : 0.85f), H * (FourLegs ? 0.25f : 0.2f), H * 0.48f);
        Blob(_body, "Torso", Vector3.Zero, bulk, _skin);
        // Low relief scutes provide an actual 3D scaled hide, without thousands of separate nodes.
        var rng = new RandomNumberGenerator { Seed = (ulong)Kind + 89 };
        var scales = new MeshBuilder();
        for (int i = 0; i < 220; i++)
        {
            float a = rng.Randf() * Mathf.Tau, b = rng.RandfRange(-0.8f, 0.8f);
            var n = new Vector3(Mathf.Cos(a) * Mathf.Sqrt(1-b*b), b, Mathf.Sin(a) * Mathf.Sqrt(1-b*b));
            var p = n * bulk * 1.015f;
            float r = H * 0.009f;
            var across = n.Cross(Vector3.Up).Normalized() * r;
            var up = across.Cross(n).Normalized() * r;
            int v = scales.Add(p + n * r * 0.3f, n, Colors.White);
            int l = scales.Add(p - across, n, Colors.White), t = scales.Add(p + up, n, Colors.White);
            int rr = scales.Add(p + across, n, Colors.White), d = scales.Add(p - up, n, Colors.White);
            scales.Tri(v,l,t); scales.Tri(v,t,rr); scales.Tri(v,rr,d); scales.Tri(v,d,l);
        }
        Attach(_body, "Scutes", scales.Commit(_stripe));
        _neck = Pivot(_body, "Neck", new Vector3(0, FourLegs ? 0 : H * 0.18f, -H * 0.37f));
        var headAt = new Vector3(0, FourLegs ? -H * 0.03f : Kind == DinosaurKind.Gallimimus ? H * 0.3f : H * 0.1f, -H * (Kind == DinosaurKind.Velociraptor ? 0.29f : 0.22f));
        Attach(_neck, "NeckMesh", Tube([Vector3.Zero,headAt*0.35f+new Vector3(0,H*0.035f,H*0.025f),headAt*0.72f,headAt], [H*0.13f,H*0.12f,H*0.095f,H*0.085f], 18, _skin, true));
        _head = Pivot(_neck, "Head", headAt);
        float skull = Kind == DinosaurKind.Tyrannosaurus ? 1.6f : Kind == DinosaurKind.Gallimimus ? 0.68f : Kind == DinosaurKind.Velociraptor ? 1.15f : 1f;
        Blob(_head, "Skull", new Vector3(0, H*0.015f, -H*0.07f), new Vector3(H*0.1f,H*0.09f,H*0.2f)*skull, _skin);
        Blob(_head, "Muzzle", new Vector3(0,-H*0.015f,-H*0.21f), new Vector3(H*0.09f,H*0.06f,H*0.13f)*skull,_skin);
        _jaw = Pivot(_head,"Jaw",new Vector3(0,-H*0.065f,0));
        Blob(_jaw,"LowerJaw",new Vector3(0,0,-H*0.16f),new Vector3(H*0.075f,H*0.025f,H*0.17f)*skull,belly);
        for (int side=-1; side<=1; side+=2)
        {
            Eye(_head,new Vector3(side*H*0.102f*skull,H*0.065f,-H*0.09f),new Vector3(0,-side*Mathf.Pi/2f,0),H*0.024f,new Color(0.96f,0.65f,0.12f),0.4f);
            Blob(_head,"Nostril",new Vector3(side*H*0.078f*skull,H*0.005f,-H*0.295f),new Vector3(H*0.012f,H*0.008f,H*0.015f),claw);
            if (!Stats.CanGraze)
                for (int i=0;i<9;i++) Spike(_head,new Vector3(side*H*0.073f*skull,-H*0.045f,-H*(0.06f+i*0.027f)),Vector3.Down*H*0.038f,H*0.009f,horn);
        }
        _tail = Pivot(_body,"Tail",new Vector3(0,H*0.04f,H*0.38f));
        Attach(_tail,"TailMesh",Tube([Vector3.Zero,new Vector3(0,0,H*0.42f),new Vector3(0,H*0.04f,H*0.88f),new Vector3(0,H*0.12f,H*1.22f)],[H*0.14f,H*0.105f,H*0.05f,H*0.009f],12,_skin,true));
        for (int i=0;i<(FourLegs?4:2);i++)
        {
            float side=i%2==0?-1:1, z=FourLegs?(i<2?-H*0.3f:H*0.3f):H*0.12f;
            var leg=Pivot(_body,"Leg"+i,new Vector3(side*bulk.X*0.7f,0,z)); _legs.Add(leg);
            Blob(leg,"Thigh",new Vector3(0,-hip*0.22f,0),new Vector3(H*0.11f,hip*0.34f,H*0.14f),_skin);
            var shin=Pivot(leg,"Lower"+i,new Vector3(0,-hip*0.48f,-H*0.05f)); _shins.Add(shin);
            Attach(shin,"ShinMesh",Tube([Vector3.Zero,new Vector3(0,-hip*0.45f,H*0.07f)],[H*0.065f,H*0.035f],10,_skin,true));
            var foot=Pivot(shin,"Foot"+i,new Vector3(0,-hip*0.45f,H*0.07f));
            for(int toe=-1;toe<=1;toe++)
            {
                Blob(foot,"Toe",new Vector3(toe*H*0.04f,0,-H*0.075f),new Vector3(H*0.026f,H*0.027f,H*0.12f),_skin);
            }
        }
        if(!FourLegs)
            for(int side=-1;side<=1;side+=2)
            {
                float arm=Kind==DinosaurKind.Tyrannosaurus?0.55f:1f;
                var joint=Pivot(_body,"Arm",new Vector3(side*bulk.X*0.8f,H*0.04f,-H*0.27f)); _arms.Add(joint);
                Attach(joint,"ArmMesh",Tube([Vector3.Zero,new Vector3(side*H*0.06f,-H*0.12f*arm,-H*0.02f),new Vector3(side*H*0.04f,-H*0.15f*arm,-H*0.16f*arm)],[H*0.04f,H*0.028f,H*0.02f],10,_skin,true));
                for(int finger=0;finger<(Kind==DinosaurKind.Tyrannosaurus?2:3);finger++) CurvedClaw(joint,new Vector3(side*H*(0.02f+finger*0.022f),-H*0.15f*arm,-H*0.16f*arm),H*0.075f,H*0.012f,claw);
            }
        if(Kind==DinosaurKind.Ankylosaurus)
        {
            for(int row=-2;row<=2;row++) for(int i=0;i<7;i++)
                Blob(_body,"Armour",new Vector3(row*bulk.X*0.36f,bulk.Y*(1-Mathf.Abs(row)*0.12f),H*(-0.35f+i*0.115f)),new Vector3(H*0.12f,H*0.08f,H*0.09f),_stripe);
            Blob(_tail,"TailClub",new Vector3(0,H*0.11f,H*1.15f),new Vector3(H*0.23f,H*0.12f,H*0.18f),horn);
            for(int side=-1;side<=1;side+=2) for(int i=0;i<7;i++) Spike(_body,new Vector3(side*bulk.X,0,H*(-0.35f+i*0.12f)),new Vector3(side*H*0.18f,H*0.1f,0),H*0.055f,horn);
        }
        if(Kind==DinosaurKind.Stegosaurus)
        {
            var plateMat=new StandardMaterial3D { AlbedoColor=new Color(0.58f,0.27f,0.14f),Roughness=0.9f,VertexColorUseAsAlbedo=true,VertexColorIsSrgb=true };
            for(int i=0;i<11;i++)
            {
                float z=H*(-0.5f+i*0.12f), size=H*(0.14f+0.2f*Mathf.Sin(i/10f*Mathf.Pi));
                Attach(_body,"DorsalPlate",PlateMesh(size,H*0.022f,plateMat),new Vector3(i%2==0?-H*0.06f:H*0.06f,bulk.Y*0.88f,z));
            }
            for(int side=-1;side<=1;side+=2) for(int i=0;i<2;i++) Spike(_tail,new Vector3(side*H*0.03f,H*0.07f,H*(0.9f+i*0.17f)),new Vector3(side*H*0.25f,H*0.12f,H*0.08f),H*0.045f,horn);
        }
        if(Kind==DinosaurKind.Triceratops)
        {
            Blob(_head,"Frill",new Vector3(0,H*0.12f,H*0.05f),new Vector3(H*0.28f,H*0.29f,H*0.045f),_stripe);
            for(int side=-1;side<=1;side+=2) Spike(_head,new Vector3(side*H*0.095f,H*0.1f,-H*0.1f),new Vector3(side*H*0.035f,H*0.16f,-H*0.32f),H*0.045f,horn);
            Spike(_head,new Vector3(0,H*0.03f,-H*0.28f),new Vector3(0,H*0.1f,-H*0.07f),H*0.03f,horn);
        }
        if(Kind==DinosaurKind.Pachycephalosaurus)
            Blob(_head,"BoneDome",new Vector3(0,H*0.12f,-H*0.05f),new Vector3(H*0.13f,H*0.12f,H*0.15f),horn);
        AddAnatomicalDetails(bulk, hip, skull, belly, horn, claw);
        // The skin shader paints flush flank markings; Echo's raised scar edges remain geometry.
        if(Kind==DinosaurKind.Velociraptor)
        {
            var scar=new StandardMaterial3D { AlbedoColor=new Color(0.68f,0.52f,0.37f),Roughness=1f };
            for(int line=0;line<3;line++) Attach(_head,"EchoScar"+line,Tube([new Vector3(-H*0.101f,H*0.05f,-H*(0.06f+line*0.025f)),new Vector3(-H*0.11f,-H*0.03f,-H*(0.1f+line*0.025f))],[H*0.006f,H*0.003f],6,scar,true));
        }
        SetColour(Colour);
        _skin.SetShaderParameter("world_to_animal",GlobalTransform.AffineInverse());
    }

    private void Blob(Node3D parent,string name,Vector3 at,Vector3 radii,Material mat)
    {
        Vector3 Shape(Vector3 u)
        {
            var p=u*radii;
            if(name=="Torso")
            {
                float waist=1f-0.14f*Mathf.Exp(-Mathf.Pow((u.Z-0.3f)*4f,2));
                p.X*=waist; p.Y*=waist;
                p.Y+=radii.Y*0.06f*(1-u.Z*u.Z);
            }
            if(name is "Skull" or "Muzzle" && Kind is DinosaurKind.Tyrannosaurus or DinosaurKind.Velociraptor or DinosaurKind.Dromaeosaurus)
            {
                p.X=Mathf.Sign(u.X)*Mathf.Pow(Mathf.Abs(u.X),0.72f)*radii.X;
                p.Y=Mathf.Sign(u.Y)*Mathf.Pow(Mathf.Abs(u.Y),0.82f)*radii.Y;
                p.X*=1-0.18f*Mathf.Max(0,-u.Z);
            }
            if(name=="Armour") p.Y+=radii.Y*0.45f*(1-u.X*u.X)*(1-u.Z*u.Z);
            return p;
        }
        var key=(Kind,name,radii);
        if(!PartMeshes.TryGetValue(key,out var mesh))
        {
            mesh=Ellipsoid(Shape,28,18,mat);
            mesh.SurfaceSetMaterial(0,null);
            PartMeshes[key]=mesh;
        }
        var instance=Attach(parent,name,mesh,at);
        instance.MaterialOverride=mat;
    }
    private static void Spike(Node3D parent,Vector3 root,Vector3 toward,float radius,Material mat) => Attach(parent,"Spike",Tube([root,root+toward*0.55f,root+toward],[radius,radius*0.5f,0.001f],8,mat,true));

    public override void Animate(float speed,float stride,float eat,float dt)
    {
        _skin.SetShaderParameter("world_to_animal",GlobalTransform.AffineInverse());
        _clock+=dt; _cycle+=speed*dt*(FourLegs?2.1f:3.3f)/Mathf.Sqrt(H);
        float rest=Mathf.Max(Sitting,Lying), strike=AttackTier<0?0:Mathf.Sin(_clock*(AttackTier==2?14:10));
        _frame.Position=new Vector3(0,-H*0.28f*rest+Mathf.Abs(Mathf.Sin(_cycle))*H*0.025f*stride,0);
        _frame.Rotation=new Vector3(Limp(0,0.1f),0,Limp(0,Mathf.Pi*0.47f));
        _body.Rotation=new Vector3(IsSwimming?-0.12f:0,0,Kind==DinosaurKind.Ankylosaurus&&AttackTier==2?strike*0.12f:0);
        _neck.Rotation=new Vector3(-eat*0.8f+(AttackTier>=0?strike*0.1f:0),0,0);
        _head.Rotation=new Vector3(-eat*0.3f,0,0);
        _jaw.Rotation=new Vector3(AttackTier>=0?-0.28f-Mathf.Abs(strike)*0.35f:0,0,0);
        _tail.Rotation=new Vector3(0,Mathf.Sin(_clock*1.8f)*0.06f*Alive+(AttackTier>=0&&FourLegs?strike*0.8f:0),0);
        for(int i=0;i<_legs.Count;i++)
        {
            float step=Mathf.Sin(_cycle+(i%2==0?0:Mathf.Pi)+(i>1?Mathf.Pi:0))*stride*(1-rest);
            _legs[i].Rotation=new Vector3(step*0.65f+rest*0.8f,0,0);
            _shins[i].Rotation=new Vector3(-Mathf.Max(0,step)*0.8f-rest*1.2f,0,0);
        }
        for(int i=0;i<_arms.Count;i++) _arms[i].Rotation=new Vector3(AttackTier>0?strike*0.7f:Mathf.Sin(_cycle+i*Mathf.Pi)*stride*0.1f,0,0);
        Chew(eat,dt);
    }
}
