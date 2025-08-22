
using Godot;
using Souchy.Godot.structures;

namespace Souchy.Godot.structures;

public static class TableExtensions
{
	// Directions for 4-connected grid (up, down, left, right)
	static readonly int[] dx = { 0, 0, 1, -1 };
	static readonly int[] dy = { 1, -1, 0, 0 };

	/// <summary>
	/// FIXME: not great integer scaling + why out of bounds exception?
	/// </summary>
	public static TableArray<T> Scale<T>(this TableArray<T> old, Vector2 newSize)
	{
		var scale = new Vector2((newSize.X / old.Width), (newSize.Y / old.Height));
		var grid = new TableArray<T>((int)newSize.X, (int)newSize.Y, old.defaultValue);

		for (int x = 0; x < old.Width; x++)
			for (int y = 0; y < old.Height; y++)
			{
				for (int i = 0; i < scale.X; i++)
					for (int j = 0; j < scale.Y; j++)
					{
						var val = old[x, y];
						var pos = new Vector2I((int)(x * scale.X) + i, (int)(y * scale.Y) + j);
						if (grid.Has(pos))
							grid[pos] = val;
						//else
						//{
						//	GD.Print("weird af out of bound terrain scaling");
						//}
						//try
						//{
						//    grid[(int) (x * scale.X) + i, (int) (y * scale.Y) + j] = old[x, y];
						//}
						//catch (Exception e)
						//{
						//    GD.PrintErr("Error: " + e.Message + " : " + e.StackTrace);
						//}
					}

			}
		return grid;
	}

	public static Rect2I MeasureBounds<T>(this TableArray<T> grid, T target)
	{
		int minX = grid.Width;
		int maxX = 0;
		int minY = grid.Height;
		int maxY = 0;
		for (int x = 0; x < grid.Width; x++)
			for (int y = 0; y < grid.Height; y++)
			{
				if (x < minX && grid[x, y].Equals(target))
					minX = x;
				if (y < minY && grid[x, y].Equals(target))
					minY = y;
				if (x > maxX && grid[x, y].Equals(target))
					maxX = x;
				if (y > maxY && grid[x, y].Equals(target))
					maxY = y;
			}
		return new(minX, minY, maxX - minX, maxY - minY);
	}
	public static Vector2I FirstWithin<T>(this TableArray<T> grid, Rect2I subSpace, T target)
	{
		for (int x = subSpace.Position.X; x < subSpace.End.X; x++)
			for (int y = subSpace.Position.Y; y < subSpace.End.Y; y++)
				if (Object.Equals(target, grid[x, y]))
					return new(x, y);
		return new(-1, -1);
	}
	public static TableArray<T> FloodCopyAndReplace<T>(this TableArray<T> grid, Vector2I pos, T target, T fill)
	{
		TableArray<T> temp = grid.Copy();
		temp.FloodFill(pos, target, fill);
		return temp;
	}

	public static TableArray<T> Replace<T>(this TableArray<T> table, T oldValue, T newValue)
	{
		foreach (var (x, y, v) in table)
			if (Object.Equals(oldValue, v))
				table[x, y] = newValue;
		return table;
	}

	public static TableArray<T> FloodCut<T>(this TableArray<T> grid, Vector2I start, T targetValue, T replace)
	{
		TableArray<T> subGrid = new(grid.Width, grid.Height, grid.defaultValue);

		Stack<(int x, int y)> stack = new Stack<(int x, int y)>();
		stack.Push((start.X, start.Y));

		while (stack.Count > 0)
		{
			var (x, y) = stack.Pop();

			// If out of bounds or already filled with the new value, skip it
			if (x < 0 || x >= grid.Width || y < 0 || y >= grid.Height || !Object.Equals(targetValue, grid[x, y]))
				continue;

			// Fill the current point
			subGrid[x, y] = grid[x, y];
			grid[x, y] = replace;

			// Push all adjacent cells (up, down, left, right) to the stack
			for (int i = 0; i < 4; i++)
			{
				int nx = x + dx[i];
				int ny = y + dy[i];

				if (nx >= 0 && nx < grid.Width && ny >= 0 && ny < grid.Height && Object.Equals(targetValue, grid[nx, ny]))
				{
					stack.Push((nx, ny));
				}
			}
		}

		return subGrid;
	}

	// Flood-fill using DFS (stack-based)
	public static void FloodFill<T>(this TableArray<T> map, Vector2I start, T targetValue, T fillValue)
	{
		int width = map.Width;
		int height = map.Height;

		// If the start point is already filled, no need to fill
		if (Object.Equals(fillValue, map[start.X, start.Y]))
			return;

		Stack<(int x, int y)> stack = new Stack<(int x, int y)>();
		stack.Push((start.X, start.Y));

		while (stack.Count > 0)
		{
			var (x, y) = stack.Pop();

			// If out of bounds or already filled with the new value, skip it
			if (x < 0 || x >= width || y < 0 || y >= height || !Object.Equals(targetValue, map[x, y]))
				continue;

			// Fill the current point
			map[x, y] = fillValue;

			// Push all adjacent cells (up, down, left, right) to the stack
			for (int i = 0; i < 4; i++)
			{
				int nx = x + dx[i];
				int ny = y + dy[i];

				if (nx >= 0 && nx < width && ny >= 0 && ny < height && Object.Equals(targetValue, map[nx, ny]))
				{
					stack.Push((nx, ny));
				}
			}
		}
	}
}
