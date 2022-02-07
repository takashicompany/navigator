namespace takashicompany.Unity.Navigator.Dev
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using System.Linq;
	using takashicompany.Unity;
	
	public class Map2dSample : MonoBehaviour
	{
		[SerializeField, TextArea]
		private string _mapStr;

		[SerializeField]
		private bool _slant;

		private SimpleNavigator2d _navigator;

		private Vector2Int? _from;

		private Vector2Int? _to;

		private int _clickCount;

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

			var map = new Map2d<bool>(maps);
			_navigator = new SimpleNavigator2d(map);
		}

		private void OnGUI()
		{
			var size = _navigator.GetSize();
			var grid = new IMGrid(size.x, size.y);

			switch (_clickCount % 3)
			{
				case 0:	// to
					{
						grid.Foreach((x, y) =>
						{
							grid.Button(x, y, _navigator.Get(new Vector2Int(x, y)) ? "◯" : "x", () =>
							{
								_to = new Vector2Int(x, y);
								_clickCount++;
							});
						});
					}
					break;
				case 1:	// from
					{
						var steps = _navigator.GetSteps(_to.Value);
						grid.Foreach((x, y) =>
						{
							var s = steps[x, y];
							var str = s.ToString();
							if (s == int.MaxValue) str = "-";
							grid.Button(x, y, str, () =>
							{
								_from = new Vector2Int(x, y);
								_clickCount++;
							});
						});
					}

					break;
				case 2: // reset
					{
						var route = _navigator.GetRoute(_from.Value, _to.Value, _slant);

						grid.Foreach((x, y) =>
						{
							if (route.Contains(new Vector2Int(x, y)))
							{
								grid.Button(x, y, "", () =>
								{
									_from = null;
									_to = null;
									_clickCount++;
								});
							}
						});
					}
					break;
			}

			// if (_to.HasValue)
			// {
			// 	var steps = _map.GetSteps(_to.Value);
			// 	grid.Foreach((x, y) =>
			// 	{
			// 		var s = steps[x, y];
			// 		var str = s.ToString();
			// 		if (s == int.MaxValue) str = "-";
			// 		grid.Button(x, y, str, () =>
			// 		{
			// 			_to = new Vector2Int(x, y);
			// 		});
			// 	});
			// }
			// else
			// {
			// 	grid.Foreach((x, y) =>
			// 	{
			// 		grid.Button(x, y, _map.Get(x, y) ? "◯" : "x", () =>
			// 		{
			// 			_to = new Vector2Int(x, y);
			// 		});
			// 	});
			// }
				
		}

	}
}