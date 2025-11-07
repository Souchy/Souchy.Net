using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;

namespace Souchy.Net.communication;

/// <summary>
/// Subscribe attribute
/// Can be used on methods. 
/// 
/// The attribute can target a string path (ex: nameof(StatType.Life), "my:scope:path", nameof(CreatureModel.nameId))
/// The path is only used to pipeline events, it can be anything, doesn't mean anything.
/// The method can have parameters to serve as event objects. 
/// The parameters must match the same as the parametrs in publish()
/// 
/// Scoped means only PublishScoped will invoke it.
/// 
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class SubscribeAttribute : Attribute
{
    public bool Scoped { get; }
    public string[] Paths { get; } = { "" };
    public SubscribeAttribute() { }
    public SubscribeAttribute(params object[] paths)
    {
        if (paths != null && paths.Length > 0)
            this.Paths = paths.Select(p => p.ToString()).ToArray();
    }
    public SubscribeAttribute(bool scoped, params object[] paths) : this(paths)
    {
        Scoped = scoped;
    }
}

public delegate void MethodLambda(object? target, object?[] args);

public record Subscription
{
    public MethodInfo MethodInfo { get; init; }
    public string Path { get; init; }
    /// <summary>
    /// Useful to ignore global events
    /// </summary>
    public bool Scoped { get; init; }

    // Weak reference to the original Delegate (so the bus doesn't keep closures/targets alive)
    private WeakReference<object?>? HandlerRef { get; }
    private MethodLambda ActionLambda { get; init; }
    private Type[] ParameterTypes { get; init; }

    public Subscription(object? target, MethodInfo method, string path, bool scoped)
    {
        HandlerRef = target == null ? null : new WeakReference<object?>(target);
        MethodInfo = method;
        ActionLambda = CreateLambda(method);
        ParameterTypes = method.GetParameters().Select(p => p.ParameterType).ToArray();
        Path = path;
        Scoped = scoped;
    }

    public void Invoke(object?[] args)
    {
        if (TryGetTarget(out var target))
            ActionLambda(target, args);
    }

    public bool TryGetTarget(out object? target)
    {
        // null for static method
        if (HandlerRef == null)
        {
            target = null;
            return true;
        }
        return HandlerRef.TryGetTarget(out target);
    }

    public bool IsAlive => HandlerRef == null || HandlerRef.TryGetTarget(out _);

    public bool Matches(string path, object? scope, params object?[] args)
    {
        if (path != Path)
            return false;

        // if either the sub or the pub is scoped, check that they're the same
        if (Scoped || scope != null)
        {
            if (!this.TryGetTarget(out var target))
                return false;

            if (target != scope)
                return false;
        }

        if (this.ParameterTypes.Length != args.Length)
            return false;

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg == null)
                continue;
            if (!ParameterTypes[i].IsAssignableFrom(arg.GetType()))
                return false;
        }
        return true;
    }

    private MethodLambda CreateLambda(MethodInfo method)
    {
        // Create parameters for the lambda expression
        var targetParam = Expression.Parameter(typeof(object), "target");
        var argsParam = Expression.Parameter(typeof(object[]), "args");

        var paramInfos = method.GetParameters();
        var callArgs = new Expression[paramInfos.Length];

        // Add arguments to the args param
        for (int i = 0; i < paramInfos.Length; i++)
        {
            var index = Expression.Constant(i);
            var arrayAccess = Expression.ArrayIndex(argsParam, index);
            // cast/unbox to the parameter type
            callArgs[i] = Expression.Convert(arrayAccess, paramInfos[i].ParameterType);
        }

        Expression instance = method.IsStatic ? null : Expression.Convert(targetParam, method.DeclaringType!);
        var call = Expression.Call(instance, method, callArgs);

        Expression body = call;

        var lambda = Expression.Lambda<MethodLambda>(body, targetParam, argsParam);
        return lambda.Compile();
    }
}

public class EventBus
{
    public ConcurrentDictionary<string, ImmutableList<Subscription>> Subscriptions { get; } = [];

    public IEnumerable<Subscription> AllSubscriptions => Subscriptions.SelectMany(kv => kv.Value);
    public int SubscriptionCount => Subscriptions.Values.Sum(list => list.Count);

    #region Subscribing
    public void Subscribe(params Delegate[] delegates)
    {
        Subscribe(null, delegates);
    }

    public void Subscribe(object? subscriber, params Delegate[] delegates)
    {
        var actors = delegates.Select(a => (a.Target, a.Method));

        if (delegates.Length == 0)
        {
            if (subscriber == null)
            {
                throw new InvalidOperationException("Can't have null subscriber and no delegates");
            }
            var stype = subscriber.GetType();
            var types = stype.GetInterfaces().ToList();
            types.Add(stype);
            actors = types.SelectMany(t => t
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Where(f => f.GetCustomAttributes<SubscribeAttribute>(true).Any()))
                .Distinct()
                .Select(m => ((object?) subscriber, m));
        }

        var subs = actors.SelectMany(del =>
        {
            var (target, method) = del;
            var paths = ActionGetPaths(target, method);
            var attr = method.GetCustomAttribute<SubscribeAttribute>(true);
            bool scoped = attr?.Scoped ?? false;
            return paths.Select(p => new Subscription(target, method, p, scoped));
        });

        foreach (var sub in subs)
        {
            Subscriptions.AddOrUpdate(sub.Path,
                [sub],
                (k, oldList) =>
                {
                    if (!sub.TryGetTarget(out var h1))
                        return oldList;
                    // Check for duplicate subscription
                    if (oldList.Any(old => old.TryGetTarget(out var h2) && h1 == h2 && sub.MethodInfo == old.MethodInfo))
                        return oldList;
                    return oldList.Add(sub);
                });
        }
    }

    private IEnumerable<string> ActionGetPaths(object? target, MethodInfo method)
    {
        var @params = method.GetParameters();
        var attrType = typeof(SubscribeAttribute);
        var attr = method.GetCustomAttribute<SubscribeAttribute>(true);
        if (attr != null)
        {
            return attr.Paths.Select(path =>
            {
                return attr.Scoped ? path + target?.GetHashCode() : path;
            });
        }
        else
        {
            return [""]; //string.Join(",", @params.Select(p => p.ParameterType.Name))];
        }
    }

    public void Unsubscribe(params Delegate[] actions) => Unsubscribe(null, actions);

    /// <summary>
    /// Remove all subscriptions that match the actions
    /// </summary>
    public void Unsubscribe(object? subscriber, params Delegate[] actions)
    {
        foreach (var action in actions)
        {
            var paths = ActionGetPaths(subscriber, action.Method);
            foreach (var path in paths)
            {
                if (!Subscriptions.TryGetValue(path, out var list))
                    continue;
                var dead = new List<Subscription>();
                foreach (var sub in list)
                {
                    if (!sub.IsAlive)
                    {
                        dead.Add(sub);
                        continue;
                    }
                    if (sub.TryGetTarget(out var handler))
                    {
                        if (handler == subscriber && sub.MethodInfo == action.Method)
                        {
                            dead.Add(sub);
                        }
                    }
                }
                RemoveDeadSubscribers(path, dead);
            }
        }
    }

    /// <summary>
    /// Remove all subscriptions that have the subscriber as target.
    /// </summary>
    public void Unsubscribe(object subscriber)
    {
        foreach (var key in Subscriptions.Keys)
        {
            if (!Subscriptions.TryGetValue(key, out var list))
                continue;
            var dead = new List<Subscription>();
            foreach (var sub in list)
            {
                if (!sub.IsAlive)
                {
                    dead.Add(sub);
                    continue;
                }
                if (sub.TryGetTarget(out var handler))
                {
                    if (handler == subscriber)
                    {
                        dead.Add(sub);
                    }
                }
            }
            RemoveDeadSubscribers(key, dead);
        }
    }

    #endregion

    #region Publish (synchronous)
    public void Publish(params object[] args)
    {
        Publish(null, null, args);
    }
    public void Publish(string path, params object[] args)
    {
        Publish(path, null, args);
    }
    public void PublishScoped(object scope, params object[] args)
    {
        Publish(null, scope, args);
    }
    public void Publish(string? path, object? scope, params object[] args)
    {
        path ??= ""; //string.Join(",", args.Select(p => p?.GetType().Name));
        path += scope?.GetHashCode();
        ProcessSubscribers(path, scope, args, sub => sub.Invoke(args));
    }
    #endregion

    #region Publish (asynchronous)
    public async Task PublishAsync(params object[] args)
    {
        await PublishAsync(null, null, args).ConfigureAwait(false);
    }
    public async Task PublishAsync(string path, params object?[] args)
    {
        await PublishAsync(path, null, args).ConfigureAwait(false);
    }
    public async Task PublishAsyncScoped(object scope, params object?[] args)
    {
        await PublishAsync(null, scope, args).ConfigureAwait(false);
    }
    public async Task PublishAsync(string? path, object? scope, params object?[] args)
    {
        path ??= ""; //string.Join(",", args.Select(p => p?.GetType().Name)); // doesnt work if we want to publish null args
        path += scope?.GetHashCode();
        var tasks = new List<Task>();
        ProcessSubscribers(path, scope, args, sub =>
        {
            try
            {
                tasks.Add(Task.Run(() => sub.Invoke(args)));
            }
            catch (Exception ex)
            {
                // scheduling failure: add a faulted task
                tasks.Add(Task.FromException(ex));
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
    #endregion

    #region Private methods
    private void ProcessSubscribers(string path, object? scope, object?[] args, Action<Subscription> actionWrapper)
    {
        if (!Subscriptions.TryGetValue(path, out var snapshot)) return;
        var dead = new List<Subscription>();
        foreach (var sub in snapshot)
        {
            if (!sub.IsAlive)
            {
                dead.Add(sub);
                continue;
            }
            if (!sub.Matches(path, scope, args))
                continue;
            actionWrapper(sub);
        }
        RemoveDeadSubscribers(path, dead);
    }
    private void RemoveDeadSubscribers(string path, List<Subscription> deadSubscribers)
    {
        if (deadSubscribers.Count == 0)
            return;
        // Try and retry to update the list atomically. If the update fails (race condition), we retry.
        while (Subscriptions.TryGetValue(path, out var oldList) && oldList.Count > 0)
        {
            var newList = oldList.RemoveAll(s => deadSubscribers.Contains(s));
            if (ReferenceEquals(newList, oldList) || newList.Count == oldList.Count) break;
            if (Subscriptions.TryUpdate(path, newList, oldList))
            {
                if (newList.IsEmpty) Subscriptions.TryRemove(path, out _);
                break;
            }
        }
    }
    #endregion

}
