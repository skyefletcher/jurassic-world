using Godot;

namespace JurassicWorld;

public abstract partial class Bird
{
    public float? EggMealProgress { get; set; }
    private Node3D? _mealEgg;
    private MeshInstance3D? _wholeEgg, _openEgg, _eggContents, _shellChip;

    public void BeginEggMeal()
    {
        EndEggMeal();
        EggMealProgress = 0f;
        _mealEgg = new Node3D { Name = "HeldEgg" };
        AddChild(_mealEgg);
        var shell = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.93f, 0.91f, 0.84f), Roughness = 0.9f,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
        };
        _wholeEgg = new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = 0.045f, Height = 0.12f }, MaterialOverride = shell,
        };
        _mealEgg.AddChild(_wholeEgg);
        // An actual open shell: jagged rim, hollow inside, with visible yolk below the opening.
        var bowl = new MeshBuilder();
        const int sides = 20, rings = 6;
        for (int ring = 0; ring <= rings; ring++)
        for (int i = 0; i <= sides; i++)
        {
            float angle = i * Mathf.Tau / sides;
            float t = ring / (float)rings;
            float latitude = t * Mathf.Pi * 0.56f;
            float jagged = ring == rings ? (i % 2 == 0 ? 0.004f : -0.004f) : 0f;
            var p = new Vector3(Mathf.Cos(angle) * Mathf.Sin(latitude) * 0.045f,
                -Mathf.Cos(latitude) * 0.06f + jagged, Mathf.Sin(angle) * Mathf.Sin(latitude) * 0.045f);
            bowl.Add(p, p.Normalized(), Colors.White);
        }
        for (int ring = 0; ring < rings; ring++)
        for (int i = 0; i < sides; i++)
        {
            int a = ring * (sides + 1) + i, b = a + sides + 1;
            bowl.Quad(a, b, b + 1, a + 1);
        }
        _openEgg = new MeshInstance3D { Mesh = bowl.Commit(shell), Visible = false };
        _mealEgg.AddChild(_openEgg);
        _eggContents = new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = 0.035f, Height = 0.025f },
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.95f, 0.65f, 0.12f), Roughness = 0.3f },
            Position = new Vector3(0, -0.015f, 0), Visible = false,
        };
        _mealEgg.AddChild(_eggContents);
        _shellChip = new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = 0.018f, Height = 0.008f }, MaterialOverride = shell, Visible = false,
        };
        _mealEgg.AddChild(_shellChip);
    }

    public void EndEggMeal()
    {
        EggMealProgress = null;
        _mealEgg?.QueueFree();
        _mealEgg = null;
    }

    public Node3D? ReleaseEgg()
    {
        var egg = _mealEgg;
        _mealEgg = null;
        EggMealProgress = null;
        return egg;
    }

    private void AnimateEggMeal()
    {
        if (EggMealProgress is not { } progress || _mealEgg is null)
            return;
        float settle = Mathf.SmoothStep(0f, 0.12f, progress);
        // One foot pins the egg while the other supports the bird.
        var footTarget = Feet[0].GlobalPosition.Lerp(ToGlobal(new Vector3(-0.06f, FootHeight, -0.17f)), settle);
        for (int iteration = 0; iteration < 10; iteration++)
        foreach (var joint in new[] { Shanks[0], Legs[0] })
        {
            var from = Feet[0].GlobalPosition - joint.GlobalPosition;
            var to = footTarget - joint.GlobalPosition;
            if (from.LengthSquared() > 0.000001f && to.LengthSquared() > 0.000001f)
                TurnEggJoint(joint, from, to, 1f);
        }
        Feet[0].GlobalBasis = GlobalBasis * new Basis(Vector3.Right, -0.2f * settle);
        var egg = Feet[0].ToGlobal(new Vector3(0.015f, 0.055f, -0.045f));
        _mealEgg.GlobalPosition = egg;
        bool cracked = progress >= 0.3f;
        _wholeEgg!.Visible = !cracked;
        _openEgg!.Visible = cracked;
        _eggContents!.Visible = cracked;
        _shellChip!.Visible = cracked;
        if (cracked)
        {
            float fall = Mathf.Clamp((progress - 0.3f) / 0.12f, 0f, 1f);
            _shellChip.Position = new Vector3(0.035f + fall * 0.04f, 0.04f - fall * 0.09f, 0);
            _shellChip.Rotation = new Vector3(fall * 2f, 0, fall);
            _eggContents.Scale = Vector3.One * Mathf.Lerp(1f, 0.05f, Mathf.SmoothStep(0.4f, 0.88f, progress));
        }

        // Deliberate cracking pecks, then deeper dips into the opening followed by a raised swallowing pose.
        float cycle = progress * 5f;
        float dip = 0.5f - 0.5f * Mathf.Cos(cycle * Mathf.Tau);
        float lift = progress > 0.86f ? Mathf.SmoothStep(0.86f, 1f, progress) : 0f;
        var target = egg + GlobalBasis.Y * (0.015f + (1f - dip) * 0.065f + lift * 0.13f);
        // Rotate connected neck joints toward the egg; never detach or translate the head.
        for (int iteration = 0; iteration < 16; iteration++)
        foreach (var joint in new[] { Head, Neck })
        {
            var beak = Head.ToGlobal(new Vector3(0, -0.036f, -0.115f));
            var from = beak - joint.GlobalPosition;
            var to = target - joint.GlobalPosition;
            if (from.LengthSquared() < 0.000001f || to.LengthSquared() < 0.000001f)
                continue;
            TurnEggJoint(joint, from, to, settle * 0.7f);
        }
    }

    private static void TurnEggJoint(Node3D joint, Vector3 from, Vector3 to, float amount)
    {
        // Aim entirely in the parent's coordinate space, so inherited size never enters the joint's rotation.
        var parentBasis = joint.GetParent<Node3D>().GlobalBasis;
        var inverse = parentBasis.Inverse();
        from = (inverse * from).Normalized();
        to = (inverse * to).Normalized();
        var turn = new Quaternion(from, to).Normalized();
        var rotation = Quaternion.Identity.Slerp(turn, amount).Normalized();
        joint.Quaternion = (rotation * joint.Quaternion.Normalized()).Normalized();
        joint.Scale = Vector3.One;
    }
}
