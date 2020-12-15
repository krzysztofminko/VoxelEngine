using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Voxels
{
	public struct VoxelInfo
	{
		public byte id;
		public Faces faces;
	}

	[Flags]
	public enum Faces : byte
	{
		None = 0,
		Backward = 1,
		Forward = 2,
		Up = 4,
		Down = 8,
		Left = 16,
		Right = 32
	}

	public class Chunk : MonoBehaviour
	{
		public Grid grid;
		public int size;
		public Vector3Int localPosition;		
		
		private VoxelInfo[] voxels;
		private List<Vector3> vertices;
		private List<int> triangles;
		private List<Vector2> uvs;
		private List<Vector3> colliderVertices;
		private List<int> colliderTriangles;

		[SerializeField][Required]
		private MeshFilter meshFilter;
		[SerializeField][Required]
		private MeshCollider meshCollider;

		[ShowInInspector][HideInEditorMode][ReadOnly][NonSerialized]
		public bool waitingForUpdate;
		[ShowInInspector][HideInEditorMode][ReadOnly]
		public int VisibleVoxels { get; private set; }


		public void InitializeData()
		{
			voxels = new VoxelInfo[size * size * size];
			vertices = new List<Vector3>(24 * size * size * size / 2);
			triangles = new List<int>(12 * size * size * size / 2);
			uvs = new List<Vector2>(24 * size * size * size / 2);
			colliderVertices = new List<Vector3>(24 * size * size * size / 2);
			colliderTriangles = new List<int>(12 * size * size * size / 2);
		}

		public VoxelInfo GetVoxel(int localX, int localY, int localZ)
		{
			return voxels[localX * size * size + localZ * size + localY];
		}

		public void SetVoxel(int localX, int localY, int localZ, byte id)
		{
			voxels[localX * size * size + localZ * size + localY].id = id;
		}

		public void UpdateMesh()
		{
			waitingForUpdate = false;
			vertices.Clear();
			triangles.Clear();
			uvs.Clear();
			colliderVertices.Clear();
			colliderTriangles.Clear();

			VoxelInfo voxel;
			VoxelType type;
			int x, y, z;
			for (x = 0; x < size; x++)
				for (z = 0; z < size; z++)
					for (y = 0; y < size; y++)
					{
						voxel = GetVoxel(x, y, z);
						type = grid.GetVoxelType(voxel.id);
						if(voxel.id > 0 && type != null)
						{
							if (voxel.faces.HasFlag(Faces.Backward))
								AddFaceBackward(type, x, y, z);
							if (voxel.faces.HasFlag(Faces.Forward))
								AddFaceForward(type, x, y, z);
							if (voxel.faces.HasFlag(Faces.Up))
								AddFaceUp(type, x, y, z);
							if (voxel.faces.HasFlag(Faces.Down))
								AddFaceDown(type, x, y, z);
							if (voxel.faces.HasFlag(Faces.Left))
								AddFaceLeft(type, x, y, z);
							if (voxel.faces.HasFlag(Faces.Right))
								AddFaceRight(type, x, y, z);
						}
					}

			meshFilter.mesh.Clear();
			meshFilter.mesh.vertices = vertices.ToArray();
			meshFilter.mesh.triangles = triangles.ToArray();

			meshFilter.mesh.uv = uvs.ToArray();
			meshFilter.mesh.RecalculateNormals();

			meshCollider.sharedMesh = meshFilter.mesh;
		}
		
		public void UpdateAllFaces()
		{
			int oldVisibleVoxels = VisibleVoxels;
			VisibleVoxels = 0;
			Faces faces;
			int wx = localPosition.x * size;
			int wy = localPosition.y * size;
			int wz = localPosition.z * size;
			for (int x = 0; x < size; x++)
				for (int z = 0; z < size; z++)
					for (int y = 0; y < size; y++)
						if (grid.GetVoxelType(GetVoxel(x, y, z).id).solid)
						{
							faces = Faces.None;
							if (!grid.GetVoxelType(wx + x, wy + y, wz + z - 1).solid)
								faces |= Faces.Backward;
							if (!grid.GetVoxelType(wx + x, wy + y, wz + z + 1).solid)
								faces |= Faces.Forward;
							if (!grid.GetVoxelType(wx + x, wy + y - 1, wz + z).solid)
								faces |= Faces.Down;
							if (!grid.GetVoxelType(wx + x, wy + y + 1, wz + z).solid)
								faces |= Faces.Up;
							if (!grid.GetVoxelType(wx + x - 1, wy + y, wz + z).solid)
								faces |= Faces.Left;
							if (!grid.GetVoxelType(wx + x + 1, wy + y, wz + z).solid)
								faces |= Faces.Right;
							SetVoxelFaces(x, y, z, faces);
							if (faces > 0)
								VisibleVoxels++;
						}
			if (VisibleVoxels > 0 || oldVisibleVoxels > 0)
				grid.AddChunkToUpdateList(this);
		}


		private void SetVoxelFaces(int localX, int localY, int localZ, Faces faces)
		{
			voxels[localX * size * size + localZ * size + localY].faces = faces;
		}

		private void AddFaceBackward(VoxelType type, int x, int y, int z)
		{
			AddVertex(new Vector3(x - 0.5f, y - 0.5f, z - 0.5f));
			AddVertex(new Vector3(x - 0.5f, y + 0.5f, z - 0.5f));
			AddVertex(new Vector3(x + 0.5f, y + 0.5f, z - 0.5f));
			AddVertex(new Vector3(x + 0.5f, y - 0.5f, z - 0.5f));
			AddTriangles();
			AddUVs(type, type.uvBackward);
		}

		private void AddFaceForward(VoxelType type, int x, int y, int z)
		{
			AddVertex(new Vector3(x + 0.5f, y - 0.5f, z + 0.5f));
			AddVertex(new Vector3(x + 0.5f, y + 0.5f, z + 0.5f));
			AddVertex(new Vector3(x - 0.5f, y + 0.5f, z + 0.5f));
			AddVertex(new Vector3(x - 0.5f, y - 0.5f, z + 0.5f));
			AddTriangles();
			AddUVs(type, type.uvForward);
		}

		private void AddFaceDown(VoxelType type, int x, int y, int z)
		{
			AddVertex(new Vector3(x - 0.5f, y - 0.5f, z - 0.5f));
			AddVertex(new Vector3(x + 0.5f, y - 0.5f, z - 0.5f));
			AddVertex(new Vector3(x + 0.5f, y - 0.5f, z + 0.5f));
			AddVertex(new Vector3(x - 0.5f, y - 0.5f, z + 0.5f));
			AddTriangles();
			AddUVs(type, type.uvDown);
		}

		private void AddFaceUp(VoxelType type, int x, int y, int z)
		{
			AddVertex(new Vector3(x - 0.5f, y + 0.5f, z + 0.5f));
			AddVertex(new Vector3(x + 0.5f, y + 0.5f, z + 0.5f));
			AddVertex(new Vector3(x + 0.5f, y + 0.5f, z - 0.5f));
			AddVertex(new Vector3(x - 0.5f, y + 0.5f, z - 0.5f));
			AddTriangles();
			AddUVs(type, type.uvUp	);
		}

		private void AddFaceLeft(VoxelType type, int x, int y, int z)
		{
			AddVertex(new Vector3(x - 0.5f, y - 0.5f, z + 0.5f));
			AddVertex(new Vector3(x - 0.5f, y + 0.5f, z + 0.5f));
			AddVertex(new Vector3(x - 0.5f, y + 0.5f, z - 0.5f));
			AddVertex(new Vector3(x - 0.5f, y - 0.5f, z - 0.5f));
			AddTriangles();
			AddUVs(type, type.uvLeft);
		}

		private void AddFaceRight(VoxelType type, int x, int y, int z)
		{
			AddVertex(new Vector3(x + 0.5f, y - 0.5f, z - 0.5f));
			AddVertex(new Vector3(x + 0.5f, y + 0.5f, z - 0.5f));
			AddVertex(new Vector3(x + 0.5f, y + 0.5f, z + 0.5f));
			AddVertex(new Vector3(x + 0.5f, y - 0.5f, z + 0.5f));
			AddTriangles();
			AddUVs(type, type.uvRight);
		}

		private void AddVertex(Vector3 vertex)
		{
			vertices.Add(vertex);
			colliderVertices.Add(vertex);
		}

		private void AddTriangle(int tri)
		{
			triangles.Add(tri);
			colliderTriangles.Add(tri - (vertices.Count - colliderVertices.Count));
		}

		private void AddTriangles()
		{
			AddTriangle(vertices.Count - 4);
			AddTriangle(vertices.Count - 3);
			AddTriangle(vertices.Count - 2);
			AddTriangle(vertices.Count - 4);
			AddTriangle(vertices.Count - 2);
			AddTriangle(vertices.Count - 1);
		}

		private void AddUVs(VoxelType type, Vector2Int uv)
		{
			float size = grid.TileSize;

			uvs.Add(new Vector2(size * uv.x + size, size * uv.y));
			uvs.Add(new Vector2(size * uv.x + size, size * uv.y + size));
			uvs.Add(new Vector2(size * uv.x, size * uv.y + size));
			uvs.Add(new Vector2(size * uv.x, size * uv.y));
		}
	}
}