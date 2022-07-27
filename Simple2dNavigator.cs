namespace takashicompany.Unity.Navigator
{
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using UnityEngine;

	public interface INavigator
	{
		Vector2Int[] GetRoute(Vector2Int from, Vector2Int to);
		bool IsInBounds(Vector2Int p);
		Vector2Int GetSize();
	}

	public interface INavigator<T> : INavigator
	{
		bool TryGet(Vector2Int p, out T v);
	}

	public interface ICustomNavigator : INavigator
	{
		Vector2Int[] GetRoute(Vector2Int from, Vector2Int to, bool enableSlant = false, int iteration = 4, bool useCache = true);
	}

	public abstract class Navigator2d<T> : INavigator<T>
	{
		protected Map2d<T> _map;
		
		public Navigator2d(Map2d<T> map)
		{
			_map = map;
		}

		public bool TryGet(Vector2Int p, out T v)
		{
			return _map.TryGet(p, out v);
		}
		
		public abstract Vector2Int[] GetRoute(Vector2Int from, Vector2Int to);

		public bool IsInBounds(Vector2Int p)
		{
			return _map.IsInBounds(p);
		}

		public Vector2Int GetSize()
		{
			return _map.GetSize();
		}
	}
	
	/// <summary>
	/// 静的な2次元マップ
	/// </summary>
	public abstract class SimpleNavigator2d<T> : Navigator2d<T>, ICustomNavigator
	{
		public static readonly int unreachableStep = int.MaxValue;

		/// <summary>
		/// キーとなるマスから見た各マスへの歩数
		/// </summary>
		private Dictionary<Vector2Int, Map2d<int>> _stepDict = new Dictionary<Vector2Int, Map2d<int>>();

		/// <summary>
		/// キーとなるマスから見た各マスを距離でソートしたもの
		/// </summary>
		/// <returns></returns>
		private Dictionary<Vector2Int, IEnumerable<Vector2Int>> _pointsSortedByDistances = new Dictionary<Vector2Int, IEnumerable<Vector2Int>>();

		public SimpleNavigator2d(Map2d<T> map) : base(map)
		{
			var size = GetSize();
		}

		/// <summary>
		/// 事前に各マスから各マスへの歩数を計算しておく
		/// </summary>
		public void PrepareStepCache()
		{
			var size = GetSize();

			foreach (var kvp in _map)
			{
				GetSteps(kvp.Key);
			}
		}

		private void RegistSteps(Vector2Int point, Map2d<int> steps, IEnumerable<Vector2Int> pointsSortedByDistance)
		{
			_stepDict[point] = steps;
			_pointsSortedByDistances[point] = pointsSortedByDistance;
		}

		private bool TryGetCache(Vector2Int point, out Map2d<int> steps, out IEnumerable<Vector2Int> pointsSortedByDistance)
		{
			if (_stepDict.TryGetValue(point, out steps))
			{
				pointsSortedByDistance = _pointsSortedByDistances[point];
				return true;
			}

			pointsSortedByDistance = null;

			return false;
		}

		/// <summary>
		/// あるマスからの歩数を計算する
		/// </summary>
		public Map2d<int> GetSteps(Vector2Int to, int iteration = 4, bool useCache = true)
		{
			return GetSteps(to, out _, iteration, useCache);
		}
		
		/// <summary>
		/// あるマスからの歩数を計算する
		/// </summary>
		public Map2d<int> GetSteps(Vector2Int point, out List<Vector2Int> pointsSortedByDinstance, int iteration = 4, bool useCache = true)		// stepは都度生成せずに対象の値を持っておけば使い回せる気がする
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

			foreach (var kvp in _map)
			{
				posHashSet.Add(kvp.Key);
			}
			
			// 全マスを対象のマスとの距離でソートする
			pointsSortedByDinstance= posHashSet.OrderBy(p => Vector2.Distance(p, point)).ToList();
			var positionsCount = pointsSortedByDinstance.Count;

			steps = new Map2d<int>();

			foreach (var kvp in _map)
			{
				// 一旦、対象のマスからの距離を最大値にしておく
				steps[kvp.Key] = unreachableStep;
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

		public List<Vector2Int> GetReachablePoints(Map2d<int> steps, Vector2Int from)
		{
			var reachables = new List<Vector2Int>();
			foreach (var s in steps)
			{
				if (CanGoTo(s.Key) && steps.TryGet(s.Key, out var step) && step != unreachableStep)
				{
					reachables.Add(s.Key);
				}
			}

			return reachables;
		}

		/// <summary>
		/// マス毎の歩数を見て、未到達領域を再計算する。計算が進捗しなくなったら終わり
		/// </summary>
		public int TryFillUnreachable(Map2d<int> steps)
		{
			var unreachables = new HashSet<Vector2Int>();

			foreach (var p in steps)
			{
				if (p.Value == unreachableStep)
				{
					unreachables.Add(p.Key);
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
		public int CalcStepByNexts(Map2d<int> steps, Vector2Int target)
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
		private bool TryGetLowerestStepByNexts(Map2d<int> steps, Vector2Int target, out int lowerestStep, out Map2d.Direction direction)
		{
			direction = Map2d.Direction.None;

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

				if (steps.TryGet(current, out var step) &&  lowerestStep > step)
				{
					lowerestStep = step;
					direction = d;
				}
			}

			return lowerestStep != unreachableStep;

		}

		public override Vector2Int[] GetRoute(Vector2Int from, Vector2Int to)
		{
			return GetRoute(from, to);
		}

		// public bool TryGetRoute(Vector2Int from, Vector2Int to, out Vector2Int[] route, bool enableSlant = false, int iteration = 4)
		// {
		// 	route = GetRoute(from, to, enableSlant, iteration);
		// 	return route != null && route.Length > 0;
		// }

		/// <summary>
		/// 経路を取得する
		/// </summary>
		public Vector2Int[] GetRoute(Vector2Int from, Vector2Int to, bool enableSlant = false, int iteration = 4, bool useCache = true)
		{
			var steps = GetSteps(to, iteration, useCache);
			
			return GetRoute(steps, from, to, enableSlant);
		}

		public Vector2Int[] GetRoute(Map2d<int> steps, Vector2Int from, Vector2Int to, bool enableSlant = false)
		{
			var hasStep = steps.TryGet(from, out var step);

			if (!hasStep)
			{
				// Debug.LogError(from + "は未登録です。");
				return null;
			}

			if (step == unreachableStep)
			{
				// Debug.LogError(from + "は到達できない場所です。");
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

		// /// <summary>
		// /// 対象のマスは通行可能か
		// /// </summary>
		// public bool CanGoTo(Vector2Int p)
		// {
		// 	if (this.IsOutOfBounds(p))
		// 	{
		// 		return false;
		// 	}

		// 	var b = TryGet(p, out var v);

		// 	return b && v;
		// }

		/// <summary>
		/// 対象のマスは通行可能か
		/// </summary>
		public abstract bool CanGoTo(Vector2Int p);

		public bool CanGoTo(int step)
		{
			return step != unreachableStep;
		}

		private class Param
		{
			public Vector2Int from { get; private set; }
			public Vector2Int to { get; private set; }
			public bool enableSlant { get; private set; }
			public int iteration { get; private set; }
			public bool useCache { get; private set; }
			public System.Action<bool, Vector2Int[]> callback { get; private set; }

			public Param(Vector2Int from, Vector2Int to, System.Action<bool, Vector2Int[]> callback, bool enableSlant = false, int iteration = 4, bool useCache = true)
			{
				this.from = from;
				this.to = to;
				this.callback = callback;
				this.enableSlant = enableSlant;
				this.iteration = iteration;
				this.useCache = useCache;
			}
		}

		private Queue<Param> _queue = new Queue<Param>();
		private bool _isWorkingThread;

		private void ThreadTask()
		{
			if (_queue.Count > 0 && !_isWorkingThread)
			{
				_isWorkingThread = true;
				var p = _queue.Dequeue();

				System.Threading.Tasks.Task.Run(() =>
				{
					var success = this.TryGetRoute(p.from, p.to, out var route, p.enableSlant, p.iteration, p.useCache);
					p.callback.Invoke(success, route);
					_isWorkingThread = false;
					ThreadTask();
				});
			}
		}


		[System.Obsolete("ちゃんと動作しているか怪しい")]
		public void SyncTryGetRoute(Vector2Int from, Vector2Int to, System.Action<bool, Vector2Int[]> callback, bool enableSlant = false, int iteration = 4, bool useCache = true)
		{
			_queue.Enqueue(new Param(from, to, callback, enableSlant, iteration, useCache));
			ThreadTask();
		}

		[System.Obsolete("ちゃんと動作しているか怪しい")]
		public IEnumerator CoSyncTryGetRoute(Vector2Int from, Vector2Int to, System.Action<bool, Vector2Int[]> callback, bool enableSlant = false, int iteration = 4, bool useCache = true)
		{
			var wait = true;

			var success = false;
			Vector2Int[] route = null;

			_queue.Enqueue(new Param(from, to, (s, r) =>
			{
				wait = false;

				success = s;
				route = r;

			}, enableSlant, iteration, useCache));

			ThreadTask();

			while (wait)
			{
				yield return null;
			}

			callback(success, route);
		}
	}

	public class SimpleNavigator2d : SimpleNavigator2d<bool>
	{
		public SimpleNavigator2d(Map2d<bool> map) : base(map)
		{
			
		}

		public override bool CanGoTo(Vector2Int p)
		{
			if (this.IsOutOfBounds(p))
			{
				return false;
			}

			var b = TryGet(p, out var v);

			return b && v;
		}
	}

	public static class NavigatorExtension
	{
		public static bool IsOutOfBounds<T>(this Navigator2d<T> map, Vector2Int p)
		{
			return !map.IsInBounds(p);
		}

		public static bool TryGetRoute(this ICustomNavigator self, Vector2Int from, Vector2Int to, out Vector2Int[] route, bool enableSlant = false, int iteration = 4, bool useCache = true)
		{
			route = self.GetRoute(from, to, enableSlant, iteration, useCache);
			return route != null && route.Length > 0 && route[route.Length - 1] == to;
		}


		/// <summary>
		/// たまに帰ってこないことがあるので、呼び出し側でも待機処理入れてくれ
		/// </summary>
		public static IEnumerator CoGetRoute(this ICustomNavigator self, Vector2Int from, Vector2Int to, System.Action<bool, Vector2Int[]> callback, bool enableSlant = false, int iteration = 4, bool useCache = true)
		{
			var complete = false;
			var success = false;
			Vector2Int[] route = null;

			System.Threading.Tasks.Task.Run(() =>
			{
				success = self.TryGetRoute(from, to, out route, enableSlant, iteration, useCache);
				complete = true;
			});

			while (!complete)
			{
				yield return null;
			}

			callback?.Invoke(success, route);
		}

		public static void GetRouteAsync(this ICustomNavigator self, Vector2Int from, Vector2Int to, System.Action<bool, Vector2Int[]> callback, bool enableSlant = false, int iteration = 4, bool useCache = true)
		{
			System.Threading.Tasks.Task.Run(() =>
			{
				var success = self.TryGetRoute(from, to, out var route, enableSlant, iteration, useCache);
				callback?.Invoke(success, route);
			});
		}
	}
}