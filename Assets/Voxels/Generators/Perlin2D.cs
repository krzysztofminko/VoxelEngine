using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Voxels.Generators
{
	[CreateAssetMenu(menuName = "Voxel Generator/Perlin2D")]
	public class Perlin2D : Generator
	{
		public Vector3 scale1;
		public Vector3 scale2;
		public Vector3 scale3;

		private float perlin;

		public override byte Generate(Grid grid, float x, float y, float z)
		{
			byte id = 0;
			perlin = Mathf.PerlinNoise(x * scale1.x, z * scale1.z) * 0.57f;
			perlin += Mathf.PerlinNoise(x * scale2.x, z * scale2.z) * 0.258f;
			perlin += Mathf.PerlinNoise(x * scale3.x, z * scale3.z) * 0.1425f;
			if (y / grid.SizeY / scale1.y * 2f < perlin)
				id = 4;
			else if (y / grid.SizeY / scale1.y < perlin)
				if (y + 1 < grid.SizeY && (y + 1) / grid.SizeY / scale1.y > perlin)
					id = 2;
				else
					id = 1;
			return id;
		}
	}
}