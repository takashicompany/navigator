namespace takashicompany.Unity.Navigator
{
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using UnityEngine;

	public abstract class ExtensionableMap2d : Map2d
	{
		
	}

	public abstract class ExtensionableMap2d<T> : ExtensionableMap2d
	{
		private Dictionary<Vector2Int, T> _points;

		private Vector2Int _min;
		private Vector2Int _max;

		public ExtensionableMap2d(Dictionary<Vector2Int, T> points)
		{
			_points = points;
		}

		protected void UpdateSize()
		{
			var minX = int.MaxValue;
			var minY = int.MaxValue;

			var maxX = int.MinValue;
			var maxY = int.MinValue;

			foreach (var kvp in _points)
			{
				var pos = kvp.Key;

				if (pos.x < minX)
				{
					minX = pos.x;
				}

				if (pos.y < minY)
				{
					minY = pos.y;
				}

				if (pos.x > maxX)
				{
					pos.x = maxX;
				}

				if (pos.y > maxY)
				{
					pos.y = maxY;
				}
			}

			_min = new Vector2Int(minX, minY);
			_max = new Vector2Int(maxX, maxY);
		}

		protected void UpdateSize(Vector2Int pos)
		{
			var minX = _min.x;
			var minY = _min.y;

			var maxX = _max.x;
			var maxY = _max.y;

			if (pos.x < minX)
			{
				minX = pos.x;
			}

			if (pos.y < minY)
			{
				minY = pos.y;
			}

			if (pos.x > maxX)
			{
				pos.x = maxX;
			}

			if (pos.y > maxY)
			{
				pos.y = maxY;
			}

			_min = new Vector2Int(minX, minY);
			_max = new Vector2Int(maxX, maxY);
		}

		public T Get(Vector2Int p)
		{
			return _points[p];
		}
		
		public abstract Vector2Int[] GetRoute(int fromX, int fromY, int toX, int toY);

		public bool IsInBounds(int x, int y)
		{
			// return 0 <= x && x < _points.GetLength(0) && 0 <= y && y < _points.GetLength(1);

			return _min.x <= x && x <= _max.x && _min.y <= y && y <= _min.y;
		}

		public bool IsOutOfBounds(int x, int y)
		{
			return !IsInBounds(x, y);
		}

		public int GetWidth()
		{
			return _max.x - _min.x;
		}

		public int GetHeight()
		{
			return _max.y - _min.y;
		}

		public Vector2Int GetSize()
		{
			return new Vector2Int(GetWidth(), GetHeight());
		}
	}

	public class SimpleExtensionableMap2d : ExtensionableMap2d<bool>
	{
		public static readonly int unreachableStep = int.MaxValue;

		private Dictionary<Vector2Int, Dictionary<Vector2Int, int>> _cachedStep; // int?[,,,] _cachedStep;

		private Dictionary<Vector2Int, Dictionary<Vector2Int, int>> _stepDict = new Dictionary<Vector2Int, Dictionary<Vector2Int, int>>();

		private Dictionary<Vector2Int, IEnumerable<Vector2Int>> _pointsSortedByDistances = new Dictionary<Vector2Int, IEnumerable<Vector2Int>>();

		public SimpleExtensionableMap2d(Dictionary<Vector2Int, bool> points) : base(points)
		{

		}

		/// <summary>
		/// 事前に各マスから各マスへの歩数を計算しておく
		/// </summary>
		public void PrepareStepCache()
		{
			for (var x = 0; x < GetWidth(); x++)
			{
				for (var y = 0; y < GetHeight(); y++)
				{
					GetSteps(new Vector2Int(x, y));
				}
			}
		}

		
		private void RegistSteps(Vector2Int point, Dictionary<Vector2Int, int> steps, IEnumerable<Vector2Int> pointsSortedByDistance)
		{
			_stepDict[point] = steps;

			_pointsSortedByDistances[point] = pointsSortedByDistance;
		}

		private bool TryGetCache(Vector2Int point, out Dictionary<Vector2Int, int> steps, out IEnumerable<Vector2Int> pointsSortedByDistance)
		{
			if (_stepDict.TryGetValue(point, out steps))
			{
				pointsSortedByDistance = _pointsSortedByDistances[point];
				return true;
			}

			pointsSortedByDistance = null;

			return false;
		}

		public Dictionary<Vector2Int, int> GetSteps(Vector2Int to, int iteration = 4, bool useCache = true)
		{
			return GetSteps(to, out _, iteration, useCache);
		}
		
		/// <summary>
		/// あるマスからの歩数を計算する
		/// </summary>
		public Dictionary<Vector2Int, int> GetSteps(Vector2Int point, out List<Vector2Int> pointsSortedByDinstance, int iteration = 4, bool useCache = true)		// stepは都度生成せずに対象の値を持っておけば使い回せる気がする
		{
			if (useCache && TryGetCache(point, out var steps, out var psd))
			{
				pointsSortedByDinstance = psd.ToList();
				return steps;
			}

			var width = GetWidth();
			var height = GetHeight();
			
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

			steps = new Dictionary<Vector2Int, int>();

			for (var x = 0; x < width; x++)
			{
				for (var y = 0; y < height; y++)
				{
					// 一旦、対象のマスからの距離を最大値にしておく
					steps[new Vector2Int(x, y)] = unreachableStep;
				}
			}

			steps[new Vector2Int(point.x, point.y)] = 0;	// 目的地なので距離は0

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

					steps[p] = step;
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

		public List<Vector2Int> GetReachablePoints(Dictionary<Vector2Int, int> steps, Vector2Int from)
		{
			var reachables = new List<Vector2Int>();
			
			steps.Foreach((p, step) =>
			{
				if (CanGoTo(p) && steps[p] != unreachableStep)
				{
					reachables.Add(p);
				}
			});

			return reachables;
		}

		/// <summary>
		/// マス毎の歩数を見て、未到達領域を再計算する。計算が進捗しなくなったら終わり
		/// </summary>
		public int TryFillUnreachable(Dictionary<Vector2Int, int> steps)
		{
			var unreachables = new HashSet<Vector2Int>();

			steps.Foreach((point, step) =>
			{
				 if (step == unreachableStep)
				{
					unreachables.Add(point);
				}
			});

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
						steps[p] = step;
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
		public int CalcStepByNexts(Dictionary<Vector2Int, int> steps, Vector2Int target)
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
		private bool TryGetLowerestStepByNexts(Dictionary<Vector2Int, int> steps, Vector2Int target, out int lowerestStep, out Direction direction)
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

				if (lowerestStep > steps[current])
				{
					lowerestStep = steps[current];
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

		public Vector2Int[] GetRoute(Dictionary<Vector2Int, int> steps, Vector2Int from, Vector2Int to, bool enableSlant = false)
		{

			if (steps[from] == unreachableStep)
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

			return Get(new Vector2Int(x, y));
		}

		public bool CanGoTo(int step)
		{
			return step != unreachableStep;
		}
	}
}