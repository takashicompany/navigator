namespace takashicompany.Unity.Navigator
{
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using UnityEngine;

	public class SimpleMap2dBuilder : Map2dBuilder<Map2d<bool>, bool>
	{

		[SerializeField, Header("地面(歩行可能)として扱うレイヤー")]
		private LayerMask _groundLayers;

		[SerializeField, Header("歩行不可能として扱うレイヤー")]
		private LayerMask _blockLayers;

		
		protected override void Process(Map2d<bool> map, Vector2Int point)
		{
			var half = unitPerGrid / 2f;

			var x = point.x;
			var z = point.y;

			var p = new Vector3(unitPerGrid.x * x, 0, unitPerGrid.y * z);

			var blocks = Physics.OverlapBox(p, half * 0.99f, Quaternion.identity, _blockLayers);

			if (blocks != null && blocks.Length > 0)
			{
				map[x, z] = false;
				return;
			}

			var ground = Physics.OverlapBox(p, half * 0.99f, Quaternion.identity, _groundLayers);

			if (ground != null && ground.Length > 0)
			{
				map[x, z] = true;
				return;
			}

			map[x, z] = false;
		}

		protected override Color GetGizmosGridColor(Map2d<bool> map, Vector2Int p)
		{
			if (map.TryGet(p, out var walkable))
			{
				return walkable ? Color.green : Color.red;
			}

			return Color.clear;
		}
    }
}