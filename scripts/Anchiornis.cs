using Godot;

namespace JurassicWorld;

/// <summary>Blender-built feathered dinosaur animated by the shared bird controller.</summary>
public partial class Anchiornis : Bird
{
    public override string DisplayName => "Anchiornis";
    public override string YoungName => "Anchiornis hatchling";
    public override AnimalStats Stats { get; } = DinosaurCatalog.Stats(DinosaurKind.Anchiornis);
    protected override float HipHeight => 0.45f;
    protected override float StandingPitch => 0.3f;
    protected override float SitHipHeight => 0.25f;
    protected override float LieHipHeight => 0.15f;

    protected override void Build()
    {
        Hair = HairMaterial();
        var model = GD.Load<PackedScene>("res://assets/anchiornis/anchiornis.glb").Instantiate<Node3D>();
        model.Name = "AnchiornisModel";
        AddChild(model);
        Node3D Part(string name) => (Node3D)model.FindChild(name, true, false);
        Frame = Part("Frame");
        Body = Part("Body");
        Neck = Part("Neck");
        Head = Part("Head");
        Tail = Part("Tail");
        Arms = [Part("WingArm0"), Part("WingArm1")];
        Hands = [Part("WingHand0"), Part("WingHand1")];
        Legs = [Part("Leg0"), Part("Leg1")];
        Shanks = [Part("Shank0"), Part("Shank1")];
        Feet = [Part("Foot0"), Part("Foot1")];
        TailFeathers = new Node3D[9];
        for (int i = 0; i < TailFeathers.Length; i++) TailFeathers[i] = Part("TailFeather" + i);
        // Register the imported jaw with the existing feeding animation.
        var importedJaw = Part("Jaw");
        var animatedJaw = Jaw(Head, importedJaw.Position);
        importedJaw.Reparent(animatedJaw);
        importedJaw.Position = Vector3.Zero;
    }
}
