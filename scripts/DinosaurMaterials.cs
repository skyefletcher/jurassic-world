using Godot;

namespace JurassicWorld;

/// <summary>One shared shader program; each animal retains its own skin colours.</summary>
public static class DinosaurMaterials
{
    private static Shader? _skinShader;
    public static ShaderMaterial Skin(Color colour, float height, bool raptor = false)
    {
        _skinShader ??= GD.Load<Shader>("res://shaders/dinosaur_skin.gdshader");
        var skin = new ShaderMaterial { Shader = _skinShader };
        skin.SetShaderParameter("hide_colour", colour);
        skin.SetShaderParameter("body_height", height);
        skin.SetShaderParameter("raptor", raptor);
        return skin;
    }
}
