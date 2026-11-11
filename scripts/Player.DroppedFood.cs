using System.Collections.Generic;
using Godot;

namespace JurassicWorld;

public partial class Player
{
    private readonly List<(RigidBody3D Body, Node3D Model, bool Egg, float Elapsed)> _droppedFood = [];

    private void PutFoodDown(Node3D model, bool egg, float elapsed = 0f)
    {
        var scale = model.GlobalBasis.Scale;
        var body = new RigidBody3D
        {
            Name = egg ? "DroppedEgg" : "DroppedFish", Mass = egg ? 0.15f : 0.5f,
            LinearDamp = 1.5f, AngularDamp = 3f,
            ContinuousCd = true,
        };
        body.AddChild(new CollisionShape3D
        {
            Shape = egg ? new SphereShape3D { Radius = 0.085f * Size }
                : new BoxShape3D { Size = new Vector3(0.8f, 0.2f, 0.18f) * Size },
        });
        GetParent().AddChild(body);
        body.GlobalPosition = model.GlobalPosition + Vector3.Up * 0.12f;
        body.GlobalRotation = model.GlobalRotation;
        model.Reparent(body);
        model.Transform = new Transform3D(Basis.FromScale(scale), Vector3.Zero);
        body.AddCollisionExceptionWith(this);
        if (Animal.IsFlying)
            body.LinearVelocity = Velocity;
        _droppedFood.Add((body, model, egg, elapsed));
    }

    private void UpdateDroppedFood()
    {
        if (Animal is not Anchiornis || IsDead)
            return;
        bool pressed = Input.IsActionJustPressed(InputSetup.PutDownFood);
        if (_fish is { } fish)
        {
            ActionPrompt = (ActionPrompt ?? "E to eat your fish") + "\nR to put the fish down";
            if (pressed)
            {
                PutFoodDown(fish, false);
                _fish = null;
            }
            return;
        }
        if (_eatingEgg && _eggBird is { } bird)
        {
            ActionPrompt = (ActionPrompt ?? "Eating the egg") + "\nR to put the egg down";
            if (pressed && bird.ReleaseEgg() is { } egg)
            {
                PutFoodDown(egg, true, _eggElapsed);
                StopEggMeal();
                _feeding = Feeding.None;
            }
            return;
        }
        if (IsCarrying || IsLifting || IsFeeding)
            return;
        for (int i = _droppedFood.Count - 1; i >= 0; i--)
        {
            var food = _droppedFood[i];
            if (!GodotObject.IsInstanceValid(food.Body))
            {
                _droppedFood.RemoveAt(i);
                continue;
            }
            if (food.Body.GlobalPosition.DistanceTo(GlobalPosition) > Stats.BodyHeight + Stats.EatReach + 0.6f)
                continue;
            if (food.Egg && (IsSwimming || Animal.IsFlying))
                continue;
            ActionPrompt = food.Egg ? "R to pick up the egg and eat" : "R to pick up the fish";
            if (!pressed)
                return;
            if (food.Egg)
            {
                StartFeeding(Feeding.Eating, EggMealSeconds);
                _eggElapsed = food.Elapsed;
                _eatingEgg = true;
                _eggBird = (Bird)Animal;
                _eggBird.BeginEggMeal();
                _eggBird.EggMealProgress = _eggElapsed / EggMealSeconds;
            }
            else
            {
                _fish = food.Model;
                _fish.Reparent(this);
                _fish.Rotation = Vector3.Zero;
                HoldFish();
            }
            food.Body.QueueFree();
            _droppedFood.RemoveAt(i);
            return;
        }
    }
}
