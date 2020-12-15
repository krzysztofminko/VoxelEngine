using UnityEngine;

namespace Voxels
{
	public abstract class Generator : ScriptableObject
	{
		public abstract byte Generate(Grid grid, float x, float y, float z);
	}
}