using System;
using System.Linq;
using Godot;

namespace JurassicWorld;

/// <summary>The shared approved skeletal model with independent skins for all four raptors.</summary>
public partial class Velociraptor : Dinosaur
{
    public override DinosaurKind Kind => DinosaurKind.Velociraptor;
    private const float WalkReference = 2.24516f, RunReference = 4.794f;
    private Node3D? _model, _legacy;
    private MeshInstance3D? _skinMesh;
    private StandardMaterial3D? _blueSkin, _deltaSkin, _echoSkin, _charlieSkin;
    private Skeleton3D? _skeleton;
    private AnimationPlayer? _player;
    private AnimationTree? _tree;
    private AnimationNodeAnimation? _action;
    private int _head = -1, _jaw = -1, _root = -1;
    private float _look, _actionBlend;
    private bool _headTurned;
    private Quaternion _beforeLook;
    private string _clip = "";
    public bool UsingImportedModel => _model is not null && (Colour is RaptorColour.Blue or RaptorColour.Delta or RaptorColour.Echo or RaptorColour.Charlie);
    public string CurrentAnimation { get; private set; } = "Idle";

    public override void _Ready()
    {
        base._Ready();
        _legacy = GetNode<Node3D>("Frame");
        _model = GD.Load<PackedScene>("res://assets/raptor/raptor.glb").Instantiate<Node3D>();
        _model.Name = "ApprovedRaptor";
        _model.Position = new Vector3(0, 0.02f, 0);
        _model.Rotation = new Vector3(0, Mathf.Pi, 0);
        AddChild(_model);
        _skinMesh = _model.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>().Single();
        _blueSkin = (StandardMaterial3D)_skinMesh.GetActiveMaterial(0);
        _deltaSkin = (StandardMaterial3D)_blueSkin.Duplicate();
        _deltaSkin.AlbedoTexture = GD.Load<Texture2D>("res://assets/raptor/delta-body.png");
        _echoSkin = (StandardMaterial3D)_blueSkin.Duplicate();
        _echoSkin.AlbedoTexture = GD.Load<Texture2D>("res://assets/raptor/echo-body.png");
        _charlieSkin = (StandardMaterial3D)_blueSkin.Duplicate();
        _charlieSkin.AlbedoTexture = GD.Load<Texture2D>("res://assets/raptor/charlie-body.png");
        _player = _model.FindChildren("*", "AnimationPlayer", true, false).OfType<AnimationPlayer>().Single();
        _skeleton = _model.FindChildren("*", "Skeleton3D", true, false).OfType<Skeleton3D>().Single();
        _head = _skeleton.FindBone("Head");
        _jaw = _skeleton.FindBone("Jaw_2_end");
        _root = _skeleton.FindBone("Root");
        foreach (string clip in new[] { "Idle", "Walk", "Run", "Bite", "BiteHeavy", "Scratch", "Eat", "Drink", "Sleep" })
            _player.GetAnimation(clip).LoopMode = Animation.LoopModeEnum.Linear;

        _tree = new AnimationTree { CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual };
        _model.AddChild(_tree);
        _tree.AnimPlayer = _tree.GetPathTo(_player);
        var graph = new AnimationNodeBlendTree();
        AnimationNodeAnimation Clip(string name) => new() { Animation = name };
        graph.AddNode("Idle", Clip("Idle"));
        graph.AddNode("Walk", Clip("Walk"));
        graph.AddNode("Run", Clip("Run"));
        graph.AddNode("WalkRate", new AnimationNodeTimeScale());
        graph.AddNode("RunRate", new AnimationNodeTimeScale());
        graph.AddNode("IdleWalk", new AnimationNodeBlend2 { Sync = true });
        graph.AddNode("WalkRun", new AnimationNodeBlend2 { Sync = true });
        _action = Clip("Bite");
        graph.AddNode("Action", _action);
        graph.AddNode("ActionSeek", new AnimationNodeTimeSeek());
        graph.AddNode("ActionBlend", new AnimationNodeBlend2());
        graph.ConnectNode("WalkRate", 0, "Walk");
        graph.ConnectNode("RunRate", 0, "Run");
        graph.ConnectNode("IdleWalk", 0, "Idle");
        graph.ConnectNode("IdleWalk", 1, "WalkRate");
        graph.ConnectNode("WalkRun", 0, "IdleWalk");
        graph.ConnectNode("WalkRun", 1, "RunRate");
        graph.ConnectNode("ActionSeek", 0, "Action");
        graph.ConnectNode("ActionBlend", 0, "WalkRun");
        graph.ConnectNode("ActionBlend", 1, "ActionSeek");
        graph.ConnectNode("output", 0, "ActionBlend");
        _tree.TreeRoot = graph;
        SetColour(Colour);
        Animate(0, 0, 0, 0);
    }

    public override void SetColour(RaptorColour colour)
    {
        base.SetColour(colour);
        if (_model is null || _legacy is null || _tree is null) return;
        bool imported = colour is RaptorColour.Blue or RaptorColour.Delta or RaptorColour.Echo or RaptorColour.Charlie;
        var material = colour switch
        {
            RaptorColour.Delta => _deltaSkin,
            RaptorColour.Echo => _echoSkin,
            RaptorColour.Charlie => _charlieSkin,
            _ => _blueSkin,
        };
        if (_skinMesh is not null && _skinMesh.GetSurfaceOverrideMaterial(0) != material)
            _skinMesh.SetSurfaceOverrideMaterial(0, material);
        _legacy.Visible = !imported;
        _model.Visible = imported;
        _tree.Active = imported;
    }

    public override void Animate(float speed, float stride, float eat, float dt)
    {
        if (!UsingImportedModel || _tree is null || _action is null || _player is null || _skeleton is null)
        {
            base.Animate(speed, stride, eat, dt);
            return;
        }
        if (_headTurned && _head >= 0)
        {
            _skeleton.SetBonePoseRotation(_head, _beforeLook);
            _headTurned = false;
        }
        float scale = Mathf.Max(0.05f, Mathf.Abs(GlobalBasis.Scale.X));
        float localSpeed = Mathf.Max(0, speed) / scale;
        float walking = Mathf.SmoothStep(0, 1, Mathf.Clamp(localSpeed / 0.5f, 0, 1));
        float running = Mathf.SmoothStep(0, 1, Mathf.Clamp((localSpeed - WalkReference) / (RunReference - WalkReference), 0, 1));
        _tree.Set("parameters/WalkRate/scale", Mathf.Max(0.05f, localSpeed / WalkReference));
        _tree.Set("parameters/RunRate/scale", Mathf.Max(0.05f, localSpeed / RunReference));
        _tree.Set("parameters/IdleWalk/blend_amount", walking);
        _tree.Set("parameters/WalkRun/blend_amount", running);

        string action = "";
        float weight = 0, seek = -1;
        float rest = Mathf.Max(Lying, Sitting * 0.65f);
        if (Dead > 0.01f) { action = "Death"; weight = Dead; }
        else if (rest > 0.005f)
        {
            action = Lying >= 0.995f ? "Sleep" : "GoToSleep";
            weight = 1;
            if (action == "GoToSleep")
                seek = rest * (float)_player.GetAnimation(action).Length;
        }
        else if (AttackTier >= 0)
        {
            action = AttackTier == 0 ? "Bite" : AttackTier == 1 ? "Scratch" : "BiteHeavy";
            weight = 1;
        }
        else if (eat > 0.01f) { action = Meal == Food.Water ? "Drink" : "Eat"; weight = eat; }
        else if (IsAirborne && !IsSwimming) { action = "Leap"; weight = 1; }

        if (action.Length > 0 && action != _clip)
        {
            _action.Animation = action;
            _tree.Set("parameters/ActionSeek/seek_request", 0.0);
            _clip = action;
        }
        if (seek >= 0) _tree.Set("parameters/ActionSeek/seek_request", seek);
        _actionBlend = Mathf.MoveToward(_actionBlend, weight, dt * 7f);
        _tree.Set("parameters/ActionBlend/blend_amount", _actionBlend);
        CurrentAnimation = action.Length > 0 ? action : localSpeed < 0.1f ? "Idle" : running > 0.5f ? "Run" : "Walk";
        _tree.Advance(seek >= 0 ? 0 : dt);
        if (action.Length == 0 && _actionBlend <= 0.001f) _clip = "";

        // Gameplay owns horizontal travel and jump height, including in the source's death/leap clips.
        if (_root >= 0)
        {
            var position = _skeleton.GetBonePosePosition(_root);
            _skeleton.SetBonePosePosition(_root, new Vector3(0, IsAirborne ? 0 : position.Y, 0));
        }
    }

    public override void LookToward(Vector3 direction, float feeding, float dt)
    {
        if (!UsingImportedModel || _skeleton is null || _head < 0)
        {
            base.LookToward(direction, feeding, dt);
            return;
        }
        var forward = -GlobalBasis.Z with { Y = 0 };
        direction.Y = 0;
        float target = direction.LengthSquared() > 0.01f && forward.LengthSquared() > 0.01f
            ? Mathf.Clamp(forward.SignedAngleTo(direction, Vector3.Up), -0.8f, 0.8f) : 0;
        target *= (1 - Mathf.Clamp(feeding, 0, 1)) * Alive * (IsResting ? 0 : 1);
        _look = Mathf.Lerp(_look, target, 1 - Mathf.Exp(-dt / 0.15f));
        int parent = _skeleton.GetBoneParent(_head);
        var parentBasis = parent < 0 ? Basis.Identity : _skeleton.GetBoneGlobalPose(parent).Basis;
        var up = parentBasis.Orthonormalized().Inverse() * (_skeleton.GlobalBasis.Orthonormalized().Inverse() * Vector3.Up);
        if (_headTurned) _skeleton.SetBonePoseRotation(_head, _beforeLook);
        _beforeLook = _skeleton.GetBonePoseRotation(_head);
        _skeleton.SetBonePoseRotation(_head, new Quaternion(up.Normalized(), _look) * _beforeLook);
        _headTurned = true;
    }

    public override Vector3 Bite => UsingImportedModel && _skeleton is not null && _jaw >= 0
        ? _skeleton.GlobalTransform * _skeleton.GetBoneGlobalPose(_jaw).Origin : base.Bite;
}
