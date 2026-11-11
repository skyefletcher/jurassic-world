using Godot;

namespace JurassicWorld;

/// <summary>Draws a loading screen before procedural world generation begins.</summary>
public partial class Boot : Control
{
    public override async void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var background=new ColorRect { Color=new Color(0.025f,0.075f,0.055f) };
        AddChild(background); background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var centre=new CenterContainer(); AddChild(centre); centre.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var column=new VBoxContainer(); column.AddThemeConstantOverride("separation",22); centre.AddChild(column);
        column.AddChild(Hud.Text("JURASSIC WORLD",52));
        column.AddChild(Hud.Text("Preparing forests, lakes and wildlife…",22));
        column.AddChild(Hud.Text("Your prehistoric world is growing. This can take a minute.",17));
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        Callable.From(LoadWorld).CallDeferred();
    }
    private void LoadWorld()
    {
        var world=GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate();
        GetTree().Root.AddChild(world); GetTree().CurrentScene=world; QueueFree();
    }
}
