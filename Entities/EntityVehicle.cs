using System.Runtime.CompilerServices;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.Server;
using static Vintagestory.API.Config.GlobalConstants;

namespace VehicleAPI.Entities;

internal class EntityVehicle : EntityChunky
{
    public EntityVehicle()
    {
        this.Stats = new EntityStats((Entity) this);
        this.WatchedAttributes.SetAttribute("dim", (IAttribute) new IntAttribute());
    }
    
    public virtual void OnEntitySpawn() => base.OnEntitySpawn();

    public static EntityChunky CreateVehicle(ICoreServerAPI sapi, IMiniDimension dim)
    {
        EntityChunky entity = (EntityChunky) sapi.World.ClassRegistry.CreateEntity(nameof (EntityChunky));
        ((RegistryObject) entity).Code = new AssetLocation("vehicleapi:vehicle");
        entity.AssociateWithDimension(dim);
        return entity;
    }
    
    public static EntityChunky CreateVehicle(ICoreClientAPI capi, IMiniDimension dim)
    {
        EntityChunky entity = (EntityChunky) capi.World.ClassRegistry.CreateEntity(nameof (EntityChunky));
        ((RegistryObject) entity).Code = new AssetLocation("vehicleapi:vehicle");
        entity.AssociateWithDimension(dim);
        return entity;
    }
}