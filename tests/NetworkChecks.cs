using System;
using System.Linq;
using Godot;
using JurassicWorld;

public partial class NetworkChecks : Node3D
{
    private Player _player=null!;
    private Network _network=null!;
    private bool _server, _struck, _passed;
    private float _time;
    public override void _Ready()
    {
        InputSetup.Register();
        _server=OS.GetCmdlineUserArgs().Contains("--server");
        _player=GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>(); AddChild(_player);
        _player.SetPhysicsProcess(false);
        _player.ChooseRaptor(_server?RaptorColour.Blue:RaptorColour.Charlie);
        _player.Animal.AttackTier=2;
        _network=new Network { Name="Network",Player=_player,PlayerName=_server?"Host":"Guest" }; AddChild(_network);
        bool connected=_server?_network.Host():_network.Join("127.0.0.1");
        if(!connected) Fail("Could not host/join localhost");
    }
    public override void _Process(double delta)
    {
        _time+=(float)delta;
        var other=_network.Others.FirstOrDefault();
        if(other?.Animal is Velociraptor raptor&&_time>1&&!_struck)
        {
            if(raptor.Colour!=(_server?RaptorColour.Charlie:RaptorColour.Blue)||raptor.AttackTier!=2) { Fail("Appearance/attack pose did not replicate"); return; }
            _network.Strike(other,40,Vector3.Zero,0); _struck=true;
        }
        if(_struck&&Mathf.Abs(_player.HitPoints-960)<0.1f)
        {
            if (!_passed) GD.Print($"NETWORK CHECK PASSED ({(_server?"host":"guest")}): joined, replicated raptor/attack, received 40 HP damage");
            _passed=true;
            // Give both peers time to observe their received damage before either disconnects.
            if(_time>3) GetTree().Quit();
        }
        if(_time>18) Fail($"Timed out: peer={other?.Animal?.DisplayName}, HP={_player.HitPoints}");
    }
    private void Fail(string message) { GD.PushError(message); GetTree().Quit(1); }
}
