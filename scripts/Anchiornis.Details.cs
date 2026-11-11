using Godot;

namespace JurassicWorld;

public partial class Anchiornis
{
    private void BuildPlumageDetails(Material dark,Material pale,Material red,Material feather)
    {
        Attach(Body,"LayeredBodyFeathers",ContourCoat(new Vector3(0.13f,0.14f,0.28f),Vector3.Zero,new Color(0.2f,0.23f,0.25f),7,28,0.062f,feather));
        Attach(Head,"FineFaceFeathers",ContourCoat(new Vector3(0.087f,0.082f,0.131f),Vector3.Zero,new Color(0.19f,0.21f,0.22f),5,20,0.025f,feather));
        Attach(Neck,"ThroatFeathers",ContourCoat(new Vector3(0.073f,0.13f,0.083f),Vector3.Zero,new Color(0.78f,0.77f,0.7f),6,20,0.035f,feather));
        // The red crest has individual pointed plumes rather than a smooth cap.
        for(int i=0;i<12;i++)
        {
            float z=-0.08f+i*0.014f;
            Attach(Head,"CrestPlume",Tube([new Vector3(0,0.07f,z),new Vector3(0,0.13f,z+0.025f),new Vector3(0,0.16f,z+0.055f)],[0.016f,0.012f,0.001f],8,red,true));
        }
        for(int side=-1;side<=1;side+=2)
        {
            Attach(Head,"FeatheredBrow",Tube([new Vector3(side*0.07f,0.045f,-0.09f),new Vector3(side*0.085f,0.057f,-0.06f),new Vector3(side*0.07f,0.045f,-0.025f)],[0.008f,0.012f,0.007f],10,dark,true));
            Attach(Head,"Nostril",Ellipsoid(new Vector3(0.004f,0.006f,0.009f),12,8,dark),new Vector3(side*0.047f,-0.005f,-0.17f));
        }
        for(int wing=0;wing<2;wing++)
        {
            float side=wing==0?-1:1;
            for(int j=0;j<10;j++)
            {
                var parent=j<5?Arms[wing]:Hands[wing];
                var root=new Vector3(side*(j%5)*0.065f,0.004f,0);
                var along=new Vector3(side*0.35f,0,1).Normalized();
                Attach(parent,"FeatherShaft",Tube([root,root+along*0.17f,root+along*0.33f],[0.0023f,0.0015f,0.0003f],6,pale,true));
                var across=Vector3.Up.Cross(along).Normalized();
                var barbs=new MeshBuilder();
                for(int barb=1;barb<15;barb++)
                {
                    float f=barb/16f;
                    var centre=root+along*(0.35f*f)+Vector3.Up*0.002f;
                    for(int edge=-1;edge<=1;edge+=2)
                    {
                        var tip=centre+across*(edge*0.038f*(1-f*0.4f))-along*0.018f;
                        var colour=barb%2==0?new Color(0.47f,0.48f,0.46f):new Color(0.66f,0.66f,0.61f);
                        int a=barbs.Add(centre,Vector3.Up,colour),b=barbs.Add(tip,Vector3.Up,colour),c=barbs.Add(centre+along*0.0015f,Vector3.Up,colour);
                        barbs.Tri(a,b,c);barbs.Tri(c,b,a);
                    }
                }
                Attach(parent,"FeatherBarbs",barbs.Commit(feather));
            }
            // Scaled ankles and sharply hooked toe tips remain visible under the hind wings.
            for(int toe=-1;toe<=1;toe++)
            {
                for(int ring=0;ring<4;ring++) Attach(Feet[wing],"ToeScute",Ellipsoid(new Vector3(0.009f,0.004f,0.007f),10,6,pale),new Vector3(toe*0.025f,0.005f,-0.025f-ring*0.02f));
                Attach(Feet[wing],"HookedTalon",Tube([new Vector3(toe*0.03f,0,-0.1f),new Vector3(toe*0.032f,0.008f,-0.13f),new Vector3(toe*0.032f,-0.018f,-0.15f)],[0.007f,0.0045f,0.0005f],8,dark,true));
            }
        }
    }

    private static ArrayMesh ContourCoat(Vector3 radii,Vector3 offset,Color colour,int rows,int columns,float length,Material material)
    {
        var mesh=new MeshBuilder();
        for(int row=0;row<rows;row++) for(int col=0;col<columns;col++)
        {
            float lat=Mathf.Lerp(-1.18f,1.18f,(row+0.5f)/rows);
            float angle=(col+(row%2)*0.5f)*Mathf.Tau/columns;
            var u=new Vector3(Mathf.Cos(lat)*Mathf.Cos(angle),Mathf.Sin(lat),Mathf.Cos(lat)*Mathf.Sin(angle));
            var n=(u/radii).Normalized();
            var root=offset+u*radii+n*0.002f;
            var along=(Vector3.Back-n*n.Dot(Vector3.Back)).Normalized();
            if(along.LengthSquared()<0.1f) along=Vector3.Up;
            var across=n.Cross(along).Normalized()*length*0.29f;
            var tip=root+along*length-n*0.002f;
            var ridge=root+along*length*0.45f+n*0.005f;
            float shade=0.85f+0.12f*Mathf.Sin(col*3.1f+row*1.9f);
            var tint=colour*shade;
            int a=mesh.Add(root,n,tint.Darkened(0.2f)),b=mesh.Add(root+along*length*0.4f-across,n,tint),c=mesh.Add(tip,n,tint.Lightened(0.08f)),d=mesh.Add(root+along*length*0.4f+across,n,tint),r=mesh.Add(ridge,n,tint.Lightened(0.12f));
            mesh.Tri(a,b,r);mesh.Tri(b,c,r);mesh.Tri(c,d,r);mesh.Tri(d,a,r);
        }
        return mesh.Commit(material);
    }
}
