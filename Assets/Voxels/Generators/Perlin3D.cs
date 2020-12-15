using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Voxels.Generators
{
	[CreateAssetMenu(menuName = "Voxel Generator/Perlin3D")]
	public class Perlin3D : Generator
	{
		public Vector3 scale;

		private byte id;
		private float perlinxy;
		private float perlinxz;
		private float perlinyz;
		private float density;

		public override byte Generate(Grid grid, float x, float y, float z)
		{
			id = 0;
			density = y / grid.SizeY * 0.75f + Mathf.PerlinNoise(grid.SizeX + x * scale.x, grid.SizeZ + z * scale.z) * 0.25f;

			perlinxz = Mathf.PerlinNoise(x * scale.x, z * scale.z);
			perlinxy = Mathf.PerlinNoise(x * scale.x, y * scale.y);
			perlinyz = Mathf.PerlinNoise(y * scale.y, z * scale.z);
			density = (density + (perlinxz + perlinxy + perlinyz) / 3) / 2;

			if (density <  0.5f)
				id = 1;
			return id;
		}
	}
}