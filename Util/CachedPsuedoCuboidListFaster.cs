using System;
using System.Collections;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

#nullable disable
namespace VehicleAPI.Util;

/// <summary>
/// Just like CachedCuboidList except we use structs internally, for RAM access performance. We leave CachedCuboidList just as it is for mod backwards compatibility
/// </summary>
public class CachedPsuedoCuboidListFaster : IEnumerable<PsuedoCuboidd>, IEnumerable
{
  public PsuedoCuboidd[] cuboids = Array.Empty<PsuedoCuboidd>();
  public FastVec3i[] positions;
  public Block[] blocks;
  public int Count;
  private int populatedSize;

  public void Clear() => this.Count = 0;

  public void Add(PsuedoCuboidd[] cuboids, int x, int y, int z, Block block = null)
  {
    for (int index = 0; index < cuboids.Length; ++index)
      this.Add(cuboids[index], x, y, z, block);
  }
  
  public void Add(Cuboidf[] cuboids, int x, int y, int z, Block block = null)
  {
    for (int index = 0; index < cuboids.Length; ++index)
    {
      PsuedoCuboidd sudoCuboid;
      sudoCuboid = new PsuedoCuboidd();
      sudoCuboid.Set(cuboids[index].MinX, cuboids[index].MinY, cuboids[index].MinZ,
        cuboids[index].MaxX, cuboids[index].MaxY, cuboids[index].MaxZ);
      this.Add(sudoCuboid, x, y, z, block);
    }
  }
  
  public void Add(Cuboidf cuboid, int x, int y, int z, Block block = null)
  {
      PsuedoCuboidd sudoCuboid;
      sudoCuboid = new PsuedoCuboidd();
      sudoCuboid.Set(cuboid.MinX, cuboid.MinY, cuboid.MinZ,
        cuboid.MaxX, cuboid.MaxY, cuboid.MaxZ);
      this.Add(sudoCuboid, x, y, z, block);
  }

  public void Add(PsuedoCuboidd cuboid, int x, int y, int z, Block block = null)
  {
    if (cuboid == null)
      return;
    if (this.Count >= this.populatedSize)
    {
      if (this.Count >= this.cuboids.Length)
        this.ExpandArrays();
      this.cuboids[this.Count] = cuboid.OffsetCopyDouble((double) x, (double) (y % 32768 /*0x8000*/), (double) z);
      this.positions[this.Count] = new FastVec3i(x, y, z);
      this.blocks[this.Count] = block;
      ++this.populatedSize;
    }
    else
    {
      this.cuboids[this.Count].Set((double) cuboid.X1 + (double) x, (double) cuboid.Y1 + (double) (y % 32768 /*0x8000*/), (double) cuboid.Z1 + (double) z, (double) cuboid.X2 + (double) x, (double) cuboid.Y2 + (double) (y % 32768 /*0x8000*/), (double) cuboid.Z2 + (double) z);
      this.positions[this.Count].Set(x, y, z);
      this.blocks[this.Count] = block;
    }
    ++this.Count;
  }

  public IEnumerator<PsuedoCuboidd> GetEnumerator()
  {
    for (int i = 0; i < this.Count; ++i)
      yield return this.cuboids[i];
  }

  IEnumerator IEnumerable.GetEnumerator() => (IEnumerator) this.GetEnumerator();

  private void ExpandArrays()
  {
    int length = this.populatedSize == 0 ? 16 /*0x10*/ : this.populatedSize * 3 / 2;
    PsuedoCuboidd[] cuboiddArray = new PsuedoCuboidd[length];
    FastVec3i[] fastVec3iArray = new FastVec3i[length];
    Block[] blockArray = new Block[length];
    for (int index = 0; index < this.populatedSize; ++index)
    {
      cuboiddArray[index] = this.cuboids[index];
      fastVec3iArray[index] = this.positions[index];
      blockArray[index] = this.blocks[index];
    }
    this.cuboids = cuboiddArray;
    this.positions = fastVec3iArray;
    this.blocks = blockArray;
  }
}
