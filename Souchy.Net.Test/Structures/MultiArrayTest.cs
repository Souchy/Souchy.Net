using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Souchy.Net.Test.Structures;

public class MultiArrayTest
{
}


public class MultiArrayList<T>
{
    public int Size;
    public int Capacity;
    public Type Typeof = typeof(T);
    public Dictionary<Type, Array> Arrays { get; } = new();

    private Dictionary<Type, Func<T, object>> _getters = new();

    public MultiArrayList(int capacity = 100)
    {
        Capacity = capacity;
        // Initialize with some default arrays if needed
        // Arrays[typeof(T)] = Array.CreateInstance(typeof(T), 10);
        // Get all public instance properties
        Typeof.GetMembers(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m is PropertyInfo || m is FieldInfo)
            .ToList();
        //foreach (var prop in Typeof.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        //{
        //    Arrays[prop.PropertyType] = Array.CreateInstance(prop.PropertyType, capacity);
        //}

        //// Get all public instance fields
        //foreach (var field in Typeof.GetFields(BindingFlags.Public | BindingFlags.Instance))
        //{
        //    Arrays[field.FieldType] = Array.CreateInstance(field.FieldType, capacity);
        // 

        foreach(var member in Typeof.GetMembers(BindingFlags.Public | BindingFlags.Instance))
        {
            var type = GetMemberType(member);
            Expression memberExpr = null;
            //var expr = Expression.Convert(Expression.Property(param, member), typeof(object));
            if (member is FieldInfo field)
            {
                memberExpr = Expression.Field(Expression.Parameter(typeof(T), "x"), field);
            }
            else if (member is PropertyInfo prop && prop.CanRead && prop.GetIndexParameters().Length == 0)
            {
                memberExpr = Expression.Property(Expression.Parameter(typeof(T), "x"), prop);
            }

            var param = Expression.Parameter(typeof(T), "x");
            var getter = Expression.Lambda<Func<T, object>>(memberExpr, param).Compile();
            _getters[type] = getter;
        }

        // For public fields
        foreach (var field in Typeof.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            Arrays[field.FieldType] = Array.CreateInstance(field.FieldType, capacity);
            // Use expression trees for fast field access
            var param = Expression.Parameter(typeof(T), "x");
            var expr = Expression.Convert(Expression.Field(param, field), typeof(object));
            var getter = Expression.Lambda<Func<T, object>>(expr, param).Compile();
            _getters[field.FieldType] = getter;
        }

        // For public properties with getters
        foreach (var prop in Typeof.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.CanRead && prop.GetIndexParameters().Length == 0)
            {
                Arrays[prop.PropertyType] = Array.CreateInstance(prop.PropertyType, capacity);
                var param = Expression.Parameter(typeof(T), "x");
                var expr = Expression.Convert(Expression.Property(param, prop), typeof(object));
                var getter = Expression.Lambda<Func<T, object>>(expr, param).Compile();
                _getters[prop.PropertyType] = getter;
            }
        }
    }

    public Type GetMemberType(MemberInfo member)
    {
        return member switch
        {
            PropertyInfo prop => prop.PropertyType,
            FieldInfo field => field.FieldType,
            _ => throw new ArgumentException("Member must be a property or field", nameof(member))
        };
    }

    public void Add(T item)
    {
        //if (Size >= Capacity)
        //{
        //    throw new InvalidOperationException("Capacity exceeded");
        //}
        // Assuming T has properties or fields that we want to store
        foreach (var prop in Typeof.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (Arrays.TryGetValue(prop.PropertyType, out var array))
            {
                //((Array)array)[Size] = prop.GetValue(item);
            }
        }
        foreach (var field in Typeof.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            if (Arrays.TryGetValue(field.FieldType, out var array))
            {
                //((Array)array)[Size] = field.GetValue(item);
            }
        }
        Size++;
    }

    public void Set<TProp>(int index, TProp value)
    {
        if (!Arrays.TryGetValue(typeof(TProp), out var array))
        {
            array = new TProp[100];
            Arrays[typeof(TProp)] = array;
        }
        ((TProp[]) array)[index] = value;
    }

    public void Get<TProp>(int index, out TProp value)
    {
        if (!Arrays.TryGetValue(typeof(TProp), out var array))
        {
            array = new TProp[100];
            Arrays[typeof(TProp)] = array;
        }
        value = ((TProp[]) array)[index];
    }
}
