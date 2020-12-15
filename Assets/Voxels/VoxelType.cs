using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

namespace Voxels
{
	[CreateAssetMenu()]
	public class VoxelType : ScriptableObject
	{	public bool solid;
		public Vector2Int uvBackward;
		public Vector2Int uvForward;
		public Vector2Int uvDown;
		public Vector2Int uvUp;
		public Vector2Int uvLeft;
		public Vector2Int uvRight;

		[ShowInInspector]
		[HideLabel]
		[PreviewField(250, ObjectFieldAlignment.Center)]
		public static Texture previewAtlas;
		
	}
}