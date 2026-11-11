using System.Threading.Tasks;
using Godot;
using JurassicWorld;

/// <summary>Rendered art review. Saves a gallery in .runtime and exits.</summary>
public partial class Preview : Node3D
{
    public override async void _Ready()
    {
        if (System.Array.Exists(OS.GetCmdlineUserArgs(), a => a == "--selector"))
        {
            var player=GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>(); AddChild(player);
            player.ProcessMode=ProcessModeEnum.Disabled;
            var canvas=new CanvasLayer(); AddChild(canvas);
            var selector=new AnimalShop { Player=player }; canvas.AddChild(selector);
            selector.Open("Choose your dinosaur","All nine dinosaurs are ready to play. Choose your dinosaur and your raptor colour.",false,player.SwitchTo);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            GetViewport().GetTexture().GetImage().SavePng("res://.runtime/selector.png"); GetTree().Quit(); return;
        }
        if (System.Array.Exists(OS.GetCmdlineUserArgs(), a => a == "--world"))
        {
            var main=GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<Node3D>(); AddChild(main);
            main.GetNode<Hud>("Hud").Visible=false;
            var player=main.GetNode<Player>("Player");
            typeof(Player).GetProperty("Age")!.SetValue(player,1f);
            player.SwitchTo(1); player.SwitchTo(0);
            bool blue=System.Array.Exists(OS.GetCmdlineUserArgs(),a=>a=="--blue");
            if(blue) player.ChooseRaptor(RaptorColour.Blue);
            player.ProcessMode=ProcessModeEnum.Inherit; player.SetPhysicsProcess(false);
            var worldCamera=new Camera3D { Position=player.GlobalPosition+(blue?new Vector3(4.5f,2.8f,-5.5f):new Vector3(7,4.5f,9)),Current=true,Fov=60 };
            AddChild(worldCamera); worldCamera.LookAt(player.GlobalPosition+Vector3.Up);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            GetViewport().GetTexture().GetImage().SavePng("res://.runtime/world.png");
            GD.Print("Saved .runtime/world.png"); GetTree().Quit(); return;
        }
        AddChild(new WorldEnvironment { Environment=new Godot.Environment { BackgroundMode=Godot.Environment.BGMode.Color,BackgroundColor=new Color(0.1f,0.17f,0.14f),AmbientLightSource=Godot.Environment.AmbientSource.Color,AmbientLightColor=new Color(0.8f,0.88f,0.8f),AmbientLightEnergy=0.8f,TonemapMode=Godot.Environment.ToneMapper.Filmic } });
        AddChild(new DirectionalLight3D { RotationDegrees=new Vector3(-55,-30,0),LightEnergy=1.7f,ShadowEnabled=true });
        AddChild(new MeshInstance3D { Mesh=new PlaneMesh { Size=new Vector2(80,80),Material=new StandardMaterial3D { AlbedoColor=new Color(0.19f,0.26f,0.18f) } } });
        var camera=new Camera3D { Position=new Vector3(12,12,22),Current=true,Fov=42 }; AddChild(camera); camera.LookAt(new Vector3(0,1,-1));
        bool closeup=System.Array.Exists(OS.GetCmdlineUserArgs(),a=>a=="--closeup");
        if(closeup)
        {
            camera.Position=new Vector3(3.5f,2.25f,-4.2f); camera.Fov=39;
            camera.LookAt(new Vector3(0,1f,0.15f));
            var raptor=new Velociraptor(); AddChild(raptor); raptor.SetColour(RaptorColour.Blue);
            raptor.Animate(0,0,0,0.1f);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            GetViewport().GetTexture().GetImage().SavePng("res://.runtime/raptor-detail.png");
            GD.Print("Saved .runtime/raptor-detail.png"); GetTree().Quit(); return;
        }
        for(int i=0;i<Player.Kinds.Length;i++)
        {
            var d=Player.Kinds[i](); AddChild(d);
            float scale=2f/d.Stats.BodyHeight; d.Scale=Vector3.One*scale;
            d.Position=new Vector3((i%3-1)*6f,0,(i/3-1)*6f); d.Rotation=new Vector3(0,-0.7f,0); d.Animate(0,0,0,0.1f);
            var label=new Label3D { Text=d.DisplayName,Position=d.Position+new Vector3(0,3.1f,0),Billboard=BaseMaterial3D.BillboardModeEnum.Enabled,FontSize=50,PixelSize=0.008f,NoDepthTest=true }; AddChild(label);
        }
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
        GetViewport().GetTexture().GetImage().SavePng("res://.runtime/dinosaurs.png");
        GD.Print("Saved .runtime/dinosaurs.png"); GetTree().Quit();
    }
}
