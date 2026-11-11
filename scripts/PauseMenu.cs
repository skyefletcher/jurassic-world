using Godot;

namespace JurassicWorld;

/// <summary>Freezes solo play while keeping the resume controls available.</summary>
public partial class PauseMenu : CanvasLayer
{
    public Player Player { get; set; } = null!;
    public AnimalShop Shop { get; set; } = null!;
    public Network Network { get; set; } = null!;

    private ColorRect _shade = null!;
    private Label _message = null!;
    private Button _resume = null!;
    private Input.MouseModeEnum _previousMouseMode;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        Layer = 10;
        _shade = new ColorRect { Color = new Color(0, 0, 0, 0.7f), Visible = false };
        AddChild(_shade);
        _shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var centre = new CenterContainer();
        _shade.AddChild(centre);
        centre.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var column = new VBoxContainer { CustomMinimumSize = new Vector2(440, 0) };
        column.AddThemeConstantOverride("separation", 24);
        centre.AddChild(column);
        _message = Hud.Text("", 28);
        _message.HorizontalAlignment = HorizontalAlignment.Center;
        column.AddChild(_message);
        _resume = new Button { Text = "Resume (Esc)", CustomMinimumSize = new Vector2(0, 56) };
        _resume.AddThemeFontSizeOverride("font_size", 24);
        _resume.Pressed += Resume;
        column.AddChild(_resume);
    }

    public override void _Input(InputEvent @event)
    {
        if (!@event.IsActionPressed("ui_cancel"))
            return;
        if (_shade.Visible)
            Resume();
        else
        {
            // Escape still closes the shop, and cannot skip the initial animal choice.
            if (!Player.CanProcess() || Shop.Visible)
                return;
            _previousMouseMode = Input.MouseMode;
            Player.CancelAttack();
            _message.Text = Network.IsOnline
                ? "Game menu\nOnline play keeps running."
                : "Paused";
            GetTree().Paused = !Network.IsOnline;
            _shade.Show();
            Input.MouseMode = Input.MouseModeEnum.Visible;
            _resume.GrabFocus();
        }
        GetViewport().SetInputAsHandled();
    }

    private void Resume()
    {
        _shade.Hide();
        GetTree().Paused = false;
        Input.MouseMode = _previousMouseMode;
    }
}
