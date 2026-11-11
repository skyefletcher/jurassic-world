using System;
using Godot;
using JurassicWorld;

public partial class AnchiornisChecks : Node3D
{
    public override void _Ready()
    {
        try
        {
            var bird = new Anchiornis();
            AddChild(bird);
            void Check(bool condition, string message)
            {
                if (!condition) throw new InvalidOperationException(message);
            }
            Node3D Part(string name) => (Node3D)bird.FindChild(name, true, false);
            Check(Part("AnchiornisModel") != null, "Imported Anchiornis must load");
            var wing = Part("WingArm0");
            bird.Animate(0, 0, 0, 0.1f);
            var folded = wing.Rotation;
            bird.IsFlying = true;
            bird.Flap = 1;
            for (int i = 0; i < 60; i++) bird.Animate(4, 1, 0, 1f / 60);
            Check(wing.Rotation.DistanceTo(folded) > 0.1f, "Imported wings must unfold and flap");
            Check(bird.Talons.IsFinite(), "Imported feet must provide valid talon anchors");
            bird.Slash();
            bird.Animate(1, 1, 0, 0.1f);
            Check(bird.Talons.IsFinite(), "Talon slash must keep imported feet valid");
            bird.IsFlying = false;
            bird.Sitting = 1;
            bird.Animate(0, 0, 1, 0.1f);
            Check(Part("Body").Position.IsFinite(), "Resting and eating must pose the imported body");
            GD.Print("ANCHIORNIS CHECKS PASSED: imported model, wingbeats, talons, slash, rest and feeding.");
            GetTree().Quit();
        }
        catch (Exception e) { GD.PushError(e.ToString()); GetTree().Quit(1); }
    }
}
