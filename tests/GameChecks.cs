using System;
using System.Linq;
using System.Reflection;
using Godot;
using JurassicWorld;

/// <summary>Engine integration checks: actual models, selector, held controls, DPS, switching and network poses.</summary>
public partial class GameChecks : Node3D
{
    private int _assertions;
    public override void _Ready() => Callable.From(Run).CallDeferred();
    private void Check(bool condition,string message)
    {
        if(!condition) throw new InvalidOperationException(message);
        _assertions++;
    }
    private void Run()
    {
        try
        {
            var main=GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<Node3D>(); AddChild(main);
            var player=main.GetNode<Player>("Player");
            var wildlife=main.GetNode<Wildlife>("Wildlife");
            wildlife.ProcessMode=ProcessModeEnum.Disabled;
            player.ProcessMode=ProcessModeEnum.Disabled;
            Check(player.Animals.Count==9,"Exactly nine selectable dinosaurs");
            Check(player.Animals.Select(a=>a.GetType()).Distinct().Count()==9,"No duplicate roster entries");
            int[] health=[1000,1800,350,2000,1600,900,1750,300,500];
            int[] stamina=[8,7,9,8,6,7,8,8,9];
            int[] basic=[20,10,10,20,12,12,15,8,15];
            int[] normal=[50,25,15,45,30,28,35,20,28];
            int[] special=[90,90,40,100,85,70,85,50,65];
            for(int i=0;i<9;i++)
            {
                var a=player.Animals[i]; var s=a.Stats;
                Check(s.MaxHealth==health[i]&&s.Scores.Stamina==stamina[i],a.DisplayName+" health/stamina");
                Check(s.BasicDps==basic[i]&&s.NormalDps==normal[i]&&s.SpecialDps==special[i],a.DisplayName+" DPS");
                Check(a.FindChildren("*","MeshInstance3D",true,false).Count>0,a.DisplayName+" has a 3D model");
                player.SwitchTo(i); Check(player.AnimalIndex==i,a.DisplayName+" switches without payment");
                if(s.Scores.WaterSpeed==0) Check(s.SwimSpeed>0&&s.SwimSpeed<=0.5f,"Zero-water score is slow swimming");
                if(s.Scores.JumpHeight==0) Check(s.JumpHeight==0,"Zero-jump score cannot jump");
            }
            player.SwitchTo(0);
            var raptor=(Velociraptor)player.Animal;
            foreach(var form in Enum.GetValues<RaptorColour>())
            {
                player.ChooseRaptor(form); Check(raptor.Colour==form,form+" appearance applies");
                var remote=new RemotePlayer(); AddChild(remote);
                remote.Receive(0,Vector3.Zero,Vector3.Zero,Vector3.Zero,RemotePlayer.Pack(player)); remote._Process(0.05);
                Check(remote.Animal is Velociraptor r&&r.Colour==form,form+" replicates to friends");
                remote.QueueFree();
            }
            var target=wildlife.CombatTargets(Vector3.Zero,2000).First(a=>a is Stegosaurus);
            var body=target.GetParent<Node3D>();
            var beasts=(System.Collections.IDictionary)typeof(Wildlife).GetField("_beasts",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(wildlife)!;
            var beast=beasts[target]!; var hp=beast.GetType().GetField("Health")!;
            var combat=typeof(Player).GetMethod("UpdateCombat",BindingFlags.NonPublic|BindingFlags.Instance)!;
            Input.MouseMode=Input.MouseModeEnum.Captured;
            player.GlobalPosition=new Vector3(0,30,0);
            // Isolate the test target from other wildlife.
            foreach(var other in wildlife.CombatTargets(Vector3.Zero,2000).Where(a=>a!=target).ToArray()) other.GetParent<Node3D>().GlobalPosition+=new Vector3(1000,0,1000);
            for(int kind=0;kind<9;kind++)
            {
                player.SwitchTo(kind);
                for(int tier=0;tier<3;tier++)
                {
                    foreach(int hz in new[]{30,60,120})
                    {
                        player.CancelAttack(); hp.SetValue(beast,100f);
                        player.Animal.Rotation=Vector3.Zero;
                        bool tail=(kind is 1 or 4&&tier<2)||(kind is 5 or 6&&tier==0);
                        body.GlobalPosition=player.GlobalPosition+new Vector3(0,0,tail?0.8f:-0.8f);
                        string action=tier==0?InputSetup.Attack:tier==1?InputSetup.NormalAttack:InputSetup.Grip;
                        Input.ActionPress(action);
                        for(int frame=0;frame<hz;frame++)
                        {
                            body.GlobalPosition=player.GlobalPosition+new Vector3(0,0,tail?0.8f:-0.8f);
                            combat.Invoke(player,[1f/hz]);
                        }
                        Input.ActionRelease(action); player.CancelAttack();
                        float actual=(100-(float)hp.GetValue(beast)!)*target.Stats.MaxHealth/100;
                        float expected=tier==0?basic[kind]:tier==1?normal[kind]:special[kind];
                        // A ten-second Triceratops charge has a deliberate recovery period between uses.
                        if(kind!=6||tier!=2||hz==30) Check(Mathf.Abs(actual-expected)<0.15f,$"{player.Animal.DisplayName} tier {tier} at {hz} Hz: {actual} vs {expected}");
                    }
                }
            }
            player.Reborn(3,Sex.Female);
            Check(!player.IsDead&&player.Age==0&&player.Sex==Sex.Female&&player.Animal is Tyrannosaurus,"Death/rebirth resets dinosaur, age, sex and health");
            GD.Print($"GAME CHECKS PASSED: {_assertions} assertions"); GetTree().Quit();
        }
        catch(Exception e) { GD.PushError(e.ToString()); GetTree().Quit(1); }
    }
}
