using Godot;

namespace JurassicWorld;

/// <summary>A shared cod model for schools and the eagle's catch, facing -Z.</summary>
public static class FishBuilder
{
    public static ArrayMesh Build()
    {
        var material = new StandardMaterial3D { VertexColorUseAsAlbedo = true, Metallic = 0.35f, Roughness = 0.38f };
        var body = Animal.Ellipsoid(u =>
        {
            float taper = 1f - Mathf.SmoothStep(-0.3f, 1f, u.Z) * 0.8f;
            return new Vector3(u.X * 0.027f * taper, u.Y * 0.043f * taper, u.Z * 0.14f);
        }, 24, 14, material, (p, _) =>
        {
            var colour = new Color(0.8f, 0.85f, 0.83f).Lerp(new Color(0.16f, 0.29f, 0.27f), Mathf.SmoothStep(-0.025f, 0.035f, p.Y));
            float scales = Mathf.Sin(p.Z * 650f + Mathf.Sin(p.Y * 550f) * 1.8f) * Mathf.Sin(p.Y * 550f);
            return colour.Lightened(scales * 0.07f);
        });
        var details = new MeshBuilder();
        var fin = new Color(0.28f, 0.41f, 0.37f);
        void Triangle(Vector3 a, Vector3 b, Vector3 c, Color colour)
        {
            var normal = (b - a).Cross(c - a).Normalized();
            int start = details.Add(a, normal, colour);
            details.Add(b, normal, colour); details.Add(c, normal, colour);
            details.Tri(start, start + 1, start + 2);
            start = details.Add(a, -normal, colour);
            details.Add(c, -normal, colour); details.Add(b, -normal, colour);
            details.Tri(start, start + 1, start + 2);
        }
        void Fan(Vector3 root, Vector3 a, Vector3 b)
        {
            for (int i = 0; i < 8; i++)
                Triangle(root, a.Lerp(b, i / 8f), a.Lerp(b, (i + 1) / 8f), i % 2 == 0 ? fin : fin.Lightened(0.16f));
        }
        Fan(new Vector3(0, 0, 0.12f), new Vector3(0, 0.048f, 0.19f), new Vector3(0, 0.004f, 0.17f));
        Fan(new Vector3(0, 0, 0.12f), new Vector3(0, -0.004f, 0.17f), new Vector3(0, -0.048f, 0.19f));
        for (int i = 0; i < 3; i++)
        {
            float z = -0.065f + i * 0.052f;
            Fan(new Vector3(0, 0.025f, z + 0.035f), new Vector3(0, 0.059f - i * 0.008f, z), new Vector3(0, 0.025f, z - 0.018f));
        }
        foreach (float side in new[] { -1f, 1f })
        {
            Fan(new Vector3(side * 0.022f, -0.005f, -0.065f), new Vector3(side * 0.065f, -0.026f, -0.015f), new Vector3(side * 0.03f, -0.025f, 0.005f));
            var eye = Animal.Ellipsoid(u => u * new Vector3(0.003f, 0.006f, 0.006f) + new Vector3(side * 0.019f, 0.015f, -0.102f),
                12, 8, material, (_, _) => new Color(0.035f, 0.045f, 0.04f));
            var append = new SurfaceTool();
            append.Begin(Mesh.PrimitiveType.Triangles);
            append.AppendFrom(eye, 0, Transform3D.Identity);
            append.SetMaterial(material);
            append.Commit(body);
            // Dark gill seam just behind each eye.
            for (int i = 0; i < 5; i++)
            {
                float y = -0.023f + i * 0.009f;
                Triangle(new Vector3(side * 0.026f, y, -0.071f), new Vector3(side * 0.026f, y + 0.009f, -0.071f),
                    new Vector3(side * 0.027f, y + 0.004f, -0.067f), fin.Darkened(0.4f));
            }
        }
        return details.Commit(material, body);
    }
}
