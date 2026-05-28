using System;
using System.Collections.Generic;
using System.Linq;
using BlockyVehicleLib.Util;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Datastructures;

namespace BlockyVehicleLib.Entities;

[DocumentAsJson]
[AddDocumentationProperty("waterDragFactor", "Gravity drag factor when in water", "System.Double", "Optional", "1", false)]
[AddDocumentationProperty("airDragFactor", "Gravity drag factor when falling. Overrides airDragFallingFactor when present", "System.Double", "Optional", "1", false)]
[AddDocumentationProperty("airDragFallingFactor", "Gravity drag factor when falling", "System.Double", "Optional", "1", false)]
[AddDocumentationProperty("groundDragFactor", "Horizontal drag factor when on the ground", "System.Double", "Optional", "1", false)]
[AddDocumentationProperty("gravityFactor", "Multiplier for gravity strength", "System.Double", "Optional", "1", false)]
public class EntityBehaviorVehiclePhysics(Entity entity) : 
    EntityControlledVehiclePhysics(entity),
    IPhysicsTickable,
    IRemotePhysics,
    IRenderer,
    IDisposable
{
  protected readonly Vec3d prevPos = new Vec3d();
  protected double motionBeforeY;
  protected bool feetInLiquidBefore;
  protected bool onGroundBefore;
  protected bool swimmingBefore;
  protected bool collidedBefore;
  protected Vec3d newPos = new Vec3d();
  protected bool vehiclesNearby = false;
  protected int tickCounter = 0;
  /// <summary>The amount of drag while travelling through water.</summary>
  //public double WaterDragValue = (double) GlobalConstants.WaterDrag;
  /// <summary>The amount of drag while travelling through the air.</summary>
  //public double AirDragValue = (double) GlobalConstants.AirDragAlways;
  /// <summary>The amount of drag while travelling on the ground.</summary>
  //public double GroundDragValue = 0.699999988079071;
  /// <summary>The amount of drag while travelling on the ground.</summary>
  //public double BoyancyMul = 1.0;
  /// <summary>
  /// The amount of gravity applied per tick to this entity.
  /// </summary>
  //public double GravityPerSecond = (double) GlobalConstants.GravityPerSecond;
  /// <summary>
  /// If set, will test for entity collision every tick (expensive)
  /// </summary>
  public Action<float> OnPhysicsTickCallback;
  [ThreadStatic] private static BlockPos tmpPos;

  public Entity Entity => this.entity;

  public bool Ticking { get; set; } = true;

  public override string PropertyName() => "entityvehiclephysics";
  
  //private EntityChunky[] nearbyVehicles = new EntityChunky[10];//Need to find a resource efficient way to keep this up to date, limit of 10 for now
  private Dictionary<int, EntityChunky> nearbyVehiclesList = new Dictionary<int, EntityChunky>();
  
  public void SetState(EntityPos pos)
  {
    this.prevPos.Set(pos);
    this.motionBeforeY = pos.Motion.Y;
    Entity entity = this.entity;
    this.onGroundBefore = entity.OnGround;
    this.feetInLiquidBefore = entity.FeetInLiquid;
    this.swimmingBefore = entity.Swimming;
    this.collidedBefore = entity.Collided;
  }

  public virtual void SetProperties(JsonObject attributes)
  {
    //this.WaterDragValue = 1.0 - (1.0 - this.WaterDragValue) * attributes["waterDragFactor"].AsDouble(1.0);
    //JsonObject attribute = (JsonObject)attributes["airDragFactor"];
    //this.AirDragValue = 1.0 - (1.0 - this.AirDragValue) * (attribute.Exists ? attribute.AsDouble(1.0) : attributes["airDragFallingFactor"].AsDouble(1.0));
    //if (this.entity.WatchedAttributes.HasAttribute("airDragFactor"))
    //  this.AirDragValue = 1.0 - (1.0 - (double) GlobalConstants.AirDragAlways) * this.entity.WatchedAttributes.GetDouble("airDragFactor", 0.0);
    //this.GroundDragValue = 0.3 * attributes["groundDragFactor"].AsDouble(1.0);
    //this.GravityPerSecond *= attributes["gravityFactor"].AsDouble(1.0);
    //this.BoyancyMul = attributes["boyancyMul"].AsDouble(1.0);
    //if (!this.entity.WatchedAttributes.HasAttribute("gravityFactor"))
    //  return;
    //this.GravityPerSecond = (double) GlobalConstants.GravityPerSecond * this.entity.WatchedAttributes.GetDouble("gravityFactor", 0.0);
  }

  public override void Initialize(EntityProperties properties, JsonObject attributes)
  {
    base.Initialize();
    this.SetProperties(attributes);
    
    if (this.entity.Api is ICoreServerAPI api)
    {
      sapi.Logger.Event("EntityBehaviorVehiclePhysics Initializing");
      api.Server.AddPhysicsTickable((IPhysicsTickable) this);
    }
    else
    {
      EnumHandling handled = EnumHandling.Handled;
      this.OnReceivedServerPos(true, ref handled);
    }
  }

  public override void OnReceivedClientPos(int version)
  {
    if (version > this.previousVersion)
    {
      this.previousVersion = version;
      this.HandleRemotePhysics(0.06666667f, true);
    }
    else
      this.HandleRemotePhysics(0.06666667f, false);
  }

  
  //This probably needs to be updated to apply tests for each relativePos for each (nearby) vehicle
  public override void HandleRemotePhysics(float dt, bool isTeleport)
  {
    if (this.nPos == (Vec3d) null)
    {
      this.nPos = new Vec3d();
      this.nPos.Set(this.entity.Pos);
    }
    
    float dtFactor = dt * 60f;
    EntityPos lPos = this.lPos;
    lPos.SetFrom(this.nPos);
    this.nPos.Set(this.entity.Pos);
    Vec3d motion = lPos.Motion;
    if (isTeleport)
      lPos.SetFrom(this.nPos);
    //motion.X = (this.nPos.X - lPos.X) / (double) dtFactor;
    //motion.Y = (this.nPos.Y - lPos.Y) / (double) dtFactor;
    //motion.Z = (this.nPos.Z - lPos.Z) / (double) dtFactor;
    if (motion.Length() > 20.0)
      motion.Set(0.0, 0.0, 0.0);
    //this.entity.Pos.Motion.Set(motion);
    PhysicsBehaviorBaseVehicle.collisionTester.NewTick(lPos);
    this.SetState(lPos);
    this.RemoteMotionAndCollision(lPos, dtFactor);
    this.ApplyTests(lPos);
  }

  public void RemoteMotionAndCollision(EntityPos pos, float dtFactor)
  {
    //removed the gravity from this
    if (vehiclesNearby) PhysicsBehaviorBaseVehicle.collisionTester.ApplyTerrainCollision(this.entity, pos, this.vehiclePosList/*entityChunkyPosList*/, dtFactor, ref this.newPos, this.subDimensionIdList/*subDimensionId*/, 0.0f, this.CollisionYExtra);
    else PhysicsBehaviorBaseVehicle.collisionTester.ApplyTerrainCollision(this.entity, pos, dtFactor, ref this.newPos, 0.0f, this.CollisionYExtra);
    pos.SetPos(this.nPos);
  }

  public void MotionAndCollision(EntityPos pos, float dt)
  {
    float dtFactor = 60f * dt;
    Entity entity = this.entity;
    Vec3d motion = pos.Motion;
    IBlockAccessor blockAccessor = entity.World.BlockAccessor;
    int dimension = pos.Dimension;
    //Removed drag
    Block block = (Block) null;
    //removed redundant physics
    /*
    double x = motion.X * (double) dtFactor + pos.X;
    double y = motion.Y * (double) dtFactor + pos.Y;
    double z = motion.Z * (double) dtFactor + pos.Z;
    this.applyCollision(pos, dtFactor);
    Vec3d newPos = this.newPos;
    if (blockAccessor.IsNotTraversable((double) (int) x, (double) (int) pos.Y, (double) (int) pos.Z, dimension))
      newPos.X = pos.X;
    if (blockAccessor.IsNotTraversable((double) (int) pos.X, (double) (int) y, (double) (int) pos.Z, dimension))
      newPos.Y = pos.Y;
    if (blockAccessor.IsNotTraversable((double) (int) pos.X, (double) (int) pos.Y, (double) (int) z, dimension))
      newPos.Z = pos.Z;
    pos.SetPos(newPos);
    if (x < newPos.X && motion.X < 0.0 || x > newPos.X && motion.X > 0.0)
      motion.X = 0.0;
    if (y < newPos.Y && motion.Y < 0.0 || y > newPos.Y && motion.Y > 0.0)
      motion.Y = 0.0;
    if ((z >= newPos.Z || motion.Z >= 0.0) && (z <= newPos.Z || motion.Z <= 0.0))
      return;
    motion.Z = 0.0;
    */
  }

  protected virtual void applyCollision(EntityPos pos, float dtFactor)
  {
    if (vehiclesNearby) PhysicsBehaviorBaseVehicle.collisionTester.ApplyTerrainCollision(this.entity, pos, this.vehiclePosList/*entityChunkyPosList*/, dtFactor, ref this.newPos, nearbyVehiclesList.Keys.ToArray()/*subDimensionId*/, 0.0f, this.CollisionYExtra);
    //else PhysicsBehaviorBaseVehicle.collisionTester.ApplyTerrainCollision(this.entity, pos, dtFactor, ref this.newPos, 0.0f, this.CollisionYExtra);
  }

  //this needs to be updated to apply tests for each relativePos for each (nearby) vehicle
  public void ApplyTests(EntityPos pos)
  {
    Entity entity = this.entity;
    GetNearbyVehicles(entity);
    IBlockAccessor blockAccessor = entity.World.BlockAccessor;
    //Removed redundant physics
    {
      PsuedoCuboidd entityBox = new PsuedoCuboidd();
      
      foreach (int key in nearbyVehiclesList.Keys)
      {
        int dim = key;
        entityBox.SetFromCuboidf(entity.SelectionBox, GetConvertedPos(nearbyVehiclesList.TryGetValue(key).Pos, entity.Pos, dim));
        int x2 = (int) entityBox.X2;
        int y2 = (int) entityBox.Y2;
        int z2 = (int) entityBox.Z2;
        int z1 = (int) entityBox.Z1;
        BlockPos tmpPos = PhysicsBehaviorBaseVehicle.collisionTester.tmpPos;
        tmpPos.SetDimension(entity.Pos.Dimension);
        for (int y1 = (int) entityBox.Y1; y1 <= y2; ++y1)
        {
          for (int x1 = (int) entityBox.X1; x1 <= x2; ++x1)
          {
            for (int z = z1; z <= z2; ++z)
            {
              tmpPos.Set(x1, y1, z);
              blockAccessor.GetBlock(tmpPos).OnEntityInside(entity.World, entity, tmpPos);
            }
          }
        }
      }
    }
  }

  public override void OnPhysicsTick(float dt)
  {
    Entity entity = this.entity;
    if (entity.State != EnumEntityState.Active || !this.Ticking)
    {
      return;
    }
    if (entity.Api.Side == EnumAppSide.Server)
    {
      if (tickCounter++ > 20)
      {
        this.nearbyVehiclesList = GetNearbyVehicles(entity);
        if (nearbyVehiclesList.Count > 0) vehiclesNearby = true;
        else vehiclesNearby = false;
        tickCounter = 0;
      }
    }
    IMountable mountableSupplier = this.mountableSupplier;
    if ((mountableSupplier != null ? (mountableSupplier.IsBeingControlled() ? 1 : 0) : 0) != 0 && entity.World.Side == EnumAppSide.Server)
      return;
    EntityPos pos = entity.Pos;
    //PhysicsBehaviorBaseVehicle.collisionTester.AssignToEntity((PhysicsBehaviorBaseVehicle) this, pos.Dimension);
    int num = pos.Motion.Length() > 0.1 ? 10 : 1;
    float dt1 = dt / (float) num;
    for (int index = 0; index < num; ++index)
    {
      this.SetState(pos);
      this.MotionAndCollision(pos, dt1);
      this.ApplyTests(pos);
    }
    entity.Pos.SetFrom(pos);
  }
  public Action<float> OnPhysicsTickCallback2;
  
  private bool Matches(Entity t1)
  {
    return ((t1.WatchedAttributes.GetAttribute("dim") as IntAttribute)?.value != null);
  }

  public override void AfterPhysicsTick(float dt)
  {
    Action afterPhysicsTick = this.entity.AfterPhysicsTick;
    if (afterPhysicsTick == null)
      return;
    afterPhysicsTick();
  }

  protected virtual bool IsFirstTick(Entity entity)
  {
    EntityPos previousServerPos = entity.PreviousServerPos;
    return previousServerPos.X == 0.0 && previousServerPos.Y == 0.0 && previousServerPos.Z == 0.0 && this.prevPos.X == 0.0 && this.prevPos.Y == 0.0 && this.prevPos.Z == 0.0;
  }

  public override void OnEntityDespawn(EntityDespawnData despawn)
  {
    if (this.sapi == null)
      return;
    this.sapi.Server.RemovePhysicsTickable((IPhysicsTickable) this);
  }

  public EntityPos GetConvertedPos(EntityPos vehiclePos, EntityPos entityPos, int subDimensionId)
  {
    EntityPos output = new EntityPos();
    output.SetPos(VehicleCollisionTester.FindRelativePosition(vehiclePos, entityPos));
    output.X +=
      (int)(subDimensionId % 4096 /*0x1000*/ * 16384 /*0x4000*/ + 8192 /*0x2000*/);
    
    output.Y += 8192 /*0x2000*/;
    
    output.Z +=
      (int)(subDimensionId / 4096 /*0x1000*/ * 16384 /*0x4000*/ + 8192 /*0x2000*/);
    double[] vehicleRotation = PsuedoCuboidd.ConvertEulerAngles(vehiclePos.Pitch, vehiclePos.Yaw, vehiclePos.Roll);
    float[] eulerAngles = Quaterniond.ToEulerAngles([-vehicleRotation[0], -vehicleRotation[1] , -vehicleRotation[2] , vehicleRotation[3]]);
    output.Pitch = eulerAngles[0];
    output.Yaw = eulerAngles[1];
    output.Roll = eulerAngles[2];
    return output;
  }

  public Dictionary<int, EntityChunky> GetNearbyVehicles(Entity entity)
  {
    //EntityChunky[] entityChunkyList = (EntityChunky[])entity.Api.World.GetEntitiesAround(
    //  entity.Pos.XYZ,
    //  (float)entity.Api.World.DefaultEntityTrackingRange, (float)entity.Api.World.DefaultEntityTrackingRange,
    //  Matches);
    //return entityChunkyList;
    return GetNearbyVehicles(entity.Pos, entity.Api.World);
  }
  
  public Dictionary<int, EntityChunky> GetNearbyVehicles(EntityPos entityPos, IWorldAccessor world)
  {
    Dictionary<int, EntityChunky> copy = nearbyVehiclesList;
    
    Entity[] entityList = world.GetEntitiesAround(
      entityPos.XYZ,
      (float)world.DefaultEntityTrackingRange, (float)world.DefaultEntityTrackingRange,
      Matches);
    for (int i = 0; i < entityList.Length; i++)
    {
      int? tmpDim = ((entityList[i].WatchedAttributes.GetAttribute("dim") as IntAttribute).value);
      if (tmpDim == null) continue;
      if (nearbyVehiclesList.ContainsKey(tmpDim.Value)) continue;
      
      EntityChunky tmpChunky = new EntityChunky();
      tmpChunky.Stats = entityList[i].Stats;
      tmpChunky.WatchedAttributes.SetInt("dim", tmpDim.Value);
      this.nearbyVehiclesList.Add((tmpChunky.WatchedAttributes.GetAttribute("dim") as IntAttribute).value, tmpChunky);
    }

    foreach (int key in nearbyVehiclesList.Keys)
    {
      //remove the vehicles that are still nearby
      copy.Remove(key);
    }
    foreach (int key in copy.Keys)
    {
      //remove the vehicles that are no longer nearby
      nearbyVehiclesList.Remove(key);
    }
    for (int i = 0; i < nearbyVehiclesList.Count; i++)
    {
      vehiclePosList[i] = nearbyVehiclesList.Values.ElementAt(i).Pos;
    }
    return nearbyVehiclesList;
  }
  

  public void Dispose()
  {
    throw new NotImplementedException();
  }

  public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
  {
    throw new NotImplementedException();
  }

  public double RenderOrder { get; }
  public int RenderRange { get; }
}