using System;
using System.Collections.Generic;
using BlockGhost.Network;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace BlockGhost.BlockGhostRenderer;

public class BlockGhostRenderer: IRenderer, IDisposable
{
    private readonly Dictionary<long, BlockGhostDimPacket> gridChunks = new Dictionary<long, BlockGhostDimPacket>();

    private int playerChunkX;
    private int playerChunkZ;
    private int lastDirtyChunkX;
    private int lastDirtyChunkZ;
    
    public double RenderOrder => 0.37;
    public int RenderRange { get; }

    private ICoreClientAPI capi;
    
    public bool ShouldLoad(ICoreAPI api)
    {
        return api.Side == EnumAppSide.Client;
    }
    
    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
    {
        throw new NotImplementedException();
    }
}