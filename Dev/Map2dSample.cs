namespace TakashiCompany.Unity.Navigator.Dev
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using System.Linq;
	using TakashiCompany.Unity;
	
	public class Map2dSample : MonoBehaviour
	{
		[SerializeField, TextArea]
		private string _mapStr;

		private SimpleMap2d _map;

		private Vector2Int? _to;

		private void Awake()
		{
			var splited = _mapStr.Split(new string[] { "\n" }, System.StringSplitOptions.None);

			var width = splited.Max(s => s.Length);
			var height = splited.Length;

			var maps = new bool[width, height];

			var empty = '-';

			for (var y = 0; y < height; y++)
			{
				var line = splited[y];
				for (var x = 0; x < width && x < line.Length; x++)
				{
					maps[x, y] = line[x] == empty;
				}
			}

			_map = new SimpleMap2d(maps);
			
		}

		private void OnGUI()
		{
			var grid = new IMGrid(_map.GetWidth(), _map.GetHeight());

			if (_to.HasValue)
			{
				var steps = _map.GetSteps(_to.Value);
				grid.Foreach((x, y) =>
				{
					var s = steps[x, y];
					var str = s.ToString();
					if (s == int.MaxValue) str = "-";
					grid.Button(x, y, str, () =>
					{
						_to = new Vector2Int(x, y);
					});
				});
			}
			else
			{
				grid.Foreach((x, y) =>
				{
					grid.Button(x, y, _map.Get(x, y) ? "◯" : "x", () =>
					{
						_to = new Vector2Int(x, y);
					});
				});
			}
				
		}

	}
}