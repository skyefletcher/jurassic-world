using Godot;

namespace JurassicWorld;

public abstract partial class Dinosaur
{
    private void AddAnatomicalDetails(Vector3 bulk,float hip,float skull,Material belly,Material horn,Material claw)
    {
        var mouth = new StandardMaterial3D { AlbedoColor=new Color(0.16f,0.075f,0.065f),Roughness=0.65f };
        var tongue = new StandardMaterial3D { AlbedoColor=new Color(0.43f,0.2f,0.18f),Roughness=0.6f };
        var crease = new StandardMaterial3D { AlbedoColor=new Color(0.105f,0.12f,0.08f),Roughness=1f };
        var sockets = new StandardMaterial3D { AlbedoColor=new Color(0.18f,0.2f,0.12f),Roughness=0.9f };
        Blob(_jaw,"MouthInterior",new Vector3(0,H*0.018f,-H*0.16f),new Vector3(H*0.071f,H*0.009f,H*0.16f)*skull,mouth);
        Blob(_jaw,"Tongue",new Vector3(0,H*0.028f,-H*0.15f),new Vector3(H*0.045f,H*0.009f,H*0.11f)*skull,tongue);
        for(int side=-1;side<=1;side+=2)
        {
            // Brow shelves, cheek muscles and lip seams frame the existing glassy eyes and moving jaw.
            Blob(_head,"CheekMuscle",new Vector3(side*H*0.075f*skull,-H*0.005f,-H*0.015f),new Vector3(H*0.042f,H*0.07f,H*0.08f)*skull,_skin);
            Attach(_head,"EyeBrow",Tube([
                new Vector3(side*H*0.08f*skull,H*0.073f,-H*0.16f),
                new Vector3(side*H*0.102f*skull,H*0.092f,-H*0.095f),
                new Vector3(side*H*0.083f*skull,H*0.08f,-H*0.025f)],
                [H*0.016f,H*0.024f,H*0.013f],12,_skin,true));
            Attach(_head,"LowerEyelid",Tube([
                new Vector3(side*H*0.095f*skull,H*0.058f,-H*0.12f),
                new Vector3(side*H*0.105f*skull,H*0.038f,-H*0.09f),
                new Vector3(side*H*0.095f*skull,H*0.055f,-H*0.065f)],
                [H*0.006f,H*0.009f,H*0.006f],8,sockets,true));
            Attach(_head,"LipSeam",Tube([
                new Vector3(side*H*0.06f*skull,-H*0.043f,-H*0.015f),
                new Vector3(side*H*0.087f*skull,-H*0.044f,-H*0.12f),
                new Vector3(side*H*0.077f*skull,-H*0.044f,-H*0.25f),
                new Vector3(side*H*0.035f*skull,-H*0.04f,-H*0.32f)],
                [H*0.004f,H*0.005f,H*0.004f,H*0.002f],8,crease,true));
            // The ear opening sits behind the jaw; nostril rims sit above the end of the muzzle.
            Blob(_head,"EarOpening",new Vector3(side*H*0.097f*skull,H*0.018f,H*0.025f),new Vector3(H*0.005f,H*0.018f,H*0.012f),crease);
            Blob(_head,"NoseRim",new Vector3(side*H*0.074f*skull,H*0.014f,-H*0.287f),new Vector3(H*0.014f,H*0.011f,H*0.02f),_skin);
            if(!Stats.CanGraze)
            {
                for(int tooth=0;tooth<10;tooth++)
                {
                    float z=-H*(0.045f+tooth*0.027f);
                    Attach(_jaw,"LowerTooth",Tube([
                        new Vector3(side*H*0.063f*skull,H*0.023f,z),
                        new Vector3(side*H*0.06f*skull,H*0.045f,z-H*0.003f),
                        new Vector3(side*H*0.057f*skull,H*0.055f,z-H*0.012f)],
                        [H*0.007f,H*0.0045f,H*0.0008f],8,horn,true));
                }
            }
            // Shoulder and hip contours blend into the torso rather than adding round joint caps.
            Blob(_body,"HipMuscle",new Vector3(side*bulk.X*0.4f,-H*0.02f,H*0.2f),new Vector3(bulk.X*0.5f,H*0.19f,H*0.22f),_skin);
            Blob(_body,"Shoulder",new Vector3(side*bulk.X*0.58f,H*0.035f,-H*0.24f),new Vector3(bulk.X*0.32f,H*0.14f,H*0.16f),_skin);
            for(int fold=0;fold<3;fold++)
            {
                float z=H*(-0.22f+fold*0.04f);
                Attach(_body,"ShoulderFold",Tube([
                    new Vector3(side*bulk.X*0.76f,H*0.09f,z),
                    new Vector3(side*bulk.X*0.91f,-H*0.015f,z+H*0.025f),
                    new Vector3(side*bulk.X*0.66f,-H*0.12f,z+H*0.055f)],
                    [H*0.004f,H*0.006f,H*0.002f],8,_skin,true));
            }
        }
        for(int i=0;i<_legs.Count;i++)
        {
            Blob(_shins[i],"CalfMuscle",new Vector3(0,-hip*0.12f,H*0.025f),new Vector3(H*0.058f,hip*0.2f,H*0.065f),_skin);
            Attach(_shins[i],"AnkleTendon",Tube([new Vector3(0,-hip*0.05f,H*0.05f),new Vector3(0,-hip*0.32f,H*0.08f),new Vector3(0,-hip*0.43f,H*0.065f)],[H*0.017f,H*0.012f,H*0.022f],10,_skin,true));
            var foot=_shins[i].GetNode<Node3D>("Foot"+i);
            for(int toe=-1;toe<=1;toe++)
            {
                // Rings of overlapping toe scales continue down to a hooked keratin claw.
                for(int ring=0;ring<4;ring++)
                    Blob(foot,"ToeScale",new Vector3(toe*H*0.04f,H*0.023f,-H*(0.03f+ring*0.033f)),new Vector3(H*0.025f,H*0.008f,H*0.018f),_stripe);
                CurvedClaw(foot,new Vector3(toe*H*0.04f,H*0.01f,-H*0.15f),H*0.075f,H*0.018f,claw);
            }
            if(Kind is DinosaurKind.Velociraptor or DinosaurKind.Dromaeosaurus)
            {
                float side=i%2==0?-1:1;
                CurvedClaw(foot,new Vector3(-side*H*0.055f,H*0.105f,-H*0.085f),H*0.18f,H*0.033f,claw);
            }
        }
        if(!FourLegs)
            for(int i=0;i<12;i++)
            {
                float z=H*(-0.4f+i*0.071f);
                float y=bulk.Y*Mathf.Sqrt(Mathf.Max(0.1f,1f-Mathf.Pow(z/bulk.Z,2)));
                Blob(_body,"DorsalScute",new Vector3(0,y+H*0.006f,z),new Vector3(H*0.022f,H*0.021f,H*0.028f),_stripe);
            }
        if(Kind==DinosaurKind.Triceratops)
        {
            for(int i=0;i<15;i++)
            {
                float a=Mathf.Lerp(-1.32f,1.32f,i/14f);
                var root=new Vector3(Mathf.Sin(a)*H*0.265f,H*(0.12f+Mathf.Cos(a)*0.275f),H*0.045f);
                Spike(_head,root,new Vector3(Mathf.Sin(a),Mathf.Cos(a),0)*H*0.055f,H*0.025f,horn);
                Attach(_head,"FrillVein",Tube([new Vector3(0,H*0.1f,-H*0.004f),root with { Z=0 }],[H*0.007f,H*0.003f],8,_skin,true));
            }
            Blob(_head,"ParrotBeak",new Vector3(0,-H*0.015f,-H*0.32f),new Vector3(H*0.075f,H*0.07f,H*0.075f),horn);
        }
        if(Kind==DinosaurKind.Pachycephalosaurus)
            for(int i=0;i<14;i++)
            {
                float a=i*Mathf.Tau/14;
                var root=new Vector3(Mathf.Cos(a)*H*0.125f,H*0.1f,H*(-0.05f+Mathf.Sin(a)*0.14f));
                Spike(_head,root,new Vector3(Mathf.Cos(a),0.5f,Mathf.Sin(a))*H*0.047f,H*0.024f,horn);
            }
        if(Kind==DinosaurKind.Ankylosaurus)
        {
            for(int row=-2;row<=2;row++) for(int tile=0;tile<7;tile++)
            {
                var at=new Vector3(row*bulk.X*0.36f,bulk.Y*(1-Mathf.Abs(row)*0.12f)+H*0.055f,H*(-0.35f+tile*0.115f));
                Spike(_body,at,new Vector3(row*0.02f,H*0.055f,0),H*0.055f,horn);
            }
            Blob(_tail,"ClubLobe",new Vector3(H*0.13f,H*0.1f,H*1.14f),new Vector3(H*0.15f,H*0.12f,H*0.17f),_stripe);
            Blob(_tail,"ClubLobe",new Vector3(-H*0.13f,H*0.1f,H*1.14f),new Vector3(H*0.15f,H*0.12f,H*0.17f),_stripe);
        }
        if(Kind==DinosaurKind.Gallimimus)
            Blob(_head,"ToothlessBeak",new Vector3(0,-H*0.015f,-H*0.225f),new Vector3(H*0.063f,H*0.04f,H*0.075f),horn);
    }

    private static void CurvedClaw(Node3D parent,Vector3 root,float length,float radius,Material material)
    {
        Attach(parent,"CurvedClaw",Tube([
            root,root+new Vector3(0,length*0.08f,-length*0.28f),
            root+new Vector3(0,-length*0.02f,-length*0.65f),
            root+new Vector3(0,-length*0.27f,-length*0.92f),
            root+new Vector3(0,-length*0.5f,-length)],
            [radius,radius*0.85f,radius*0.56f,radius*0.25f,radius*0.025f],10,material,true));
    }

    private static ArrayMesh PlateMesh(float size,float thickness,Material material)
    {
        // A bevelled, asymmetrical leaf of bone, with a pointed crown and a broad root.
        Vector2[] outline=[new(-0.36f,0),new(-0.65f,0.55f),new(-0.48f,1.17f),new(-0.12f,1.85f),new(0.35f,1.5f),new(0.64f,0.72f),new(0.32f,0)];
        var mesh=new MeshBuilder();
        for(int side=-1;side<=1;side+=2)
        {
            var n=new Vector3(side,0,0);
            int middle=mesh.Add(new Vector3(side*thickness,0.76f*size,0),n,new Color(0.88f,0.8f,0.68f));
            for(int i=0;i<outline.Length;i++)
            {
                var a=outline[i]; var b=outline[(i+1)%outline.Length];
                int v1=mesh.Add(new Vector3(side*thickness*0.15f,a.Y*size,a.X*size),n,new Color(0.64f,0.55f,0.45f));
                int v2=mesh.Add(new Vector3(side*thickness*0.15f,b.Y*size,b.X*size),n,new Color(0.8f,0.68f,0.5f));
                if(side<0) mesh.Tri(middle,v1,v2); else mesh.Tri(middle,v2,v1);
                var edgeNormal=new Vector3(0,b.X-a.X,a.Y-b.Y).Normalized();
                int e=mesh.Add(new Vector3(-thickness*0.15f,a.Y*size,a.X*size),edgeNormal,Colors.White);
                int f=mesh.Add(new Vector3(thickness*0.15f,a.Y*size,a.X*size),edgeNormal,Colors.White);
                int g=mesh.Add(new Vector3(thickness*0.15f,b.Y*size,b.X*size),edgeNormal,Colors.White);
                int h=mesh.Add(new Vector3(-thickness*0.15f,b.Y*size,b.X*size),edgeNormal,Colors.White);
                if(side<0) mesh.Quad(e,f,g,h);
            }
        }
        return mesh.Commit(material);
    }
}
