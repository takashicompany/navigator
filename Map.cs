namespace takashicompany.Unity.Navigator
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

	public interface IMap2d<T>
	{
		// T Get(int x, int y);
		T Get(Vector2Int p);	// TODO 後でどちらかを拡張関数にする
		Vector2Int[] GetRoute(int fromX, int fromY, int toX, int toY);
		bool IsInBounds(Vector2Int p);
		// bool IsOutOfBounds(int x, int y);
		// int GetWidth();
		// int GetHeight();
		Vector2Int GetSize();	// TODO ここも後で拡張関数
	}

	public abstract class Map2d<T> : Map2d, IMap2d<T>
	{
		private T[,] _points;
		
		public Map2d(T[,] points)
		{
			_points = points;
		}

		// public T Get(int x, int y)
		// {
		// 	return _points[x, y];
		// }

		public T Get(Vector2Int p)
		{
			return _points[p.x, p.y];
		}
		
		public abstract Vector2Int[] GetRoute(int fromX, int fromY, int toX, int toY);

		public bool IsInBounds(Vector2Int p)
		{
			return 0 <= p.x && p.x < _points.GetLength(0) && 0 <= p.y && p.y < _points.GetLength(1);
		}

		// public bool IsOutOfBounds(int x, int y)
		// {
		// 	return !IsInBounds(x, y);
		// }

		// public int GetWidth()
		// {
		// 	return _points.GetLength(0);
		// }

		// public int GetHeight()
		// {
		// 	return _points.GetLength(1);
		// }

		public Vector2Int GetSize()
		{
			return new Vector2Int( _points.GetLength(0), _points.GetLength(1));
		}
	}
	
	/// <summary>
	/// 静的な2次元マップ
	/// </summary>
	public class StaticMap2d : Map2d<bool>
	{
		public static readonly int unreachableStep = int.MaxValue;

		private int?[,,,] _cachedStep;	// 使ってなくない？

		/// <summary>
		/// キーとなるマスから見た各マスへの歩数
		/// </summary>
		private Dictionary<Vector2Int, int[,]> _stepDict = new Dictionary<Vector2Int, int[,]>();

		/// <summary>
		/// キーとなるマスから見た各マスを距離でソートしたもの
		/// </summary>
		/// <returns></returns>
		private Dictionary<Vector2Int, IEnumerable<Vector2Int>> _pointsSortedByDistances = new Dictionary<Vector2Int, IEnumerable<Vector2Int>>();

		public StaticMap2d(bool[,] points) : base(points)
		{
			var size = GetSize();
			_cachedStep = new int?[size.x, size.y, size.x, size.y];
		}

		/// <summary>
		/// 事前に各マスから各マスへの歩数を計算しておく
		/// </summary>
		public void PrepareStepCache()
		{
			var size = GetSize();
			for (var x = 0; x < size.x; x++)
			{
				for (var y = 0; y < size.y; y++)
				{
					GetSteps(new Vector2Int(x, y));
				}
			}
		}

		private void RegistSteps(Vector2Int point, int[,] steps, IEnumerable<Vector2Int> pointsSortedByDistance)
		{
			_stepDict[point] = steps;

			for (var x = 0; x < steps.GetLength(0); x++)
			{
				for (var y = 0; y < steps.GetLength(1); y++)
				{
					var step = steps[x, y];
					_cachedStep[point.x, point.y, x, y] = step;
					_cachedStep[x, y, point.x, point.y] = step;
				}
			}

			_pointsSortedByDistances[point] = pointsSortedByDistance;
		}

		private bool TryGetCache(Vector2Int point, out int[,] steps, out IEnumerable<Vector2Int> pointsSortedByDistance)
		{
			if (_stepDict.TryGetValue(point, out steps))
			{
				pointsSortedByDistance = _pointsSortedByDistances[point];
				return true;
			}

			pointsSortedByDistance = null;

			return false;
		}

		public int[,] GetSteps(Vector2Int to, int iteration = 4, bool useCache = true)
		{
			return GetSteps(to, out _, iteration, useCache);
		}
		
		/// <summary>
		/// あるマスからの歩数を計算する
		/// </summary>
		public int[,] GetSteps(Vector2Int point, out List<Vector2Int> pointsSortedByDinstance, int iteration = 4, bool useCache = true)		// stepは都度生成せずに対象の値を持っておけば使い回せる気がする
		{
			if (useCache && TryGetCache(point, out var steps, out var psd))
			{
				pointsSortedByDinstance = psd.ToList();
				return steps;
			}
			var size = GetSize();
			var width = size.x;
			var height = size.y;
			
			// 全マスをハッシュセットに登録
			var posHashSet = new HashSet<Vector2Int>();

			for (var x = 0; x < width; x++)
			{
				for (var y = 0; y < height; y++)
				{
					posHashSet.Add(new Vector2Int(x, y));
				}
			}
			
			// 全マスを対象のマスとの距離でソートする
			pointsSortedByDinstance= posHashSet.OrderBy(p => Vector2.Distance(p, point)).ToList();
			var positionsCount = pointsSortedByDinstance.Count;

			steps = new int[width, height];

			for (var x = 0; x < width; x++)
			{
				for (var y = 0; y < height; y++)
				{
					// 一旦、対象のマスからの距離を最大値にしておく
					steps[x, y] = unreachableStep;
				}
			}

			steps[point.x, point.y] = 0;	// 目的地なので距離は0

			for (int i = 0; i < iteration; i++)
			{
				for (int j = 0; j < positionsCount; j++)
				{
					var pointIndex = i % 2 == 0 ? j : positionsCount - 1 - j;	// iが奇数のときは逆から計算する
					var p = pointsSortedByDinstance[pointIndex];

					// 目的地は計算しない
					if (p == point)
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

			RegistSteps(point, steps, pointsSortedByDinstance.ToArray());

			return steps;
		}

		public List<Vector2Int> GetReachablePoints(Vector2Int from, int iteration = 4)
		{
			var steps = GetSteps(from, iteration);

			return GetReachablePoints(steps, from);
		}

		public List<Vector2Int> GetReachablePoints(int[,] steps, Vector2Int from)
		{
			var reachables = new List<Vector2Int>();
			steps.Foreach((p, step) =>
			{
				if (CanGoTo(p) && steps[p.x, p.y] != unreachableStep)
				{
					reachables.Add(p);
				}
			});

			return reachables;
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

		public bool TryGetRoute(Vector2Int from, Vector2Int to, out Vector2Int[] route, bool enableSlant = false, int iteration = 4)
		{
			route = GetRoute(from, to, enableSlant, iteration);
			return route != null && route.Length > 0;
		}

		/// <summary>
		/// 経路を取得する
		/// </summary>
		public Vector2Int[] GetRoute(Vector2Int from, Vector2Int to, bool enableSlant = false, int iteration = 4)
		{
			var steps = GetSteps(to, iteration);

			return GetRoute(steps, from, to, enableSlant);
		}

		public Vector2Int[] GetRoute(int[,] steps, Vector2Int from, Vector2Int to, bool enableSlant = false)
		{

			if (steps[from.x, from.y] == unreachableStep)
			{
				Debug.LogError(from + "は到達できない場所です。");
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
			if (this.IsOutOfBounds(p))
			{
				return false;
			}

			return Get(p);
		}

		public bool CanGoTo(int step)
		{
			return step != unreachableStep;
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

		public static bool IsOutOfBounds<T>(this Map2d<T> map, Vector2Int p)
		{
			return !map.IsInBounds(p);
		}
	}

}