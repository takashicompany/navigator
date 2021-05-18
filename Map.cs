namespace TakashiCompany.Unity.Navigator
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using System.Linq;

	public abstract class Map2d
	{
		public enum Direction
		{
			Foward,
			Right,
			Back,
			Left
		}

		// System.Enum のGetValuesとかしてもいいけど、重くないか心配なので
		public static readonly Direction[] directions = new Direction[] { Direction.Foward, Direction.Right, Direction.Back, Direction.Left };
	}

	public abstract class Map2d<T> : Map2d
	{
		private T[,] _points;
		
		public Map2d(T[,] points)
		{
			_points = points;
		}

		public T Get(int x, int y)
		{
			return _points[x, y];
		}
		
		public abstract Vector2Int[] GetRoute(int fromX, int fromY, int toX, int toY);

		public bool IsInBounds(int x, int y)
		{
			return 0 <= x && x < _points.GetLength(0) && 0 <= y && y < _points.GetLength(1);
		}

		public bool IsOutOfBounds(int x, int y)
		{
			return !IsInBounds(x, y);
		}

		public int GetWidth()
		{
			return _points.GetLength(0);
		}

		public int GetHeight()
		{
			return _points.GetLength(1);
		}
	}

	public class SimpleMap2d : Map2d<bool>
	{
		public SimpleMap2d(bool[,] points) : base(points)
		{

		}

		private int[,] GetSteps(Vector2Int to)
		{
			var width = GetWidth();
			var height = GetHeight();
			
			var posHashSet = new HashSet<Vector2Int>();

			for (var x = 0; x < width; x++)
			{
				for (var y = 0; y < height; y++)
				{
					posHashSet.Add(new Vector2Int(x, y));
				}
			}
			
			var positionsByDistance = posHashSet.OrderBy(p => Vector2.Distance(p, to)).ToList();

			var steps = new int[width, height];

			for (var x = 0; x < width; x++)
			{
				for (var y = 0; y < height; y++)
				{
					steps[x, y] = int.MaxValue;
				}
			}

			steps[to.x, to.y] = 0;	// 目的地なので距離は0

			var count = 4;

			for (int i = 0; i < count; i++)
			{
				foreach (var p in positionsByDistance)
				{
					// 目的地は計算しない
					if (p == to)
					{
						continue;
					}

					// 通行できないマスはそもそも計算しない
					if (!CanGoTo(p))
					{
						continue;
					}

					int distance = int.MaxValue - 1;

					foreach (var d in Map2d.directions)
					{
						var current = p + d.ToV2Int();

						// 通行できないマスは参照しない
						if (!CanGoTo(current))
						{
							continue;
						}

						if (distance > steps[current.x, current.y])
						{
							distance = steps[current.x, current.y];
						}

					}

					distance++;	// 一番低いマスから

					steps[p.x, p.y] = distance;
				}

				positionsByDistance.Reverse();
			}

			return steps;
		}


		// private class Walker
		// {
		// 	private SimpleMap2d _map;
		// 	private Vector2Int _dest;
		// 	private HashSet<Vector2Int> _route;
		// 	private HashSet<Walker> _goal;

		// 	public Walker(SimpleMap2d map, Vector2Int dest, HashSet<Vector2Int> route, HashSet<Walker> goal)
		// 	{
		// 		_map = map;
		// 		_dest = dest;
		// 		_route = route;
		// 		_goal = goal;
		// 	}

		// 	private Vector2Int Current()
		// 	{
		// 		return _route.Last();
		// 	}

		// 	public bool TryWalk()
		// 	{
		// 		var current = Current();

		// 		int count = 0;

		// 		foreach (var d in Map2d.directions)
		// 		{
					
		// 		}
		// 	}

		// 	private bool CanGoTo(int x, int y)
		// 	{
		// 		return CanGoTo(x, y) && !_route.Contains(new Vector2Int(x, y));
		// 	}
			
		// }

		// public override Vector2Int[] GetRoute(int fromX, int fromY, int toX, int toY)
		// {
			
		// }

		public bool CanGoTo(Vector2Int p)
		{
			return CanGoTo(p.x, p.y);
		}

		public bool CanGoTo(int x, int y)
		{
			if (IsOutOfBounds(x, y))
			{
				return false;
			}

			return Get(x, y);
		}

		public override Vector2Int[] GetRoute(int fromX, int fromY, int toX, int toY)
		{
			throw new System.NotImplementedException();
		}
	}

	public static class MapExtension
	{
		public static Vector2Int ToV2Int(this Map2d.Direction self)
		{
			switch (self)
			{
				case Map2d.Direction.Foward	:	return Vector2Int.up;
				case Map2d.Direction.Right	:	return Vector2Int.right;
				case Map2d.Direction.Back	:	return Vector2Int.down;
				case Map2d.Direction.Left	:	return Vector2Int.left;
			}

			throw new System.NotImplementedException();
		}
	}

}