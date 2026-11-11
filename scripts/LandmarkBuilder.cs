using System;
using System.Collections.Generic;
using Godot;

namespace JurassicWorld;

/// <summary>
/// Builds the landmarks' meshes in code, as everything else is: Lookout Crag's slab, the mammoth bones, the frozen
/// waterfall, the Balanced Stone, the springs' steam, the Ice Arch and the basalt Pillars. Anything an animal could
/// stand on or bump into is solid.
/// </summary>
public static partial class LandmarkBuilder
{
    private static readonly Color SnowWhite = new(0.3f, 0.43f, 0.21f);
    private static readonly Color Bone = new(0.86f, 0.82f, 0.71f);
    private static readonly Color OldBone = new(0.74f, 0.68f, 0.55f);
    private static readonly Color PaleIce = new(0.3f, 0.33f, 0.26f);
    private static readonly Color DeepIce = new(0.15f, 0.17f, 0.14f);
    private static readonly Color Basalt = new(0.2f, 0.2f, 0.23f);

    /// <summary>Lumps and grain for rock and ice, so no two boulders are the same shape.</summary>
    private static readonly FastNoiseLite Lumps = new() { Seed = 31, NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth, Frequency = 1.2f };

    private static StandardMaterial3D Painted(float roughness = 0.9f, float metallic = 0f) => new()
    {
        VertexColorUseAsAlbedo = true,
        VertexColorIsSrgb = true,
        Roughness = roughness,
        Metallic = metallic,
        CullMode = BaseMaterial3D.CullModeEnum.Disabled,
    };

    private static StandardMaterial3D Plain(Color colour, float roughness, float metallic = 0f) =>
        new() { AlbedoColor = colour, Roughness = roughness, Metallic = metallic };

    /// <summary>Adds a mesh that animals collide with, as its own shape exactly.</summary>
    private static void Solid(Node3D parent, string name, Mesh mesh, Transform3D at)
    {
        parent.AddChild(new MeshInstance3D { Name = name, Mesh = mesh, Transform = at });
        var body = new StaticBody3D { Name = name + "Body", Transform = at };
        body.AddChild(new CollisionShape3D { Shape = new ConcavePolygonShape3D { Data = mesh.GetFaces(), BackfaceCollision = true } });
        parent.AddChild(body);
    }

    /// <summary>Pushes a coordinate on the unit sphere out towards the unit cube, flattening the shape's faces.</summary>
    private static float Boxy(float v, float power) => Mathf.Sign(v) * Mathf.Pow(Mathf.Abs(v), power);

    /// <summary>A lumpy ellipsoid: a boulder, a block of ice, a half-buried skull.</summary>
    private static ArrayMesh Lumpy(Vector3 radii, float roughness, int seed, Material material, Func<Vector3, Vector3, Color>? colouring = null)
    {
        var shift = new Vector3(seed * 7.31f, seed * 3.17f, 0f);
        return Animal.Ellipsoid(u => u * radii * (1f + roughness * Lumps.GetNoise3Dv(u * 1.5f + shift)), 24, 14, material, colouring);
    }

    /// <summary>
    /// Grey granite, darker in its cracks, crusted with orange lichen on its sides and snow on its tops, where it lies flat
    /// enough to hold it.
    /// </summary>
    private static Color Granite(Vector3 p, Vector3 n)
    {
        float grain = Lumps.GetNoise3D(p.X * 1.5f, p.Y * 1.5f, p.Z * 1.5f);
        var rock = new Color(0.52f, 0.5f, 0.48f).Darkened(Mathf.Clamp(-grain, 0f, 1f) * 0.35f).Lightened(Mathf.Clamp(grain, 0f, 1f) * 0.1f);
        float lichen = Mathf.SmoothStep(0.3f, 0.5f, Lumps.GetNoise3D(p.X * 0.6f + 50f, p.Y * 0.6f, p.Z * 0.6f));
        rock = rock.Lerp(new Color(0.75f, 0.5f, 0.2f), lichen * 0.45f * (1f - Mathf.Abs(n.Y)));
        return rock.Lerp(SnowWhite, Mathf.SmoothStep(0.6f, 0.85f, n.Y));
    }

    /// <summary>
    /// Glacier ice: deep blue low down and in its depths, paler higher up, banded with the layers of old snow it was
    /// pressed from, and white where fresh snow lies on top.
    /// </summary>
    private static Color Ice(Vector3 p, Vector3 n, float height)
    {
        float band = 0.5f + 0.5f * Mathf.Sin(p.Y * 2.2f + Lumps.GetNoise3D(p.X * 0.4f, 0f, p.Z * 0.4f) * 3f);
        var ice = DeepIce.Lerp(PaleIce, Mathf.Clamp(height, 0f, 1f) * 0.6f + band * 0.3f);
        return ice.Lerp(SnowWhite, Mathf.SmoothStep(0.55f, 0.8f, n.Y));
    }

    /// <summary>
    /// Lookout Crag's ledge: a long, thin blade of granite jutting far out from the top of the tower over the meadow,
    /// narrowing to a point and kicking up at its tip, where a layer of rock harder than the rest under it has been left
    /// standing as the softer rock weathered away. An animal can walk out along it to its tip, high over the steppe.
    /// Great boulders broken off the tower lie heaped at its foot.
    /// </summary>
    public static void Crag(Node3D parent, Vector3 top, Vector2 facing, Func<float, float, float> ground)
    {
        var ledge = Animal.Ellipsoid(u =>
        {
            var b = new Vector3(Boxy(u.X, 0.5f), Boxy(u.Y, 0.6f), Boxy(u.Z, 0.4f));
            // 0 at the root, deep in the tower's top, to 1 at the tip, 26 m out over the meadow (the model faces -Z).
            float t = (1f - b.Z) / 2f;
            var p = new Vector3(b.X * Mathf.Lerp(5f, 0.6f, Mathf.Pow(t, 1.3f)), b.Y * Mathf.Lerp(1.6f, 0.4f, t), b.Z * 18f - 8f);
            p.Y += 4f * t * t * t;
            return p + Vector3.One * 0.25f * Lumps.GetNoise3Dv(p * 0.3f);
        }, 40, 18, Painted(), Granite);
        var basis = Basis.LookingAt(new Vector3(facing.X, 0f, facing.Y), Vector3.Up);
        Solid(parent, "Ledge", ledge, new Transform3D(basis, top + Vector3.Down * 1.4f));

        // Boulders heaped round the foot of the tower, the biggest the size of a mammoth.
        var across = facing.Orthogonal();
        var face = new Vector2(top.X, top.Z);
        for (int i = 0; i < 10; i++)
        {
            var at = face + facing * (3f + i % 3 * 2.5f) + across * (i - 4.5f) * 3.5f;
            float size = 2f + (i * 37 % 10) * 0.22f;
            var rock = Lumpy(new Vector3(size * 1.3f, size, size * 1.1f), 0.25f, i, Painted(), Granite);
            Solid(parent, $"Boulder{i}", rock, new Transform3D(new Basis(Vector3.Up, i * 1.3f), new Vector3(at.X, ground(at.X, at.Y) + size * 0.3f, at.Y)));
        }
    }

    /// <summary>
    /// The Balanced Stone: a huge boulder the retreating ice left perched on a narrow pillar of rock, as glacial
    /// erratics are, on top of a knoll. Three tall stones lean against one side, each higher than the last, so a good
    /// jumper can climb from one to the next and up onto the boulder's top.
    /// </summary>
    public static void BalancedStone(Node3D parent, Vector3 top, Vector2 facing, Func<float, float, float> ground)
    {
        var rock = Painted();
        // A slim pillar six metres high, and the boulder, far wider than it, perched on its top.
        Solid(parent, "Pedestal", Lumpy(new Vector3(1.7f, 3.8f, 1.7f), 0.15f, 20, rock, Granite), new Transform3D(Basis.Identity, top + Vector3.Up * 2.6f));
        Solid(parent, "Boulder", Lumpy(new Vector3(7f, 4.4f, 6f), 0.18f, 21, rock, Granite),
            new Transform3D(new Basis(new Vector3(facing.X, 0f, facing.Y), 0.12f), top + Vector3.Up * 10.4f));

        // Each stepping stone reaches from below the ground up to its own height; the boulder's top is near 15 m. They
        // stand round the back, so the boulder is seen perched alone from the front.
        var side = (facing.Orthogonal() - facing).Normalized();
        var steps = new[] { (Out: 13f, Top: 3.5f), (Out: 10f, Top: 6.5f), (Out: 7.2f, Top: 9.5f) };
        for (int i = 0; i < steps.Length; i++)
        {
            var at = new Vector2(top.X, top.Z) + side * steps[i].Out;
            float floor = ground(at.X, at.Y) - 0.6f;
            float peak = top.Y + steps[i].Top;
            float half = (peak - floor) / 2f;
            var stone = Lumpy(new Vector3(2.4f, half, 2.2f), 0.15f, 22 + i, rock, Granite);
            Solid(parent, $"Step{i}", stone, new Transform3D(new Basis(Vector3.Up, i * 0.8f), new Vector3(at.X, floor + half, at.Y)));
        }
    }

    /// <summary>Steam rising off a hot pool in soft, slow puffs that swell and fade as they drift up into the cold air.</summary>
    public static void Steam(Node3D parent, Vector3 surface, float radius)
    {
        var puff = new GradientTexture2D
        {
            Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new Vector2(0.5f, 0.5f),
            FillTo = new Vector2(1f, 0.5f),
            Width = 64,
            Height = 64,
            Gradient = new Gradient { Offsets = [0f, 1f], Colors = [new Color(1f, 1f, 1f, 1f), new Color(1f, 1f, 1f, 0f)] },
        };
        var material = new StandardMaterial3D
        {
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            BillboardMode = BaseMaterial3D.BillboardModeEnum.Particles,
            VertexColorUseAsAlbedo = true,
            AlbedoTexture = puff,
        };
        var grow = new Curve();
        grow.AddPoint(new Vector2(0f, 0.4f));
        grow.AddPoint(new Vector2(1f, 1f));
        parent.AddChild(new CpuParticles3D
        {
            Name = "Steam",
            Position = surface + Vector3.Up * 0.3f,
            Amount = Mathf.RoundToInt(radius * 10f),
            Lifetime = 7f,
            Preprocess = 7f,
            Mesh = new QuadMesh { Size = new Vector2(4f, 4f), Material = material },
            EmissionShape = CpuParticles3D.EmissionShapeEnum.Box,
            EmissionBoxExtents = new Vector3(radius * 0.7f, 0.1f, radius * 0.7f),
            Direction = Vector3.Up,
            Spread = 20f,
            InitialVelocityMin = 0.4f,
            InitialVelocityMax = 1f,
            Gravity = new Vector3(0.1f, 0.25f, 0f),
            ScaleAmountMin = 1f,
            ScaleAmountMax = 2.5f,
            ScaleAmountCurve = grow,
            ColorRamp = new Gradient
            {
                Offsets = [0f, 0.2f, 1f],
                Colors = [new Color(1f, 1f, 1f, 0f), new Color(0.95f, 0.97f, 1f, 0.6f), new Color(1f, 1f, 1f, 0f)],
            },
        });
    }

    /// <summary>
    /// The Ice Arch: a span of old glacier ice twenty metres across and thirteen high, left standing as the glacier
    /// melted back around it, thick at its feet and thinner over the top, with snow lying along its crown. Its legs are
    /// sheer, so only a climber gets up onto it. Blocks and pinnacles of ice stand round about.
    /// </summary>
    public static void IceArch(Node3D parent, Vector2 centre, Vector2 along, Func<float, float, float> ground, RandomNumberGenerator rng)
    {
        const float span = 22f, rise = 13f;
        const int steps = 44, sides = 18;
        var a = centre - along * span / 2f;
        var b = centre + along * span / 2f;
        float low = Mathf.Min(ground(a.X, a.Y), ground(b.X, b.Y)) - 2f;
        float high = Mathf.Max(ground(a.X, a.Y), ground(b.X, b.Y)) + rise;
        var across = new Vector3(along.Y, 0f, -along.X);

        Vector3 Centre(float t) => new(
            Mathf.Lerp(a.X, b.X, t),
            low + (high - low) * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.Pi * t)), 0.55f),
            Mathf.Lerp(a.Y, b.Y, t));

        var mesh = new MeshBuilder();
        var index = new int[steps + 1, sides + 1];
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            var point = Centre(t);
            var tangent = (Centre(Mathf.Min(1f, t + 0.01f)) - Centre(Mathf.Max(0f, t - 0.01f))).Normalized();
            var up = tangent.Cross(across).Normalized();
            // Sin(Pi) comes out a hair below zero, which Pow would turn into NaN.
            float arc = Mathf.Max(0f, Mathf.Sin(Mathf.Pi * t));
            float thick = Mathf.Lerp(4.5f, 2.4f, Mathf.Pow(arc, 1.5f));
            float wide = Mathf.Lerp(5.5f, 3.8f, arc);
            for (int s = 0; s <= sides; s++)
            {
                float angle = Mathf.Tau * s / sides;
                float cx = Boxy(Mathf.Cos(angle), 0.6f), cy = Boxy(Mathf.Sin(angle), 0.6f);
                var offset = across * wide * cx + up * thick * cy;
                var vertex = point + offset * (1f + 0.08f * Lumps.GetNoise3Dv(point * 0.2f + offset));
                var normal = (across * cx / wide + up * cy / thick).Normalized();
                index[i, s] = mesh.Add(vertex, normal, Ice(vertex, normal, (vertex.Y - low) / (high - low)));
            }
        }
        for (int i = 0; i < steps; i++)
        for (int s = 0; s < sides; s++)
            mesh.Quad(index[i, s], index[i, s + 1], index[i + 1, s + 1], index[i + 1, s]);
        Solid(parent, "Arch", mesh.Commit(Painted(0.15f, 0.1f)), Transform3D.Identity);

        // Seracs: leaning pinnacles and blocks of ice, all that is left of the rest of the glacier.
        var material = Painted(0.15f, 0.1f);
        for (int i = 0; i < 7; i++)
        {
            Vector2 at;
            do
                at = centre + Vector2.Right.Rotated(rng.Randf() * Mathf.Tau) * rng.RandfRange(9f, 22f);
            // Off beyond the feet, out of the way through the arch.
            while (Mathf.Abs((at - centre).Dot(along)) < span / 2f + 2f);
            float tall = rng.RandfRange(2f, 6f);
            var radii = new Vector3(rng.RandfRange(1.5f, 3f), tall, rng.RandfRange(1.5f, 2.5f));
            var block = Lumpy(radii, 0.25f, 40 + i, material, (p, n) => Ice(p, n, (p.Y + tall) / (2f * tall)));
            var tilt = new Basis(Vector3.Up, rng.Randf() * Mathf.Tau) * new Basis(Vector3.Right, rng.RandfRange(-0.2f, 0.2f));
            Solid(parent, $"Serac{i}", block, new Transform3D(tilt, new Vector3(at.X, ground(at.X, at.Y) + tall * 0.6f, at.Y)));
        }
    }

    /// <summary>
    /// The Pillars: hundreds of six-sided columns of black basalt packed side by side, as lava cracks when it cools
    /// slowly, broken off at different heights so they rise in a rough stair to a crest at the back. Each is solid, so
    /// an animal can hop from column to column and climb to the top; snow lies on their flat tops.
    /// </summary>
    public static void Pillars(Node3D parent, Vector2 centre, Vector2 facing, float radius, Func<float, float, float> ground, RandomNumberGenerator rng)
    {
        const float corner = 1.25f;
        float flat = corner * Mathf.Sqrt(3f);
        // The crest stands at the back, away from the start, so the stair climbs as a visitor walks in.
        var crest = centre - facing * radius * 0.35f;
        float floor = ground(centre.X, centre.Y);

        var column = HexColumn();
        var placements = new List<(Transform3D Transform, Color Tint)>();
        var body = new StaticBody3D { Name = "Columns" };
        int reach = Mathf.CeilToInt(radius / flat) + 1;
        var e1 = new Vector2(Mathf.Cos(Mathf.Pi / 6f), Mathf.Sin(Mathf.Pi / 6f)) * flat;
        var e2 = new Vector2(0f, flat);
        for (int q = -reach * 2; q <= reach * 2; q++)
        for (int r = -reach * 2; r <= reach * 2; r++)
        {
            var at = centre + e1 * q + e2 * r;
            var offset = at - centre;
            float edge = radius * (1f + 0.18f * Lumps.GetNoise2D(Mathf.Atan2(offset.Y, offset.X) * 3f, 0f));
            if (offset.Length() > edge || rng.Randf() < 0.05f)
                continue;

            // Broken off in steps of about knee height, so neighbours differ by a hop.
            float rise = 12f * Mathf.Pow(Mathf.Clamp(1f - at.DistanceTo(crest) / (radius * 1.25f), 0f, 1f), 1.2f);
            rise = Mathf.Snapped(rise + rng.RandfRange(0f, 0.7f), 0.45f);
            float g = ground(at.X, at.Y);
            float topY = Mathf.Max(floor + 0.3f + rise, g + 0.3f);
            float baseY = g - 1.5f;
            float height = topY - baseY;

            float tint = rng.RandfRange(0.85f, 1.15f);
            placements.Add((new Transform3D(Basis.FromScale(new Vector3(corner * 0.98f, height, corner * 0.98f)), new Vector3(at.X, baseY, at.Y)),
                new Color(tint, tint, tint)));
            body.AddChild(new CollisionShape3D
            {
                Shape = new CylinderShape3D { Radius = corner * 0.92f, Height = height },
                Position = new Vector3(at.X, baseY + height / 2f, at.Y),
            });
        }

        var multi = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            UseColors = true,
            Mesh = column,
            InstanceCount = placements.Count,
        };
        for (int i = 0; i < placements.Count; i++)
        {
            multi.SetInstanceTransform(i, placements[i].Transform);
            multi.SetInstanceColor(i, placements[i].Tint);
        }
        parent.AddChild(new MultiMeshInstance3D { Name = "Basalt", Multimesh = multi });
        parent.AddChild(body);
    }

    /// <summary>One basalt column: a six-sided prism a unit across its corners and a unit tall, its top white with snow.</summary>
    private static ArrayMesh HexColumn()
    {
        var mesh = new MeshBuilder();
        var corners = new Vector3[6];
        for (int k = 0; k < 6; k++)
            corners[k] = new Vector3(Mathf.Cos(k * Mathf.Pi / 3f), 0f, Mathf.Sin(k * Mathf.Pi / 3f));

        int middle = mesh.Add(Vector3.Up, Vector3.Up, SnowWhite);
        var rim = new int[6];
        for (int k = 0; k < 6; k++)
            rim[k] = mesh.Add(corners[k] + Vector3.Up, Vector3.Up, SnowWhite);
        for (int k = 0; k < 6; k++)
            mesh.Tri(middle, rim[(k + 1) % 6], rim[k]);

        for (int k = 0; k < 6; k++)
        {
            var a = corners[k];
            var b = corners[(k + 1) % 6];
            var normal = ((a + b) / 2f).Normalized();
            // A rim of snow just over the edge, then dark rock all the way down.
            int a0 = mesh.Add(a, normal, Basalt), b0 = mesh.Add(b, normal, Basalt);
            int a1 = mesh.Add(a + Vector3.Up, normal, Basalt.Lightened(0.15f)), b1 = mesh.Add(b + Vector3.Up, normal, Basalt.Lightened(0.15f));
            mesh.Quad(a0, a1, b1, b0);
        }
        return mesh.Commit(Painted(0.85f));
    }
}
