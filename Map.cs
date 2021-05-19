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
			None,
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

		public T Get(Vector2Int p)
		{
			return Get(p.x, p.y);
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
		public static readonly int unreachableStep = int.MaxValue;

		public SimpleMap2d(bool[,] points) : base(points)
		{

		}
		
		/// <summary>
		/// 各マスの歩数を取得する
		/// </summary>
		public int[,] GetSteps(Vector2Int to, int iteration = 4)
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
			var positionsCount = positionsByDistance.Count;

			var steps = new int[width, height];

			for (var x = 0; x < width; x++)
			{
				for (var y = 0; y < height; y++)
				{
					steps[x, y] = unreachableStep;
				}
			}

			steps[to.x, to.y] = 0;	// 目的地なので距離は0

			for (int i = 0; i < iteration; i++)
			{
				for (int j = 0; j < positionsCount; j++)
				{
					var pointIndex = i % 2 == 0 ? j : positionsCount - 1 - j;	// iが奇数のときは逆から計算する
					var p = positionsByDistance[pointIndex];

					// 目的地は計算しない
					if (p == to)
					{
						continue;
					}

					var step = CalcStepByNexts(steps, p);

					steps[p.x, p.y] = step;
				}

				if (iteration / 2 - 1 == i)		// 前後半の合間に一度未到達領域を整理する
				{
					TryFillUnreachable(steps);
				}
			}

			return steps;
		}

		/// <summary>
		/// マス毎の歩数を見て、未到達領域を再計算する。計算が進捗しなくなったら終わり
		/// </summary>
		public int TryFillUnreachable(int[,] steps)
		{
			var unreachables = new HashSet<Vector2Int>();

			for(int x = 0; x < steps.GetLength(0); x++)
			{
				for (int y = 0; y < steps.GetLength(1); y++)
				{
					if (steps[x, y] == unreachableStep)
					{
						unreachables.Add(new Vector2Int(x, y));
					}
				}
			}

			if (unreachables.Count == 0)
			{
				return 0;
			}

			var firstUnreachables = unreachables.Count;

			var prevUnreachable = firstUnreachables;

			var deletes = new HashSet<Vector2Int>();

			do
			{
				prevUnreachable = unreachables.Count;

				foreach (var p in unreachables)
				{
					var step = CalcStepByNexts(steps, p);

					if (step != unreachableStep)
					{
						deletes.Add(p);
						steps[p.x, p.y] = step;
					}
				}

				foreach (var d in deletes)
				{
					unreachables.Remove(d);
				}

			} while(prevUnreachable != unreachables.Count);		// 成果が出る限り続ける

			return firstUnreachables - unreachables.Count;
		}

		/// <summary>
		/// 対象のマスの上下左右から一番低い歩数を見つけて、それに1足したものを返す
		/// </summary>
		public int CalcStepByNexts(int[,] steps, Vector2Int target)
		{
			if (TryGetLowerestStepByNexts(steps, target, out var lowerestStep, out _))
			{
				if (lowerestStep != unreachableStep) lowerestStep++;

				return lowerestStep;
			}

			return unreachableStep;
		}

		/// <summary>
		/// 対象のマスの四方を見て、一番歩数の低いマスと歩数を返す
		/// </summary>
		private bool TryGetLowerestStepByNexts(int[,] steps, Vector2Int target, out int lowerestStep, out Direction direction)
		{
			direction = Direction.None;

			// 通行できないマスはそもそも計算しない
			if (!CanGoTo((Vector2Int)target))
			{
				lowerestStep = unreachableStep;
				return false;
			}

			lowerestStep = unreachableStep;

			foreach (var d in Map2d.directions)
			{
				var current = target + d.ToV2Int();

				// 通行できないマスは参照しない
				if (!CanGoTo(current))
				{
					continue;
				}

				if (lowerestStep > steps[current.x, current.y])
				{
					lowerestStep = steps[current.x, current.y];
					direction = d;
				}
			}

			return lowerestStep != unreachableStep;

		}

		public override Vector2Int[] GetRoute(int fromX, int fromY, int toX, int toY)
		{
			return GetRoute(new Vector2Int(fromX, fromY), new Vector2Int(toX, toY));
		}

		/// <summary>
		/// 経路を取得する
		/// </summary>
		public Vector2Int[] GetRoute(Vector2Int from, Vector2Int to, bool enableSlant = false, int iteration = 4)
		{
			var steps = GetSteps(to, iteration);

			if (steps[from.x, from.y] == unreachableStep)
			{
				return null;
			}

			var route = new List<Vector2Int>() { from };
			
			var current = from;

			while (current != to)
			{
				if (!TryGetLowerestStepByNexts(steps, current, out _, out var direction))
				{
					// 無いとは思うけど...
					break;
				}

				current += direction.ToV2Int();

				route.Add(current);
			}

			// TODO 斜め移動を許容するオプション
			if (enableSlant)
			{
				for (int i = 0; i < route.Count - 2; i++)
				{
					var now = route[i];
					var next2 = route[i + 2];
					
					// 2マス先は斜めじゃない
					if (Mathf.Abs(now.x - next2.x) >= 2 || Mathf.Abs(now.y - next2.y) >= 2)
					{
						continue;
					}

					var dir = next2 - now;

					var a = now + Vector2Int.right * dir.x;
					var b = now + Vector2Int.up * dir.y;

					// 隣が通過可能だったら、いきなり斜めに移動しちゃう
					if (CanGoTo(a) && CanGoTo(b))
					{
						route.RemoveAt(i + 1);
					}
				}
			}

			return route.ToArray();
		}

		/// <summary>
		/// 対象のマスは通行可能か
		/// </summary>
		public bool CanGoTo(Vector2Int p)
		{
			return CanGoTo(p.x, p.y);
		}

		/// <summary>
		/// 対象のマスは通行可能か
		/// </summary>
		public bool CanGoTo(int x, int y)
		{
			if (IsOutOfBounds(x, y))
			{
				return false;
			}

			return Get(x, y);
		}
	}

	public static class MapExtension
	{
		/// <summary>
		/// 方向をVector2Intに変換
		/// </summary>
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