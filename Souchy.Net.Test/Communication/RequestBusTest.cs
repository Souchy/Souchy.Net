using Souchy.Net.comm;
using Souchy.Net.Test.Structures;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Souchy.Net.Test.Communication;

public class RequestBusTest
{

    [Fact]
    public async Task RequestBus_Works()
    {
        AsyncRequestBus bus = new();
        bus.SubscribeAsync<MyRequest, string>(OnRequest);
        var result = await bus.RequestAsync(new MyRequest("Hello"));
        Assert.Equal("World", result);
    }
    public record MyRequest(string Message) : IRequest<string>;
    private async Task<string> OnRequest(MyRequest req, CancellationToken token)
    {
        await Task.Delay(10, token); // simulate work
        return "World";
    }

    [Fact]
    public void RequestBus_Works_Sync()
    {
        AsyncRequestBus bus = new();
        bus.SubscribeAsync<MyRequest, string>(OnRequest);
        var result = bus.RequestAsync(new MyRequest("Hello")).Result;
        Assert.Equal("World", result);
    }

    [Fact]
    public async Task RequestBus_Nullable()
    {
        AsyncRequestBus bus = new();
        bus.SubscribeAsync<MyRequestNullable, string?>(OnRequestNullResponse);
        var result = await bus.RequestAsync(new MyRequestNullable("Hello"), true);
        Assert.Null(result);
    }
    [Fact]
    public async Task RequestBus_NullableException()
    {
        AsyncRequestBus bus = new();
        bus.SubscribeAsync<MyRequestNullable, string?>(OnRequestNullResponse);
        await Assert.ThrowsAnyAsync<InvalidOperationException>(async () =>
        {
            var res = await bus.RequestAsync(new MyRequestNullable("Hello"), false);
        });
    }
    public record MyRequestNullable(string Message) : IRequest<string?>;
    private async Task<string?> OnRequestNullResponse(MyRequestNullable req, CancellationToken token)
    {
        await Task.Delay(10, token); // simulate work
        return null;
    }

    
    [Fact]
    public async Task RequestBus_Timesout_Pass()
    {
        AsyncRequestBus bus = new();
        bus.SubscribeAsync<MyRequest, string>(OnRequest);
        var result = await bus.RequestAsync(new MyRequest("Hello"), 100);
        Assert.Equal("World", result);
    }
    [Fact]
    public async Task RequestBus_Timesout()
    {
        AsyncRequestBus bus = new();
        bus.SubscribeAsync<MyRequest, string>(OnRequest);
        await Assert.ThrowsAnyAsync<TimeoutException>(async () =>
        {
            var result = await bus.RequestAsync(new MyRequest("Hello"), 1);
        });
    }
}
