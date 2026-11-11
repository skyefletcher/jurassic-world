using System;
using System.Linq;
using Godot;
using JurassicWorld;

public partial class RaptorChecks : Node3D
{
    public override void _Ready()
    {
        try
        {
            var raptor = new Velociraptor();
            AddChild(raptor);
            void Check(bool condition, string message)
            {
                if (!condition) throw new InvalidOperationException(message);
            }
            Check(raptor.UsingImportedModel, "Blue must use the approved imported model");
            Check(!raptor.GetNode<Node3D>("Frame").Visible, "Legacy mesh must be hidden for Blue");
            Check(raptor.GetNode<Node3D>("ApprovedRaptor").Visible, "Approved mesh must be visible");
            foreach (var colour in Enum.GetValues<RaptorColour>())
            {
                raptor.SetColour(colour);
                Check(raptor.UsingImportedModel == (colour is RaptorColour.Blue or RaptorColour.Delta or RaptorColour.Echo or RaptorColour.Charlie), "Colour switching must select correct model");
            }
            raptor.SetColour(RaptorColour.Blue);
            var skinMesh = (MeshInstance3D)raptor.GetNode<Node3D>("ApprovedRaptor").FindChildren("*", "MeshInstance3D", true, false)
                .OfType<MeshInstance3D>().Single();
            var blueTexture = ((StandardMaterial3D)skinMesh.GetActiveMaterial(0)).AlbedoTexture;
            raptor.SetColour(RaptorColour.Charlie);
            Check(raptor.UsingImportedModel, "Charlie must use the approved skeleton");
            Check(((StandardMaterial3D)skinMesh.GetActiveMaterial(0)).AlbedoTexture.ResourcePath == "res://assets/raptor/charlie-body.png", "Charlie must use her own green barred skin");
            raptor.Animate(5, 1, 0, 0.1f);
            Check(raptor.CurrentAnimation == "Run", "Charlie must share the skeletal gait controller");
            raptor.SetColour(RaptorColour.Delta);
            Check(raptor.UsingImportedModel, "Delta must use the approved skeleton");
            Check(((StandardMaterial3D)skinMesh.GetActiveMaterial(0)).AlbedoTexture.ResourcePath == "res://assets/raptor/delta-body.png", "Delta must use its own teal-green skin");
            raptor.Animate(5, 1, 0, 0.1f);
            Check(raptor.CurrentAnimation == "Run", "Delta must share the imported gait controller");
            raptor.SetColour(RaptorColour.Echo);
            Check(raptor.UsingImportedModel, "Echo must use the approved skeleton");
            Check(((StandardMaterial3D)skinMesh.GetActiveMaterial(0)).AlbedoTexture.ResourcePath == "res://assets/raptor/echo-body.png", "Echo must have her own brown skin");
            raptor.Animate(5, 1, 0, 0.1f);
            Check(raptor.CurrentAnimation == "Run", "Echo must share the skeletal gait controller");
            raptor.SetColour(RaptorColour.Delta);
            Check(((StandardMaterial3D)skinMesh.GetActiveMaterial(0)).AlbedoTexture.ResourcePath == "res://assets/raptor/delta-body.png", "Echo must not change Delta's skin");
            raptor.SetColour(RaptorColour.Blue);
            Check(((StandardMaterial3D)skinMesh.GetActiveMaterial(0)).AlbedoTexture == blueTexture, "Switching back must restore Blue's original texture");
            var skeleton = raptor.FindChildren("*", "Skeleton3D", true, false)[0] as Skeleton3D;
            Check(skeleton is not null && skeleton.GetBoneCount() == 136, "Approved skeleton must survive gameplay integration");
            int foot = skeleton!.FindBone("Foot.L");
            Vector3 before = skeleton.GetBoneGlobalPose(foot).Origin;
            float motion = 0;
            for (int i = 0; i < 90; i++)
            {
                raptor.Animate(2.2f, 1, 0, 1f / 60);
                motion = Mathf.Max(motion, before.DistanceTo(skeleton.GetBoneGlobalPose(foot).Origin));
            }
            Check(motion > 0.01f && raptor.CurrentAnimation == "Walk", "Imported walk must move bones");
            raptor.Animate(8, 1, 0, 0.1f);
            Check(raptor.CurrentAnimation == "Run", "Fast travel must select run");
            raptor.Meal = Food.Water;
            raptor.Animate(0, 0, 1, 0.1f);
            Check(raptor.CurrentAnimation == "Drink", "Water must use drinking clip");
            raptor.Meal = Food.Meat;
            raptor.Animate(0, 0, 1, 0.1f);
            Check(raptor.CurrentAnimation == "Eat", "Meat must use eating clip");
            string[] attacks = ["Bite", "Scratch", "BiteHeavy"];
            for (int tier = 0; tier < 3; tier++)
            {
                raptor.AttackTier = tier;
                raptor.Animate(0, 0, 0, 0.1f);
                Check(raptor.CurrentAnimation == attacks[tier], "Attack must use its source animation");
            }
            raptor.AttackTier = -1;
            raptor.Sitting = 0.7f;
            raptor.Animate(0, 0, 0, 0.1f);
            Check(raptor.CurrentAnimation == "GoToSleep", "Sit must use a settled source pose");
            raptor.Sitting = 0;
            raptor.Lying = 1;
            raptor.Animate(0, 0, 0, 0.1f);
            Check(raptor.CurrentAnimation == "Sleep", "Lying must use sleep");
            raptor.Lying = 0;
            raptor.IsAirborne = true;
            raptor.Animate(3, 0, 0, 0.1f);
            Check(raptor.CurrentAnimation == "Leap", "Jump must use leap");
            raptor.IsAirborne = false;
            raptor.Dead = 1;
            for (int i = 0; i < 90; i++) raptor.Animate(0, 0, 0, 1f / 60);
            var root = skeleton.GetBonePosePosition(skeleton.FindBone("Root"));
            Check(raptor.CurrentAnimation == "Death" && Mathf.Abs(root.X) + Mathf.Abs(root.Z) < 0.001f, "Death must not move the model away from its gameplay body");
            raptor.Dead = 0;
            raptor.Animate(0, 0, 0, 0.1f);
            raptor.LookToward(Vector3.Right, 0, 0.1f);
            Check(raptor.Bite.IsFinite(), "Animated mouth anchor must remain valid");
            int head = skeleton.FindBone("Head");
            Quaternion settled = Quaternion.Identity;
            for (int i = 0; i < 180; i++)
            {
                raptor.Animate(0, 0, 0, 0);
                raptor.LookToward(Vector3.Right, 0, 1f / 60);
                var rotation = skeleton.GetBonePoseRotation(head);
                if (i == 178) settled = rotation;
                if (i == 179) Check(settled.AngleTo(rotation) < 0.005f, "Head aim must settle without accumulating rotation");
            }
            raptor.Growth = 0.3f;
            raptor.Animate(3, 1, 0, 0.1f);
            Check(raptor.Scale.IsEqualApprox(Vector3.One * 0.3f), "Growth must scale the approved model");
            GD.Print("RAPTOR GAMEPLAY CHECKS PASSED: model selection, gait, feeding, attacks, rest, jump, death, look and growth.");
            GetTree().Quit();
        }
        catch (Exception e) { GD.PushError(e.ToString()); GetTree().Quit(1); }
    }
}
