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

	public class Map2d<T> : IEnumerable<KeyValuePair<Vector2Int, T>>
	{
		private Dictionary<Vector2Int, T> _dict;

		private Vector2Int _min;
		private Vector2Int _max;

		public Map2d()
		{
			_dict = new Dictionary<Vector2Int, T>();
		}

		public Map2d(Dictionary<Vector2Int, T> dict)
		{
			_dict = dict;
			UpdateSize();
		}

		public Map2d(T[,] array) : this()
		{
			array.Foreach((v2Int, v) =>
			{
				_dict.Add(v2Int, v);
			});

			UpdateSize();
		}

		public T this[int x, int y]
		{
			get
			{
				return _dict[new Vector2Int(x, y)];
			}

			set
			{
				_dict[new Vector2Int(x, y)] = value;
			}
		}

		public T this[Vector2Int p]
		{
			get { return _dict[p]; }
			set { _dict[p] = value; }
		}

		public IEnumerator<KeyValuePair<Vector2Int, T>> GetEnumerator()
		{
			return _dict.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _dict.GetEnumerator();
		}

		protected void UpdateSize()
		{
			var minX = int.MaxValue;
			var minY = int.MaxValue;

			var maxX = int.MinValue;
			var maxY = int.MinValue;

			foreach (var kvp in _dict)
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
					maxX = pos.x;
				}

				if (pos.y > maxY)
				{
					maxY = pos.y;
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
				maxX = pos.x;
			}

			if (pos.y > maxY)
			{
				maxY = pos.y;
			}

			_min = new Vector2Int(minX, minY);
			_max = new Vector2Int(maxX, maxY);
		}

		public int GetWidth()
		{
			return _max.x - _min.x + 1;
		}

		public int GetHeight()
		{
			return _max.y - _min.y + 1;
		}

		public Vector2Int GetSize()
		{
			return new Vector2Int(GetWidth(), GetHeight());
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