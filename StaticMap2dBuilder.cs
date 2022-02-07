namespace takashicompany.Unity.Navigator
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	

	public class StaticMap2dBuilder : MonoBehaviour
	{
		[SerializeField]
		private Vector2Int _grids = new Vector2Int(4, 4);

		[SerializeField]
		private Vector2 _unitPerGrid = Vector2.one;

		public Vector2 unitPerGrid => _unitPerGrid;

		[SerializeField]
		private LayerMask _groundLayers;

		[SerializeField]
		private LayerMask _blockLayers;

		private SimpleNavigator _map;

		[ContextMenu("build map")]
		public SimpleNavigator BuildMap()
		{
			_map = BuildMapByOverlapBox(_grids, _unitPerGrid, _groundLayers, _blockLayers);

			return _map;
		}

		private void OnDrawGizmos()
		{
			if (_map == null)
			{
				Gizmos.color = Color.cyan;
				
				_grids.Foreach(v2int =>
				{
					var p = Utils.GetPositionOnGrid(_grids, v2int, _unitPerGrid).ToV3XZ();
					Gizmos.DrawWireCube(p, _unitPerGrid.ToV3XZ());
				});
			}
			else
			{
				
				_grids.Foreach(v2int =>
				{
					var reachable = _map.Get(v2int);
					
					var p = Utils.GetPositionOnGrid(_grids, v2int, _unitPerGrid).ToV3XZ();

					if (reachable)
					{
						Gizmos.color = Color.cyan;
						Gizmos.DrawWireCube(p, _unitPerGrid.ToV3XZ());
					}
					else
					{
						Gizmos.color = Color.red;
						Gizmos.DrawWireCube(p, _unitPerGrid.ToV3XZ());
					}
				});
			}
		}

		public static SimpleNavigator BuildMapByOverlapBox(Vector2Int grids, Vector2 unitPerGrid, LayerMask groundLayers, LayerMask blockLayers)
		{
			var points = new bool[grids.x, grids.y];

			var boxSize = unitPerGrid.ToV3XZ();
			boxSize.y = 100f;	// 100は適当に設定しました。

			grids.Foreach(v2int =>
			{
				var p = Utils.GetPositionOnGrid(grids, v2int, unitPerGrid).ToV3XZ();


				var blocks = Physics.OverlapBox(p, boxSize / 2 * 0.99f, Quaternion.identity, blockLayers);

				if (blocks != null && blocks.Length > 0)
				{
					points[v2int.x, v2int.y] = false;
					return;
				}

				var grounds = Physics.OverlapBox(p, boxSize / 2 * 0.99f, Quaternion.identity, groundLayers);

				if (grounds != null && grounds.Length > 0)
				{
					points[v2int.x, v2int.y] = true;
					return;
				}
			});

			return new SimpleNavigator(points);
		}
	}
}