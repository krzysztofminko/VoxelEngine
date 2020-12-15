using UnityEngine;

namespace Voxels.Generators
{
	[CreateAssetMenu(menuName = "Voxel Generator/Random")]
	public class Random : Generator
	{
		public override byte Generate(Grid grid, float x, float y, float z)
		{
			return (byte)UnityEngine.Random.Range(0, 2);
		}
	}
}
