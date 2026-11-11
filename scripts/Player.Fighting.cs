using System;
using System.Linq;
using Godot;

namespace JurassicWorld;

/// <summary>Three held attack controls, integrated as HP/second, with one active attack at a time.</summary>
public partial class Player
{
    public Network? Network { get; set; }
    public RaptorColour RaptorForm { get; private set; } = RaptorColour.Blue;
    public string DinosaurName => Animal is Velociraptor ? $"Velociraptor ({RaptorForm})" : Animal.DisplayName;
    public float HitPoints => Health * Animal.Stats.MaxHealth / 100f;
    public string CombatHelp => $"{HitPoints:0}/{Animal.Stats.MaxHealth} HP  ·  Hold F: {Stats.BasicAttack} ({Stats.BasicDps}/s)\nQ: {Stats.NormalAttack} ({Stats.NormalDps}/s)  ·  G: {Stats.SpecialAttack} ({Stats.SpecialDps}/s)";
    private Animal? _specialTarget;
    private bool _targetLifted;
    private float _chargeRemaining, _chargeCooldown, _stunned, _attackSeconds;
    private Vector3 _knockback, _chargeDirection;
    private int _lastAttack = -1;

    public void ChooseRaptor(RaptorColour form)
    {
        RaptorForm=form;
        foreach(var raptor in _animals.OfType<Velociraptor>()) raptor.SetColour(form);
        var config=new ConfigFile(); config.SetValue("raptor","appearance",(int)form); config.Save("user://raptor.cfg");
    }
    private void LoadRaptor()
    {
        var config=new ConfigFile();
        if(config.Load("user://raptor.cfg")==Error.Ok)
            RaptorForm=(RaptorColour)Math.Clamp((int)config.GetValue("raptor","appearance",0),0,3);
        foreach(var raptor in _animals.OfType<Velociraptor>()) raptor.SetColour(RaptorForm);
    }
    public void CancelAttack() => StopCombat();
    private void StopCombat()
    {
        ReleaseSpecial(); _chargeRemaining=0; _lastAttack=-1; _attackSeconds=0;
        if(_animals.Length>0) Animal.AttackTier=-1;
    }
    private void ReleaseSpecial()
    {
        if(_specialTarget is not null)
        {
            if(_targetLifted) Wildlife?.Lift(_specialTarget,null);
            else Wildlife?.Grip(_specialTarget,false,pins:true);
        }
        _specialTarget=null; _targetLifted=false;
    }

    private void UpdateCombat(float dt)
    {
        _chargeCooldown=Mathf.Max(0,_chargeCooldown-dt);
        _stunned=Mathf.Max(0,_stunned-dt);
        if(IsDead||IsExhausted||IsFeeding||Posture!=Posture.Standing||_stunned>0)
        { StopCombat(); return; }
        int tier=Input.IsActionPressed(InputSetup.Grip)?2:Input.IsActionPressed(InputSetup.NormalAttack)?1:Input.IsActionPressed(InputSetup.Attack)?0:-1;
        if(Animal is Triceratops && tier==2 && _chargeRemaining<=0 && _chargeCooldown<=0)
        { _chargeRemaining=10; _chargeCooldown=12; _chargeDirection=Forward(Animal.Rotation.Y); }
        if(_chargeRemaining>0)
        {
            if(Input.IsActionJustPressed(InputSetup.Jump)) { _chargeRemaining=0; tier=-1; }
            else { tier=2; _chargeRemaining=Mathf.Max(0,_chargeRemaining-dt); }
        }
        else if(Animal is Triceratops && tier==2 && _chargeCooldown>0) tier=-1;
        if(tier<0) { StopCombat(); return; }
        if(tier!=2) ReleaseSpecial();
        Animal.AttackTier=tier;
        _attackSeconds=tier==_lastAttack?_attackSeconds+dt:0;
        if(Animal is Bird bird && (tier!=_lastAttack || _attackSeconds>Bird.SlashSeconds)) { bird.Slash(); _attackSeconds=0; }
        _lastAttack=tier;

        bool tail=(Animal is Ankylosaurus or Stegosaurus && tier<2)||(Animal is Triceratops or Pachycephalosaurus && tier==0);
        bool radial=Animal is Ankylosaurus && tier==2;
        var forward=Forward(Animal.Rotation.Y);
        float reach=Mathf.Max(1.2f,Stats.MouthDistance+Stats.BodyRadius);
        var origin=GlobalPosition;
        bool InArc(Vector3 p)
        {
            var delta=(p-origin) with { Y=0 };
            return radial||delta.LengthSquared()<0.04f||delta.Normalized().Dot(tail?-forward:forward)>0.05f;
        }
        float dps=tier==2?Stats.SpecialDps:tier==1?Stats.NormalDps:Stats.BasicDps;
        if(Wildlife is not null)
        {
            var targets=Wildlife.CombatTargets(origin,reach).Where(a=>InArc(a.GlobalPosition)).ToArray();
            bool grabs=tier==2&&Animal is Velociraptor or Dromaeosaurus or Anchiornis or Tyrannosaurus;
            if(_specialTarget is not null&&(!Wildlife.IsAlive(_specialTarget)||!grabs)) ReleaseSpecial();
            if(grabs&&_specialTarget is null&&targets.Length>0)
            {
                var target=targets[0];
                if(Animal is not Tyrannosaurus||target.Stats.BodyHeight*Wildlife.SizeOf(target)<Stats.BodyHeight*0.75f)
                {
                    _specialTarget=target; _targetLifted=Animal is Tyrannosaurus;
                    if(!_targetLifted) Wildlife.Grip(target,true,pins:true);
                }
            }
            if(_specialTarget is { } held)
            {
                if(_targetLifted) Wildlife.Lift(held,GlobalPosition+forward*Stats.MouthDistance+Vector3.Up*Stats.BodyHeight*0.65f,Animal.GlobalRotation.Y);
                else if(((held.GlobalPosition-GlobalPosition) with {Y=0}).Length()>reach*1.8f) ReleaseSpecial();
            }
            var victims=_specialTarget is { } grabbed?new[]{grabbed}:radial||tail||Animal is Stegosaurus?targets:targets.Take(1).ToArray();
            foreach(var prey in victims)
            {
                Wildlife.Bite(prey,dps*dt,GlobalPosition);
                if(_herds.TryGetValue(Animal,out var family)) family.Attack(prey);
                bool knock=tier==2&&Animal is Gallimimus or Pachycephalosaurus or Triceratops or Ankylosaurus;
                bool toss=tier==1&&Animal is Stegosaurus or Ankylosaurus;
                if(knock||toss) Wildlife.Knock(prey,forward*(knock?2f:0.5f)*dt,toss?3f:0,0.35f);
            }
        }
        if(Network is { IsOnline:true } network)
            foreach(var rival in network.Others.Where(r=>r.Animal is not null&&r.Health>0&&r.GlobalPosition.DistanceTo(origin)<reach+r.BodyRadius&&InArc(r.GlobalPosition)).Take(radial?16:1))
                network.Strike(rival,dps*dt,forward*(tier==2?1f:0),tier==2?0.2f:0);
    }

    public void ReceiveStrike(float damage,Vector3 push,float stun)
    {
        if(!float.IsFinite(damage)||damage<=0||IsDead) return;
        Bitten(Mathf.Min(damage,100f));
        _stunned=Mathf.Max(_stunned,Mathf.Clamp(stun,0,1));
        _knockback=push.LimitLength(5);
    }
    private Vector3 CombatMovement(Vector3 direction)
    {
        if(_stunned>0) return Vector3.Zero;
        if(_chargeRemaining>0) return _chargeDirection;
        if(Animal.AttackTier>=1&&Animal is Tyrannosaurus or Pachycephalosaurus) return Forward(Animal.Rotation.Y);
        return _specialTarget is not null?Vector3.Zero:direction;
    }
}
