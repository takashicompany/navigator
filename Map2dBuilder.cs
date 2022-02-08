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
			// if (_map == null)
			// {
			// 	Gizmos.color = Color.cyan;
				
			// 	_grids.Foreach(v2int =>
			// 	{
			// 		var p = Utils.GetPositionOnGrid(_grids, v2int, _unitPerGrid).ToV3XZ();
			// 		Gizmos.DrawWireCube(p, _unitPerGrid.ToV3XZ());
			// 	});
			// }
			// else
			// {
				
			// 	_grids.Foreach(v2int =>
			// 	{
			// 		var reachable = _map[v2int];
					
			// 		var p = Utils.GetPositionOnGrid(_grids, v2int, _unitPerGrid).ToV3XZ();

			// 		if (reachable)
			// 		{
			// 			Gizmos.color = Color.cyan;
			// 			Gizmos.DrawWireCube(p, _unitPerGrid.ToV3XZ());
			// 		}
			// 		else
			// 		{
			// 			Gizmos.color = Color.red;
			// 			Gizmos.DrawWireCube(p, _unitPerGrid.ToV3XZ());
			// 		}
			// 	});
			// }
		}

		public static Map2d<bool> BuildMapByOverlapBox(Transform root, Vector2 unitPerGrid, LayerMask groundLayers, LayerMask blockLayers)
		{
			var colliders = root.GetComponentsInChildren<Collider>();

			var boundsList = new List<Bounds>();

			foreach (var c in colliders)
			{
				boundsList.Add(c.bounds);
			}

			// var bounds = boundsList.

			// var points = new bool[grids.x, grids.y];

			// var boxSize = unitPerGrid.ToV3XZ();
			// boxSize.y = 100f;	// 100は適当に設定しました。

			// grids.Foreach(v2int =>
			// {
			// 	var p = Utils.GetPositionOnGrid(grids, v2int, unitPerGrid).ToV3XZ();


			// 	var blocks = Physics.OverlapBox(p, boxSize / 2 * 0.99f, Quaternion.identity, blockLayers);

			// 	if (blocks != null && blocks.Length > 0)
			// 	{
			// 		points[v2int.x, v2int.y] = false;
			// 		return;
			// 	}

			// 	var grounds = Physics.OverlapBox(p, boxSize / 2 * 0.99f, Quaternion.identity, groundLayers);

			// 	if (grounds != null && grounds.Length > 0)
			// 	{
			// 		points[v2int.x, v2int.y] = true;
			// 		return;
			// 	}
			// });

			// return new Map2d<bool>(points);

			return null;
		}
	}
}