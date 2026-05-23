using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BlockyVehicleLib.Items;
using BlockyVehicleLib.Entities;
using BlockyVehicleLib.Network;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.GameContent;
using Vintagestory.Server;

namespace BlockyVehicleLib;

public class BlockyVehicleLibModSystem : ModSystem
{
    private ICoreAPI api;
    public ICoreServerAPI sapi;
    public ICoreClientAPI capi;
    private Dictionary<string, int> _dimensionRegistry = null!;
    private int _dimensionIndex = -1;
    private bool _spawnSuccess = false;
    

    //Each player only gets one minidimension (for now)
    //Will change this later, mostly to support singleplayer

    public override void StartPre(ICoreAPI coreApi)
    {
        api = coreApi;
    }
    
    // Called on server and client
    // Useful for registering block/entity classes on both sides
    public override void Start(ICoreAPI coreApi)
    {
        api.RegisterEntity(Mod.Info.ModID + ".vehicle", typeof(EntityChunky));
        api.RegisterItemClass(Mod.Info.ModID + ".vehiclewand", typeof(ItemVehicleWand));
        api.RegisterEntityBehaviorClass(Mod.Info.ModID + ".entityphysicsbehavior", typeof(EntityBehaviorVehiclePhysics));
        //api.RegisterEntityBehaviorClass(Mod.Info.ModID + ".vehiclephysicsbehavior", typeof(BVLBehaviorVehiclePhysics));
        api.Network
            .RegisterChannel("VehicleNetworkApi")
            .RegisterMessageType<DimensionIndexRequest>()
            .RegisterMessageType<DimensionIndexResponse>()
            .RegisterMessageType<DimensionSpawnRequest>()
            .RegisterMessageType<DimensionSpawnClientResponse>()
            .RegisterMessageType<DimensionSpawnClientComplete>();
    }

    IServerNetworkChannel serverChannel;
    
    public override void StartServerSide(ICoreServerAPI serverApi)
    {
        sapi = serverApi;
        //_dimensionRegistry = new Dictionary<string, int>();
        //IMiniDimension dim = serverApi.World.BlockAccessor.CreateMiniDimension(new Vec3d(0, 0, 0));
        //int index = serverApi.Server.LoadMiniDimension(dim);
        //EntityChunky entity = EntityVehicle.CreateVehicle(serverApi, dim);
        
        sapi.Event.SaveGameLoaded += OnSaveGameLoaded;
        sapi.Event.GameWorldSave += OnGameWorldSave;
        sapi.Event.PlayerJoin += OnPlayerJoin;
        //sapi.Event.PlayerCreate += OnPlayerCreate;
        
        serverChannel = sapi.Network
            .GetChannel("VehicleNetworkApi")
            .SetMessageHandler<DimensionIndexRequest>(OnDimensionIndexRequest)
            .SetMessageHandler<DimensionSpawnRequest>(OnDimensionSpawnRequest)
            .SetMessageHandler<DimensionSpawnClientComplete>(OnDimensionSpawnClientComplete);
        
        //Mod.Logger.Notification("Mini dimension loaded, index: " + index);
    }
    
    public void OnDimensionIndexRequest(IServerPlayer player, DimensionIndexRequest message)
    {
        api.Logger.Event("BlockyVehicleLibModSystem.OnDimensionIndexResponse (server side): " + message.playerName);
        int index = GetMiniDimensionPlayerIndex(player);
        ((ItemVehicleWand)api.World.Items[message.vehicleWandID]).DimensionIndex = index;
        serverChannel.SendPacket(new DimensionIndexResponse() { index = index, vehicleWandID = message.vehicleWandID}, player);
    }
    
    public int GetMiniDimensionPlayerIndex(IPlayer player)
    {
        if (api.Side == EnumAppSide.Server)
        {
            _dimensionIndex = -1;
            foreach (var ele in _dimensionRegistry)
            {
                if (ele.Key == player.PlayerUID)
                {
                    _dimensionIndex = ele.Value;
                }
            }
            int output = _dimensionIndex;
            _dimensionIndex = -1;
            return output;
        }
        return -1;
    }

    IClientNetworkChannel clientChannel;
    
    public override void StartClientSide(ICoreClientAPI clientApi)
    {
        capi = clientApi;
        
        clientChannel = capi.Network.GetChannel("VehicleNetworkApi")
            .SetMessageHandler<DimensionIndexResponse>(OnDimensionIndexResponse)
            .SetMessageHandler<DimensionSpawnClientResponse>(OnDimensionSpawnClientResponse);
        //Mod.Logger.Notification("Hello from template mod client side: " + Lang.Get("Vehicle:hello"));
    }

    
    private void OnDimensionIndexResponse(DimensionIndexResponse message)
    {
        api.Logger.Event("BlockyVehicleLibModSystem.OnDimensionIndexResponse (client side): " + message.index);
        ((ItemVehicleWand)api.World.Items[message.vehicleWandID]).DimensionIndex = message.index;
    }

    public async void OnDimensionSpawnRequest(IPlayer player, DimensionSpawnRequest message)
    {
        if (api.Side == EnumAppSide.Client) return;
        
        //What should happen here?
        //Find the minidimension associated with the given index
        //Clear the minidimension (A warning should be given to the player first)
        //unload the unused server chunks
        //spawn the entity
        //associate the minidimension with the entity
        //Send a packet to all nearby players to get them to do the clientside of this
        //Place the blocks into the minidimension
        //Will need a more rigourous way to place blocks in the minidimension once more than one block are involved
        
        IMiniDimension? messageDim = sapi.Server.GetMiniDimension(message.dimensionIndex);
        IMiniDimension dim;
        
        //set the loaded minidimension to the correct index (should be unnecessary in current state, but will keep for future proofing)
        BlockPos pos = message.blockSel.Position.Copy();
        
        if (messageDim == null)
        {
            dim = sapi.World.BlockAccessor.CreateMiniDimension(new Vec3d((double) pos.X, (double) pos.Y + 1, (double) pos.Z));
            sapi.Server.SetMiniDimension(dim, message.dimensionIndex);
            sapi.Logger.Error("Mini dimension not found, new dimension created");
        }
        else
        {
            dim = messageDim;
            sapi.Server.SetMiniDimension(dim, message.dimensionIndex);
            dim.CurrentPos.SetPos(pos); //repeat this on client side
        }
        
        dim.SetSubDimensionId(message.dimensionIndex);
        BlockPos pos2 = new BlockPos(new Vec3i(0, 0, 0), 1);
        //pos.Sub(message.blockSel.Position);
        //pos.SetDimension(1);
        pos2.X +=
            (int)(message.dimensionIndex % 4096 /*0x1000*/ * 16384 /*0x4000*/ + 8192 /*0x2000*/);
        
        pos2.Y += 8192 /*0x2000*/;
        
        pos2.Z +=
            (int)(message.dimensionIndex / 4096 /*0x1000*/ * 16384 /*0x4000*/ + 8192 /*0x2000*/);
        
        dim.ClearChunks();
        //create the entity and associate it with the minidimension
        //Note: need to find preexisting entities and either recycle them or remove them
        EntityChunky entity = EntityVehicle.CreateVehicle(sapi, dim);
        sapi.World.SpawnEntity(entity);
        //dim.CurrentPos.SetPos(entity.Pos);
        serverChannel.SendPacket(new DimensionSpawnClientResponse() {dimId = dim.subDimensionId, blockPos = pos, vecPos = message.pos, blockId = message.blockId}, (IServerPlayer) player);
        await WaitingOnClient();
        
        //Do these after client side
        int blockId = message.blockId;
        dim.SetBlock(blockId, pos2);
        sapi.World.BlockAccessor.SetBlock(blockId, pos2);
        //dim.UnloadUnusedServerChunks();
        IPlayer[] players = sapi.Server.Players;
        dim.CollectChunksForSending(players);
        api.Logger.Event("Vehicle Spawned Successfully");
    }

    private void OnDimensionSpawnClientResponse(DimensionSpawnClientResponse message)
    {
        if (api.Side == EnumAppSide.Client)
        {
            IMiniDimension dim = capi.World.GetOrCreateDimension(message.dimId, message.vecPos);
            dim.CurrentPos.SetPos(message.blockPos);
            capi.World.SpawnEntity(EntityVehicle.CreateVehicle(capi, dim));
            clientChannel.SendPacket(new DimensionSpawnClientComplete() {success = true});
        }
    }
    
    private void OnDimensionSpawnClientComplete(IPlayer player, DimensionSpawnClientComplete message)
    {
        _spawnSuccess = message.success;
    }

    private async Task WaitingOnClient()
    {
        int i = 0;
        while (!_spawnSuccess && i < 1000)
        {
            i++;
            await Task.Delay(1);
        }
        _spawnSuccess = false;
    }
    
    private void OnPlayerJoin(IPlayer player)
    {
        if (api.Side == EnumAppSide.Server)
        {
            if (GetMiniDimensionPlayerIndex(player) == -1)
            {
                IMiniDimension dim = sapi.World.BlockAccessor.CreateMiniDimension(new Vec3d(0, 0, 0));
                int index = sapi.Server.LoadMiniDimension(dim);
                _dimensionRegistry.Add(player.PlayerUID, index);
                /*
                //need to come back to this
                //serverChannel.SendPacket(new DimensionIndexResponse { index = index }, ((IServerPlayer)player));
                sapi.World.SpawnEntity(EntityVehicle.CreateVehicle(sapi, dim));
                */
            }
        }
        //Check if the player is in the minidimension registry
        //if not, create and load a minidimension, then add them to the registry
        
        //Mod.Logger.Notification("Player joined: " + player.PlayerUID);
    }

    private void OnSaveGameLoaded()
    {
        byte[] data = sapi.WorldManager.SaveGame.GetData("Vehicle.DimensionRegistry");
        _dimensionRegistry = data == null ? new Dictionary<string, int>() : SerializerUtil.Deserialize<Dictionary<string, int>>(data);
        //Load all minidimensions from the registry
        //Load all blocks from each minidimension into the world, to be added later
    }

    private void OnGameWorldSave()
    {
        
        //May need to define how _dimensionRegistry is serialized
        //check how schematics are saved and copy that? Could get big and messy
        //empty dimensions should get skipped, will add later
        sapi.WorldManager.SaveGame.StoreData("Vehicle.DimensionRegistry", SerializerUtil.Serialize(_dimensionRegistry));
    }
}