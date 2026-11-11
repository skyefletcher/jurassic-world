using System;
using System.Collections.Generic;
using Godot;

namespace JurassicWorld;

/// <summary>Dinosaur selection. Every kind is available; there is no currency or unlocking.</summary>
public partial class AnimalShop : ColorRect
{
    public Player? Player { get; set; }
    public bool CanClose { get; private set; }
    private Label _title = null!, _subtitle = null!;
    private Button _close = null!;
    private readonly Dictionary<Sex,Button> _sexes = [];
    private readonly Dictionary<RaptorColour,Button> _colours = [];
    private Action<int>? _chosen;

    public override void _Ready()
    {
        Color=new Color(0.025f,0.07f,0.055f,1f); Visible=false;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var centre=new CenterContainer(); AddChild(centre); centre.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var column=new VBoxContainer { CustomMinimumSize=new Vector2(850,0) };
        column.AddThemeConstantOverride("separation",14); centre.AddChild(column);
        _title=Hud.Text("",36); column.AddChild(_title);
        _subtitle=Hud.Text("",18); column.AddChild(_subtitle);
        var sexRow=new HBoxContainer { Alignment=BoxContainer.AlignmentMode.Center }; column.AddChild(sexRow);
        foreach(var sex in Enum.GetValues<Sex>())
        {
            var button=Choice(sex.ToString()); sexRow.AddChild(button); _sexes[sex]=button;
            button.Pressed+=()=>{ Player!.BecomeSex(sex); Refresh(); };
        }
        column.AddChild(Hud.Text("Velociraptor appearance",20));
        var colours=new HBoxContainer { Alignment=BoxContainer.AlignmentMode.Center }; column.AddChild(colours);
        foreach(var colour in Enum.GetValues<RaptorColour>())
        {
            var button=Choice(colour.ToString()); colours.AddChild(button); _colours[colour]=button;
            button.Pressed+=()=>{ Player!.ChooseRaptor(colour); Refresh(); };
        }
        var grid=new GridContainer { Columns=3 }; grid.AddThemeConstantOverride("h_separation",12); grid.AddThemeConstantOverride("v_separation",12); column.AddChild(grid);
        for(int i=0;i<Player!.Animals.Count;i++)
        {
            int index=i; var animal=Player.Animals[i]; var stats=animal.Stats;
            var button=new Button { CustomMinimumSize=new Vector2(270,86), Text=$"{animal.DisplayName}\n{stats.MaxHealth} HP · {stats.BasicDps}/{stats.NormalDps}/{stats.SpecialDps} DPS" };
            button.AddThemeFontSizeOverride("font_size",19);
            button.TooltipText=$"Jump {stats.Scores.JumpHeight}/10 · Length {stats.Scores.JumpLength}/10\nLand {stats.Scores.LandSpeed}/10 · Water {stats.Scores.WaterSpeed}/10 · Agility {stats.Scores.Agility}/10 · Stamina {stats.Scores.Stamina}/10\nF: {stats.BasicAttack}\nQ: {stats.NormalAttack}\nG: {stats.SpecialAttack}";
            button.Pressed+=()=>{ var chosen=_chosen; Close(); chosen?.Invoke(index); };
            grid.AddChild(button);
        }
        _close=new Button { Text="Return to the world (B)",CustomMinimumSize=new Vector2(0,42) }; _close.Pressed+=Close; column.AddChild(_close);
    }
    private static Button Choice(string text) => new() { Text=text,ToggleMode=true,CustomMinimumSize=new Vector2(140,38) };
    public void Open(string title,string subtitle,bool canClose,Action<int> chosen)
    {
        _title.Text=title; _subtitle.Text=subtitle; CanClose=canClose; _close.Visible=canClose; _chosen=chosen;
        Visible=true; Refresh(); Input.MouseMode=Input.MouseModeEnum.Visible;
    }
    public void Close() { Visible=false; _chosen=null; Input.MouseMode=Input.MouseModeEnum.Captured; }
    private void Refresh()
    {
        foreach(var (sex,button) in _sexes) button.SetPressedNoSignal(Player!.Sex==sex);
        foreach(var (colour,button) in _colours) button.SetPressedNoSignal(Player!.RaptorForm==colour);
    }
    public override void _UnhandledInput(InputEvent e)
    {
        if(Visible)
        {
            if(CanClose&&(e.IsActionPressed(InputSetup.Shop)||e.IsActionPressed("ui_cancel"))) { Close(); GetViewport().SetInputAsHandled(); }
        }
        else if(e.IsActionPressed(InputSetup.Shop)&&Player!.CanProcess()&&!Player.IsDead)
        {
            Player.CancelAttack();
            Open("Choose your dinosaur","All nine dinosaurs are available. Hover a dinosaur to see its abilities.",true,Player.SwitchTo);
            GetViewport().SetInputAsHandled();
        }
    }
}
