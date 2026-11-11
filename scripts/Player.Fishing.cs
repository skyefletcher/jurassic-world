using Godot;

namespace JurassicWorld;

/// <summary>Eagles snatch surface fish with their feet, and row ashore with wet wings.</summary>
public partial class Player
{
    private Node3D? _fish;

    /// <summary>Water brakes the entry momentum; buoyancy then carries the eagle back to its rowing waterline.</summary>
    private void EaglePlunge(float dt, Vector3 direction, float? surface)
    {
        if (surface is not { } top)
        {
            StopDiving();
            IsSwimming = Animal.IsSwimming = false;
            return;
        }

        IsSwimming = Animal.IsSwimming = Animal.IsDiving = true;
        bool rising = Input.IsActionPressed(InputSetup.Jump);
        // Drag acts on every component. Steering gives a little control without restoring the speed of the dive.
        var velocity = Velocity * Mathf.Exp(-2.2f * dt);
        var horizontal = new Vector3(velocity.X, 0, velocity.Z).MoveToward(direction * Stats.SwimSpeed * 0.5f, dt * 1.5f);
        velocity = new Vector3(horizontal.X, velocity.Y + (rising ? 13f : 8f) * dt, horizontal.Z);
        var before = GlobalPosition;
        Velocity = velocity;
        MoveAndSlide();
        Fish(before);

        float floatY = top - Stats.FloatDepth;
        if (GlobalPosition.Y >= floatY && velocity.Y >= 0)
        {
            GlobalPosition = GlobalPosition with { Y = floatY };
            StopDiving();
            Animal.Flap = Animal.Dive = 0;
            return;
        }

        ActionPrompt = _fish is null ? "F to grab a fish; hold Space to swim up" : "Hold Space to swim up with your fish";
        Breath = Mathf.MoveToward(Breath, 0, 100f / Stats.BreathSeconds * dt);
        if (Breath <= 0)
            Hurt(DrownDamagePerSecond * dt);
        UpdateNeeds(dt, exerting: true);
        UpdateStamina(dt, effort: Stats.SwimEffort);
        float yaw = direction == Vector3.Zero ? Animal.Rotation.Y
            : Mathf.LerpAngle(Animal.Rotation.Y, Yaw(direction), dt * Stats.TurnSpeed);
        float pitch = Mathf.Clamp(Mathf.Atan2(velocity.Y, Mathf.Max(horizontal.Length(), 1f)), -1.2f, 0.7f);
        Animal.Rotation = new Vector3(Mathf.Lerp(Animal.Rotation.X, pitch, Mathf.Min(1, dt * 5)), yaw, 0);
        Animal.Flap = 0;
        Animal.Dive = 1;
        AnimateAnimal(velocity.Length(), 0, 0, dt);
    }

    private void Fish(Vector3 from)
    {
        if (Animal is not Anchiornis || IsDead)
            return;
        if (_fish is not null)
        {
            if (IsSwimming || _mode == Mode.Flying)
                ActionPrompt = "Carry your fish ashore to eat it";
            else
            {
                ActionPrompt = "E or click to eat your fish";
                if (Input.IsActionJustPressed(InputSetup.Eat))
                {
                    Hunger = Mathf.Min(100f, Hunger + Seabed.FishNourishment);
                    Animal.Meal = Food.Meat;
                    DoChore(ChoreKind.EatMeat, 1f);
                    DropFish();
                }
            }
            return;
        }
        if (IsCarrying || IsLifting || IsFeeding)
            return;
        ActionPrompt ??= IsSwimming ? "F to catch a fish; WASD to row, Space to take off" : null;
        bool grabbing = Input.IsActionPressed(InputSetup.Sprint) && (_mode == Mode.Flying || IsDiving)
            || Input.IsActionJustPressed(InputSetup.Attack);
        if (!grabbing || Seabed?.CatchSurfaceFish(from + Vector3.Down * 0.3f, GlobalPosition + Vector3.Down * 0.3f, 1f * Size, surfaceOnly: !IsDiving) != true)
            return;
        _fish = new Node3D { Name = "CaughtFish" };
        _fish.AddChild(new MeshInstance3D { Mesh = FishBuilder.Build(), Scale = Vector3.One * 2.5f,
            Rotation = new Vector3(0, Mathf.Pi / 2f, 0) });
        AddChild(_fish);
        Hunted();
        ((Bird)Animal).Slash();
    }

    private void HoldFish()
    {
        if (_fish is not null && Animal is Bird bird)
            _fish.GlobalPosition = bird.Talons;
    }

    private void DropFish()
    {
        _fish?.QueueFree();
        _fish = null;
    }
}
