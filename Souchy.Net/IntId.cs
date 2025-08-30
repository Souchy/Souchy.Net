using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Souchy.Net;

public class IntId
{
    private int _nextId = 0;
    private readonly Stack<int> _freeIds = [];

    public int GetNextId()
    {
        if (_freeIds.Count > 0)
            return _freeIds.Pop();
        if (_nextId == int.MaxValue)
            throw new InvalidOperationException("ID space exhausted");
        return _nextId++;
    }

    public void ReleaseId(int id)
    {
        _freeIds.Push(id);
    }
}

public class IntIdSync
{
    private int _nextId = 0;
    private readonly Stack<int> _freeIds = [];
    private readonly object _lock = new();

    public int GetNextId()
    {
        lock (_lock)
        {
            if (_freeIds.Count > 0)
                return _freeIds.Pop();
            if (_nextId == int.MaxValue)
                throw new InvalidOperationException("ID space exhausted");
            return _nextId++;
        }
    }
    public void ReleaseId(int id)
    {
        lock (_lock)
        {
            _freeIds.Push(id);
        }
    }
}
