using System.Collections.Generic;
using System.Linq;
using Godot;

namespace JurassicWorld;

public partial class Wildlife
{
    public IEnumerable<Animal> CombatTargets(Vector3 origin,float reach) => _beasts.Values
        .Where(b=>!b.IsDead&&!b.Group.Beaten&&Mathf.Abs(b.Body.GlobalPosition.Y-origin.Y)<reach+b.Animal.Stats.BodyHeight*b.Size)
        .Where(b=>((b.Body.GlobalPosition-origin) with { Y=0 }).Length()<reach+b.Animal.Stats.BodyRadius*b.Size)
        .OrderBy(b=>b.Body.GlobalPosition.DistanceSquaredTo(origin)).Select(b=>b.Animal);

    public void Knock(Animal animal,Vector3 push,float lift,float seconds)
    {
        if(!_beasts.TryGetValue(animal,out var beast)||beast.IsDead) return;
        beast.Stunned=Mathf.Max(beast.Stunned,seconds);
        var at=beast.Body.GlobalPosition+push;
        float edge=_terrain.HalfSize-3;
        at.X=Mathf.Clamp(at.X,-edge,edge); at.Z=Mathf.Clamp(at.Z,-edge,edge);
        at.Y=Mathf.Max(at.Y,_terrain.GroundBelow(at));
        if(lift>0&&beast.Fall<=0) { at.Y+=0.35f; beast.Fall=lift; }
        beast.Body.GlobalPosition=at;
    }
}
