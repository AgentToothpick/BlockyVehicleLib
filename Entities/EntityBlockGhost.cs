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

namespace BlockGhost.Entities;

internal class EntityBlockGhost : EntityChunky
{
    public EntityBlockGhost()
    {
        this.Stats = new EntityStats((Vintagestory.API.Common.Entities.Entity) this);
        this.WatchedAttributes.SetAttribute("dim", (IAttribute) new IntAttribute());
    }
    
    public virtual void OnEntitySpawn() => base.OnEntitySpawn();

    public static EntityChunky CreateBlockGhost(ICoreServerAPI sapi, IMiniDimension dim)
    {
        EntityChunky entity = (EntityChunky) sapi.World.ClassRegistry.CreateEntity(nameof (EntityChunky));
        ((RegistryObject) entity).Code = new AssetLocation("blockghost:blockghost");
        entity.AssociateWithDimension(dim);
        return entity;
    }
    
    public static EntityChunky CreateBlockGhost(ICoreClientAPI capi, IMiniDimension dim)
    {
        EntityChunky entity = (EntityChunky) capi.World.ClassRegistry.CreateEntity(nameof (EntityChunky));
        ((RegistryObject) entity).Code = new AssetLocation("blockghost:blockghost");
        entity.AssociateWithDimension(dim);
        return entity;
    }
}