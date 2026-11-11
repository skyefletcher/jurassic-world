using Godot;

namespace JurassicWorld;

public enum DinosaurKind { Velociraptor, Ankylosaurus, Gallimimus, Tyrannosaurus, Stegosaurus, Pachycephalosaurus, Triceratops, Anchiornis, Dromaeosaurus }
public enum RaptorColour { Blue, Echo, Charlie, Delta }

/// <summary>The user's balance sheet. Damage is HP per second; needs and internal health bars use percentages.</summary>
public static class DinosaurCatalog
{
    public static AnimalStats Stats(DinosaurKind kind)
    {
        var (jump, length, land, water, agility, stamina, hp, basic, normal, special) = kind switch
        {
            DinosaurKind.Velociraptor => (7, 8, 9, 2, 10, 8, 1000, 20, 50, 90),
            DinosaurKind.Ankylosaurus => (0, 0, 2, 0, 1, 7, 1800, 10, 25, 90),
            DinosaurKind.Gallimimus => (6, 7, 9, 1, 8, 9, 350, 10, 15, 40),
            DinosaurKind.Tyrannosaurus => (3, 4, 7, 2, 4, 8, 2000, 20, 45, 100),
            DinosaurKind.Stegosaurus => (0, 0, 2, 0, 1, 6, 1600, 12, 30, 85),
            DinosaurKind.Pachycephalosaurus => (3, 4, 6, 1, 4, 7, 900, 12, 28, 70),
            DinosaurKind.Triceratops => (1, 1, 4, 1, 2, 8, 1750, 15, 35, 85),
            DinosaurKind.Anchiornis => (8, 8, 6, 0, 10, 8, 300, 8, 20, 50),
            _ => (9, 10, 10, 3, 10, 9, 500, 15, 28, 65),
        };
        bool herbivore = kind is DinosaurKind.Ankylosaurus or DinosaurKind.Stegosaurus or DinosaurKind.Triceratops or DinosaurKind.Pachycephalosaurus or DinosaurKind.Gallimimus;
        var (height, radius, mouth, camera) = kind switch
        {
            DinosaurKind.Tyrannosaurus => (5.4f, 1.2f, 3.1f, 16f),
            DinosaurKind.Stegosaurus => (3.8f, 1.2f, 2.8f, 14f),
            DinosaurKind.Triceratops => (3.1f, 1.1f, 2.6f, 13f),
            DinosaurKind.Ankylosaurus => (2.4f, 1.1f, 2.4f, 12f),
            DinosaurKind.Gallimimus => (2.8f, 0.55f, 1.5f, 8f),
            DinosaurKind.Pachycephalosaurus => (2.4f, 0.6f, 1.5f, 8f),
            DinosaurKind.Anchiornis => (0.9f, 0.22f, 0.5f, 4.5f),
            DinosaurKind.Dromaeosaurus => (1.4f, 0.4f, 1.1f, 6.5f),
            _ => (1.9f, 0.5f, 1.6f, 8f),
        };
        var attacks = kind switch
        {
            DinosaurKind.Ankylosaurus => ("Tail swipe", "Tail knock-up", "Shake off attackers"),
            DinosaurKind.Gallimimus => ("Bite", "Claw slash", "Knockdown kick"),
            DinosaurKind.Tyrannosaurus => ("Bite", "Open-jaw charge", "Lift smaller opponent"),
            DinosaurKind.Stegosaurus => ("Tail swipe", "Tail knock-up", "Front-leg stomp"),
            DinosaurKind.Pachycephalosaurus => ("Tail slash", "Headbutt", "Knockdown charge"),
            DinosaurKind.Triceratops => ("Tail swipe", "Horn jab", "Ten-second horn charge"),
            DinosaurKind.Anchiornis => ("Bite", "Claw slash", "Grab, bite and scratch"),
            _ => ("Bite", "Big-claw slash", "Grab, bite and scratch"),
        };
        return new AnimalStats
        {
            Scores = new AnimalScores { JumpHeight = jump, JumpLength = length, LandSpeed = land, WaterSpeed = water, Agility = agility, Stamina = stamina },
            MaxHealth = hp, BasicDps = basic, NormalDps = normal, SpecialDps = special,
            BasicAttack = attacks.Item1, NormalAttack = attacks.Item2, SpecialAttack = attacks.Item3,
            CanGraze = herbivore, Abilities = herbivore ? Ability.Herd : Ability.Hunt | Ability.Pack | Ability.Grip,
            Companions = kind is DinosaurKind.Velociraptor or DinosaurKind.Anchiornis ? 0 : herbivore ? 3 : 2,
            BodyHeight = height, BodyRadius = radius, MouthDistance = mouth, EatReach = 1.2f,
            FloatDepth = height * 0.6f, WadeDepth = height * 0.3f, CameraHeight = height * 0.8f,
            CameraDistance = camera, FlySpeed = 8f, BreathSeconds = 35f,
        } with { Abilities = kind == DinosaurKind.Anchiornis ? Ability.Hunt | Ability.Fly | Ability.ClimbTrees | Ability.Grip : kind == DinosaurKind.Tyrannosaurus ? Ability.Hunt | Ability.Dive : herbivore ? Ability.Herd : Ability.Hunt | Ability.Pack | Ability.Grip };
    }
}
