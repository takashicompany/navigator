namespace takashicompany.Unity.Navigator
{
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using UnityEngine;

	public class SimpleMap2dBuilder : Map2dBuilder<bool>
	{

		[SerializeField, Header("地面(歩行可能)として扱うレイヤー")]
		private LayerMask _groundLayers;

		[SerializeField, Header("歩行不可能として扱うレイヤー")]
		private LayerMask _blockLayers;

		
		public override void Process(int x, int z, Map2d<bool> map)
		{
			var half = unitPerGrid / 2f;

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

		protected override Color GetGizmosGridColor(Vector2Int p, Map2d<bool> map)
		{
			if (map.TryGet(p, out var walkable))
			{
				return walkable ? Color.green : Color.red;
			}

			return Color.clear;
		}
    }
}