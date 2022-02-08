namespace takashicompany.Unity.Navigator
{
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using UnityEngine;

	public class Map2dBuilder : MonoBehaviour
	{
		[SerializeField]
		private Transform _root;

		[SerializeField, Header("1マスの大きさ")]
		private Vector2 _unitPerGrid = Vector2.one;

		public Vector2 unitPerGrid => _unitPerGrid;

		[SerializeField, Header("地面(歩行可能)として扱うレイヤー")]
		private LayerMask _groundLayers;

		[SerializeField, Header("歩行不可能として扱うレイヤー")]
		private LayerMask _blockLayers;

		private Map2d<bool> _map;

		[ContextMenu("build map")]
		public Map2d<bool> BuildMap()
		{
			_map = BuildMapByOverlapBox(_root, _unitPerGrid, _groundLayers, _blockLayers);

			return _map;
		}

		private void OnDrawGizmos()
		{
			if (!Application.isPlaying)
			{
				BuildMap();
			}

			if (_map != null)
			{
				foreach (var kvp in _map)
				{
					var point = kvp.Key;
					var walkable = kvp.Value;

					Gizmos.color = walkable ? Color.green : Color.red;

					var p = new Vector3(unitPerGrid.x * point.x, 0, unitPerGrid.y * point.y);

					Gizmos.DrawCube(p, unitPerGrid.ToV3XZ() * 0.975f);
				}
			}
		}

		public static Map2d<bool> BuildMapByOverlapBox(Transform root, Vector2 unitPerGrid, LayerMask groundLayers, LayerMask blockLayers)
		{
			if (root == null)
			{
				return null;
			}

			var map = new Map2d<bool>();

			var colliders = root.GetComponentsInChildren<Collider>();

			var bounds = new Bounds();

			foreach (var c in colliders)
			{
				bounds.Encapsulate(c.bounds);
			}

			var min = GetGridPosition(bounds.min, unitPerGrid);
			var max = GetGridPosition(bounds.max, unitPerGrid);

			var half = unitPerGrid / 2f;
			half.y = 10f; // 適当な値。

			for (int x = min.x; x <= max.x; x++)
			{
				for (int z = min.y; z <= max.y; z++)
				{
					var p = new Vector3(unitPerGrid.x * x, 0, unitPerGrid.y * z);

					var blocks = Physics.OverlapBox(p, half * 0.99f, Quaternion.identity, blockLayers);

					if (blocks != null && blocks.Length > 0)
					{
						map[x, z] = false;
						continue;
					}

					var ground = Physics.OverlapBox(p, half * 0.99f, Quaternion.identity, groundLayers);

					if (ground != null && ground.Length > 0)
					{
						map[x, z] = true;
						continue;
					}

					map[x, z] = false;
				}
			}

			return map;
		}

		public static Vector2Int GetGridPosition(Vector3 p, Vector2 unitPerGrid)
		{
			var x = (int)System.Math.Round(p.x / unitPerGrid.x, System.MidpointRounding.AwayFromZero);
			var z = (int)System.Math.Round(p.z / unitPerGrid.y, System.MidpointRounding.AwayFromZero);		// zはyで割る

			return new Vector2Int(x, z);
		}
	}
}