namespace takashicompany.Unity.Navigator
{
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using UnityEngine;

	public abstract class Map2dBuilder<T, U> : MonoBehaviour where T : Map2d<U>, new()
	{
		[SerializeField]
		private Transform _root;

		[SerializeField, Header("1マスの大きさ")]
		private Vector2 _unitPerGrid = Vector2.one;

		public Vector2 unitPerGrid => _unitPerGrid;

		// [SerializeField, Header("地面(歩行可能)として扱うレイヤー")]
		// private LayerMask _groundLayers;

		// [SerializeField, Header("歩行不可能として扱うレイヤー")]
		// private LayerMask _blockLayers;

		protected T _map;

		[ContextMenu("build map")]
		public T BuildMap()
		{
			_map = BuildMapByOverlapBox(_root, _unitPerGrid);

			return _map;
		}

		protected virtual void OnDrawGizmos()
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
					var color = GetGizmosGridColor(_map, point);
					color.a = 0.5f;
					Gizmos.color = color;
					
					var p = new Vector3(unitPerGrid.x * point.x, 0, unitPerGrid.y * point.y);

					Gizmos.DrawCube(p, unitPerGrid.ToV3XZ() * 0.975f);
				}
			}
		}

		public T BuildMapByOverlapBox(Transform root, Vector2 unitPerGrid)
		{
			if (root == null)
			{
				return null;
			}

			var map = new T();

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
					Process(map, new Vector2Int(x, z));
				}
			}

			return map;
		}

		protected abstract void Process(T map, Vector2Int p);
		protected abstract Color GetGizmosGridColor(T map, Vector2Int p);


		public static Vector2Int GetGridPosition(Vector3 p, Vector2 unitPerGrid)
		{
			var x = (int)System.Math.Round(p.x / unitPerGrid.x, System.MidpointRounding.AwayFromZero);
			var z = (int)System.Math.Round(p.z / unitPerGrid.y, System.MidpointRounding.AwayFromZero);		// zはyで割る

			return new Vector2Int(x, z);
		}
	}
}