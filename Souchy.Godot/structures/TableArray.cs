using Godot;
using System.Collections;

namespace Souchy.Godot.structures;

public class TableArray<T> : IEnumerable<(int x, int y, T v)>
{
	private T[] _array;

	public T? defaultValue { get; set; } = default;
	// Width (number of columns)
	public int Width { get; }
	// Height (number of rows)
	public int Height { get; }
	public Vector2I Size => new(Width, Height);

	public TableArray(int width, int height)
	{
		Width = width;
		Height = height;
		_array = new T[Width * Height];
	}
	public TableArray(int width, int height, T? defaultValue) : this(width, height)
	{
		this.defaultValue = defaultValue;
		Fill(defaultValue);
	}
	public TableArray<T> Copy()
	{
		var copy = new TableArray<T>(Width, Height)
		{
			defaultValue = defaultValue
		};
		Array.Copy(_array, copy._array, _array.Length);
		return copy;
	}

	public int Index(int x, int y) => x + Width * y;
    public int Index(Vector2I pos) => Index(pos.X, pos.Y);
	private (int x, int y, T v) Cell(int index)
	{
		(int y, int x) = int.DivRem(index, Width);
		return (x, y, this[index]);
	}

	public T this[int index]
	{
		get => _array[index];
		set => _array[index] = value;
	}
	public T this[int x, int y]
	{
		get => _array[Index(x, y)];
		set => _array[Index(x, y)] = value;
	}
	public T this[Vector2I pos]
	{
		get => this[pos.X, pos.Y];
		set => this[pos.X, pos.Y] = value;
	}

	public bool Has(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;
	public bool Has(Vector2I pos) => Has(pos.X, pos.Y);

	public bool Replace(Vector2I pos, T target, T value)
	{
		if (Is(pos, target))
		{
			this[pos] = value;
			return true;
		}
		return false;
	}

	public bool Is(Vector2I pos, T value) => Has(pos) && Equals(this[pos], value);

	public IEnumerator<(int x, int y, T v)> GetEnumerator()
	{
		for (int i = 0; i < _array.Length; i++)
			yield return Cell(i);
	}

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	public void Fill(T? t)
	{
		Array.Fill(_array, t);
	}

	public void Clear() => Fill(defaultValue);

	public TableArray<T> GetNeighboors9(int x, int y)
	{
		return SubArray(new Vector2I(x - 1, y - 1), new Vector2I(3, 3));
    }
    public TableArray<T> GetNeighboors5(int x, int y)
    {
        var sub = new TableArray<T>(3, 3, defaultValue);
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (i == 1 || y == 1) // cross shape in the center
                {
                    var pixel = new Vector2I(x + i - 1, y + j - 1); // offset by -1,-1 to center the 9 cells around the target
                    if (Has(pixel))
                        sub[i, j] = this[pixel];
                }
            }
        }
        return sub;
    }

    public TableArray<T> SubArray(Vector2I pos, Vector2I size)
	{
		var sub = new TableArray<T>(size.X, size.Y, defaultValue);
		for (int i = 0; i < size.X; i++)
		{
			for (int j = 0; j < size.Y; j++)
			{
				var pixel = pos + new Vector2I(i, j);
				//pixel.X %= this.Width;
				//pixel.Y %= this.Height;
				if (Has(pixel))
					sub[i, j] = this[pixel];
			}
		}
		return sub;
	}

	public TableArray<R> Map<R>(Func<(int x, int y, T v), R> func)
	{
		var mapped = new TableArray<R>(Width, Height, func((-1, -1, defaultValue)));
		foreach (var p in this)
		{
			mapped[p.x, p.y] = func(p);
		}
		return mapped;
	}

}
