using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Souchy.Net.comm;

// Some are Requests, and so they need a response. (ex: validate input, get user data)
// Some are Events, and so they don't need a response. (ex: update ui)

public interface IRequest;
public interface IRequest<TResponse>;

/// <summary>
/// Each Request has a Response.
/// Which means each Request is handled by exactly one handler.
/// 
/// Ex: 
/// - UI layer sends a Request to Data layer to validate user input, Data layer responds with valid/invalid.
/// - UI layer sends a Request to Data layer to get user data, Data layer responds with user data.
///     - So the Response type is UserData.
///     - Data layer has a handler for GetUserDataRequest.
/// </summary>
public class AsyncRequestBus
{
    public record struct IgnoreResult(int i)
    {
        public static readonly IgnoreResult Success = new(0);
    }

    public bool ContinueOnCapturedContext { get; set; } = false;

    /// <summary>
    /// <TRequest, Func<TRequest, Token, Task<TResponse>>
    /// </summary>
    private readonly ConcurrentDictionary<Type, Func<object, CancellationToken, Task<object?>>> _handlers = new();

    #region Public methods
    public bool SubscribeAsync<TReq>(Func<TReq, CancellationToken, Task> handler) where TReq : IRequest
    {
        ArgumentNullException.ThrowIfNull(handler);
        var route = typeof(TReq);
        async Task<object?> wrapper(object o, CancellationToken token)
        {
            await handler((TReq) o, token).ConfigureAwait(ContinueOnCapturedContext);
            return IgnoreResult.Success;
        }
        return _handlers.TryAdd(route, wrapper);
    }

    public bool SubscribeAsync<TReq, TResp>(Func<TReq, CancellationToken, Task<TResp>> handler) where TReq : IRequest<TResp>
    {
        ArgumentNullException.ThrowIfNull(handler);
        var route = typeof(TReq);
        async Task<object?> wrapper(object o, CancellationToken token) => await handler((TReq) o, token).ConfigureAwait(ContinueOnCapturedContext);
        return _handlers.TryAdd(route, wrapper);
    }

    public bool UnSubscribeAsync<T>(Func<T, CancellationToken, Task> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        var route = typeof(T);
        return _handlers.TryRemove(route, out _);
    }

    public async Task<TResponse?> RequestAsync<TResponse>(IRequest<TResponse> request, bool acceptNull = false, CancellationToken cancellationToken = default)
    {
        var task = InvokeHandlerAsync(request, cancellationToken);
        return await ValidateResult<TResponse?>(request, task, acceptNull).ConfigureAwait(ContinueOnCapturedContext);
    }

    public async Task RequestAsync(IRequest request, CancellationToken cancellationToken = default)
    {
        await InvokeHandlerAsync(request, cancellationToken).ConfigureAwait(ContinueOnCapturedContext);
    }

    public async Task<TResponse?> RequestAsync<TResponse>(IRequest<TResponse> request, int timeout, bool acceptNull = false, CancellationToken cancellationToken = default)
    {
        if (timeout <= 0) throw new ArgumentOutOfRangeException(nameof(timeout), "timeout must be greater than zero.");
        var task = AwaitHandlerWithTimeoutAsync<TResponse?>(request, timeout, cancellationToken);
        return await ValidateResult<TResponse?>(request, task, acceptNull).ConfigureAwait(ContinueOnCapturedContext);
    }

    public async Task RequestAsync(IRequest request, int timeout, CancellationToken cancellationToken = default)
    {
        if (timeout <= 0) throw new ArgumentOutOfRangeException(nameof(timeout), "timeout must be greater than zero.");
        await AwaitHandlerWithTimeoutAsync<IgnoreResult>(request, timeout, cancellationToken).ConfigureAwait(ContinueOnCapturedContext);
    }
    #endregion


    #region Private methods
    // Core helper that contains the common lookup/ invocation logic.
    // It returns a Task<object> representing the handler's lifetime.
    private Task<object?> InvokeHandlerAsync(object request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var requestType = request.GetType();
        if (!_handlers.TryGetValue(requestType, out var func))
            throw new InvalidOperationException($"No handler registered for request type {requestType.Name}");
        try
        {
            // func signature already returns Task<object>
            var task = func(request, cancellationToken);
            return task!;
        }
        catch (Exception ex)
        {
            // preserve the information that the handler threw synchronously
            throw new InvalidOperationException("Handler threw synchronously while accepting the request", ex);
        }
    }
    // Generic timeout helper that races the handler Task<object> against a delay.
    // TResponse is the expected response type (use object? for void/ignored result).
    private async Task<object?> AwaitHandlerWithTimeoutAsync<TResponse>(object request, int timeout, CancellationToken cancellationToken)
    {
        // Start the handler and delay tasks
        var handlerTask = InvokeHandlerAsync(request, cancellationToken);
        var delayTask = Task.Delay(timeout, cancellationToken);

        // Race handler vs delay. Use the configured capture policy for the await of WhenAny so callers can opt to capture context.
        var completed = await Task.WhenAny(handlerTask, delayTask).ConfigureAwait(ContinueOnCapturedContext);

        if (completed == handlerTask)
        {
            return await handlerTask.ConfigureAwait(ContinueOnCapturedContext);
        }

        // Delay completed -> either cancellation or timeout
        if (cancellationToken.IsCancellationRequested)
            throw new OperationCanceledException("Request was canceled.", cancellationToken);

        // Timeout: ensure we observe any later exception from handlerTask to avoid UnobservedTaskException
        _ = handlerTask.ContinueWith(t => { var _ = t.Exception; },
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);

        throw new TimeoutException($"Request of type {request.GetType().Name} timed out after {timeout}ms");
    }
    private async Task<TResponse?> ValidateResult<TResponse>(object request, Task<object?> handlerTask, bool acceptNull = false)
    {
        // Handler finished first: await to propagate exceptions/cancellation and get result
        var resultObj = await handlerTask.ConfigureAwait(ContinueOnCapturedContext);
        // For void-style calls we may return default(T)
        if (resultObj is TResponse typed) return typed;
        // If result is null
        if (resultObj == null && acceptNull)
            return default;
        // Otherwise invalid cast
        throw new InvalidOperationException($"Handler for request type {request.GetType().Name} " +
            $"returned invalid response type {resultObj?.GetType().Name ?? "null"}, expected {typeof(TResponse).Name}");
    }
    #endregion

}

