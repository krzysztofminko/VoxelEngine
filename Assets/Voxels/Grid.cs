using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Serialization;
using UnityEngine.SocialPlatforms;

namespace Voxels
{
	public class Grid : MonoBehaviour
	{
		[SerializeField]
		private Generator generator;
		[SerializeField]
		private Chunk chunkPrefab;
		[SerializeField]
		private Texture2D atlas;
		[SerializeField][Min(1)]
		private int atlasTilesInRow = 1;
		[SerializeField][Min(1)]
		private int loadChunksPerFrame = 5;
		[SerializeField][Min(1)]
		private int updateChunksPerFrame = 5;
		[SerializeField][Min(1)]
		private int chunkSize = 16;
		[SerializeField][Min(1)]
		private int _sizeX = 16 * 10;
		[SerializeField][Min(1)]
		private int _sizeY = 16 * 10;
		[SerializeField][Min(1)]
		private int _sizeZ = 16 * 10;
		[SerializeField]
		private VoxelType[] voxelTypes;

		private Chunk[] chunks;
		[ShowInInspector][HideInEditorMode]
		private List<Chunk> updateList;
		private Stopwatch stopWatch = new Stopwatch();

		public int SizeX { get => _sizeX; private set => _sizeX = value; }
		public int SizeY { get => _sizeY; private set => _sizeY = value; }
		public int SizeZ { get => _sizeZ; private set => _sizeZ = value; }
		[ShowInInspector][ReadOnly][HideInEditorMode]
		public int XChunks { get; private set; }
		[ShowInInspector][ReadOnly][HideInEditorMode]
		public int YChunks { get; private set; }
		[ShowInInspector][ReadOnly][HideInEditorMode]
		public int ZChunks { get; private set; }
		[ShowInInspector][ReadOnly][HideInEditorMode]
		public float TileSize { get; private set; }


		private void Awake()
		{
			SizeX = Mathf.Max(chunkSize, SizeX - SizeX % chunkSize);
			SizeY = Mathf.Max(chunkSize, SizeY - SizeY % chunkSize);
			SizeZ = Mathf.Max(chunkSize, SizeZ - SizeZ % chunkSize);
			XChunks = SizeX / chunkSize;
			YChunks = SizeY / chunkSize;
			ZChunks = SizeZ / chunkSize;
			TileSize = 1.0f / atlasTilesInRow;
			chunks = new Chunk[XChunks * YChunks * ZChunks];
			updateList = new List<Chunk>(chunks.Length);

			StartCoroutine(LoadChunks());

		}

		private IEnumerator LoadChunks()
		{
			//Load chunks
			UnityEngine.Debug.Log("Loading Chunks...");
			stopWatch.Restart();

			Chunk chunk;
			int loadedChunks = 0;

			//For every Chunk
			for (int x = 0; x < XChunks; x++)
				for (int z = 0; z < ZChunks; z++)
					for (int y = 0; y < YChunks; y++)
					{
						//Instantiate chunk
						chunk = Instantiate(chunkPrefab, transform.position + new Vector3(x * chunkSize, y * chunkSize, z * chunkSize), Quaternion.identity, transform);
						chunk.localPosition = new Vector3Int(x, y, z);
						chunk.grid = this;
						chunk.size = chunkSize;
						chunk.name = $"{chunkPrefab.name} {x}, {y}, {z}";
						chunk.InitializeData();
						SetChunk(x, y, z, chunk);

						//Generate chunk voxels
						if (generator)
						{
							//For every local Voxel
							for (int vx = 0; vx < chunkSize; vx++)
								for (int vz = 0; vz < chunkSize; vz++)
									for (int vy = 0; vy < chunkSize; vy++)
										chunk.SetVoxel(vx, vy, vz, generator.Generate(this, x * chunkSize + vx, y * chunkSize + vy, z * chunkSize + vz));
						}

						//Skip to next frame
						loadedChunks++;
						if (loadedChunks >= loadChunksPerFrame)
						{
							loadedChunks = 0;
							yield return null;
						}
					}

			//Update chunks faces
			UnityEngine.Debug.Log(stopWatch.Elapsed);
			UnityEngine.Debug.Log("Updating faces...");
			stopWatch.Restart();

			//For every Chunk
			for (int x = 0; x < XChunks; x++)
				for (int z = 0; z < ZChunks; z++)
					for (int y = 0; y < YChunks; y++)
					{
						GetChunk(x, y, z).UpdateAllFaces();

						//Skip to next frame
						loadedChunks++;
						if (loadedChunks >= loadChunksPerFrame)
						{
							loadedChunks = 0;
							yield return null;
						}
					}
			UnityEngine.Debug.Log(stopWatch.Elapsed);
			stopWatch.Stop();

			StartCoroutine(UpdateChunks());
		}

		private IEnumerator UpdateChunks()
		{
			while (true)
			{
				int updated = 0;
				for (int i = 0; i < updateChunksPerFrame; i++)
				{
					if (updateList.Count > 0)
					{
						updateList[0].UpdateMesh();
						updateList.RemoveAt(0);

						updated++;
					}
				}
				yield return null;
			}
		}

		private void SetChunk(int x, int y, int z, Chunk chunk)
		{
			chunks[x * ZChunks * YChunks + z * YChunks + y] = chunk;
		}

		private Chunk GetChunk(int x, int y, int z)
		{
			return chunks[x * ZChunks * YChunks + z * YChunks + y];
		}

		public void AddChunkToUpdateList(Chunk chunk)
		{
			if (!chunk.waitingForUpdate)
			{
				updateList.Add(chunk);
				chunk.waitingForUpdate = true;
			}
		}

		public void SetVoxel(int x, int y, int z, VoxelType type)
		{
			SetVoxel(x, y, z, GetVoxelTypeId(type));
		}

		public void SetVoxel(int x, int y, int z, byte id)
		{
			SetVoxel(x / chunkSize, y / chunkSize, z / chunkSize, x % chunkSize, y % chunkSize, z % chunkSize, id);
		}

		public void SetVoxel(int chunkX, int chunkY, int chunkZ, int localX, int localY, int localZ, byte id)
		{
			if (chunkX < 0 || chunkY < 0 || chunkZ < 0 || chunkX >= XChunks || chunkY >= YChunks || chunkZ >= ZChunks)
				return;
			if (localX < 0 || localY < 0 || localZ < 0 || localX >= chunkSize || localY >= chunkSize || localZ >= chunkSize)
				return;

			Chunk chunk = GetChunk(chunkX, chunkY, chunkZ);

			chunk.SetVoxel(localX, localY, localZ, id);
			chunk.UpdateAllFaces();

			if (chunkX > 0)
				GetChunk(chunkX - 1, chunkY, chunkZ).UpdateAllFaces();
			if (chunkX < XChunks - 1)
				GetChunk(chunkX + 1, chunkY, chunkZ).UpdateAllFaces();

			if (chunkY > 0)
				GetChunk(chunkX, chunkY - 1, chunkZ).UpdateAllFaces();
			if (chunkY < YChunks - 1)
				GetChunk(chunkX, chunkY + 1, chunkZ).UpdateAllFaces();

			if (chunkZ > 0)
				GetChunk(chunkX, chunkY, chunkZ - 1).UpdateAllFaces();
			if (chunkZ < ZChunks - 1)
				GetChunk(chunkX, chunkY, chunkZ + 1).UpdateAllFaces();
		}

		public byte GetVoxelId(int x, int y, int z)
		{
			if (x < 0 || y < 0 || z < 0 || x > SizeX - 1 || y > SizeY - 1 || z > SizeZ - 1)
				return  0;
			else
			{
				int vx = x - (x / chunkSize) * chunkSize;
				int vy = y - (y / chunkSize) * chunkSize;
				int vz = z - (z / chunkSize) * chunkSize;
				Chunk chunk = GetChunk(x / chunkSize, y / chunkSize, z / chunkSize);
				return chunk.GetVoxel(vx, vy, vz).id;
			}
		}

		public VoxelType GetVoxelType(int x, int y, int z)
		{
			return GetVoxelType(GetVoxelId(x, y, z));
		}

		public VoxelType GetVoxelType(byte id)
		{
			return voxelTypes[id];
		}

		public byte GetVoxelTypeId(VoxelType type)
		{
			for (int i = 0; i < voxelTypes.Length; i++)
				if (voxelTypes[i] == type)
					return (byte)i;
			UnityEngine.Debug.LogError($"Voxel type '{type}' not listed in Grid settings.", this);
			return 0;
		}
	}
}
