using Vintagestory.API.Common.Entities;

namespace BlockyVehicleLib.Entities;

public class EntityControlledVehiclePhysics(Entity entity) : 
    PhysicsBehaviorBaseVehicle(entity),
    IPhysicsTickable,
    IRemotePhysics
{
    public override string PropertyName()
    {
        return "controlledvehiclephysics";
    }

    public virtual void OnPhysicsTick(float dt)
    {
        throw new System.NotImplementedException();
    }

    public virtual void AfterPhysicsTick(float dt)
    {
        throw new System.NotImplementedException();
    }

    public bool Ticking { get; set; }
    public Entity Entity { get; }
    public void HandleRemotePhysics(float dt, bool isTeleport)
    {
        throw new System.NotImplementedException();
    }

    public virtual void OnReceivedClientPos(int version)
    {
        throw new System.NotImplementedException();
    }
}