using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//TODO: Interface for external communication with Voxels system
namespace Voxels
{
	public class Editor : MonoBehaviour
	{
		[SerializeField]
		private bool debug;
		[SerializeField]
		private LayerMask chunkLayer;
		[SerializeField]
		private Grid grid;

		public VoxelType place;
		public VoxelType clear;

		private void Update()
		{
			if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
			{
				if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, chunkLayer))
				{
					Chunk chunk = hit.collider.GetComponent<Chunk>();
					if (chunk)
					{
						if (Input.GetMouseButtonDown(0) || (Input.GetMouseButton(0) && Input.GetButton("Continuous")))
						{
							//Place voxel
							Vector3Int voxelPositionInt = RayToVoxelPosition(new Ray(hit.point, hit.normal), true);
							grid.SetVoxel(voxelPositionInt.x, voxelPositionInt.y, voxelPositionInt.z, place);

							if (debug)
								Debug.DrawLine(voxelPositionInt, voxelPositionInt + hit.normal.normalized, Color.white, 2);
						}
						else if (Input.GetMouseButtonDown(1) || (Input.GetMouseButton(1) && Input.GetButton("Continuous")))
						{
							//Clear voxel
							Vector3Int voxelPositionInt = RayToVoxelPosition(new Ray(hit.point, hit.normal), false);
							grid.SetVoxel(voxelPositionInt.x, voxelPositionInt.y, voxelPositionInt.z, clear);

							if (debug)
								Debug.DrawLine(voxelPositionInt, voxelPositionInt + hit.normal.normalized, Color.gray, 2);
						}

						if (debug)
							Debug.DrawLine(hit.point, hit.point + hit.normal.normalized, Color.red, 2);
					}
				}
			}
		}

		private Vector3Int RayToVoxelPosition(Ray ray, bool above = false)
		{
			return new Vector3Int(
				WorldToVoxelCoordinate(ray.origin.x, ray.direction.normalized.x, above),
				WorldToVoxelCoordinate(ray.origin.y, ray.direction.normalized.y, above),
				WorldToVoxelCoordinate(ray.origin.z, ray.direction.normalized.z, above)
			);
		}

		private int WorldToVoxelCoordinate(float coordinate, float norm, bool above = false)
		{
			if (Mathf.Abs(coordinate - (int)coordinate) == 0.5f)    // if coordinate is on the x, y, or z face of voxel			
			{
				if (above)	
					coordinate += norm * 0.5f;  // move coordinate half of voxel size in forward direction from face
				else
					coordinate -= norm * 0.5f; // move coordinate half of voxel size in backward direction from face
			}

			//return rounded coordinate of voxel
			return Mathf.RoundToInt(coordinate);
		}
	}
}