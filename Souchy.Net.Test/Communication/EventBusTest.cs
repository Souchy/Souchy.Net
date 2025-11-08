using Souchy.Net.comm;
using Souchy.Net.communication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Souchy.Net.Test.Communication.RequestBusTest;

namespace Souchy.Net.Test.Communication;

public class EventBusTest
{
    public class IncrementEvent(int i)
    {
        public int i = i;
    }

    [Fact]
    public void Check_Loop()
    {
        EventBus bus = new();
        bus.Subscribe(this, OnRequest);
        Assert.Single(bus.AllSubscriptions);

        int count = 1_000_000;
        var ev = new IncrementEvent(0);
        for(int i = 0; i < count; i++)
        {
            bus.Publish(ev);
        }

        Assert.Equal(count, ev.i);
    }
    [Fact]
    public async Task Check_Loop_ASync()
    {
        EventBus bus = new();
        bus.Subscribe(this, OnRequest);
        Assert.Single(bus.AllSubscriptions);

        int count = 1_000_000;
        var ev = new IncrementEvent(0);
        for (int i = 0; i < count; i++)
        {
            await bus.PublishAsync(ev);
        }

        Assert.Equal(count, ev.i);
    }

    [Fact]
    public void Check_Synchronous_Duplicate()
    {
        EventBus bus = new();
        bus.Subscribe(this, OnRequest);
        bus.Subscribe(this, OnRequest);
        Assert.Single(bus.AllSubscriptions);

        var ev = new IncrementEvent(0);
        bus.Publish(ev);
        bus.Publish(ev);

        Assert.Equal(2, ev.i);
    }

    [Fact]
    public void Check_Unsubscribes_1()
    {
        EventBus bus = new();
        bus.Subscribe(this, OnRequest);
        bus.Unsubscribe(this, OnRequest);
        Assert.Empty(bus.Subscriptions);

        var ev = new IncrementEvent(0);
        bus.Publish(ev);

        Assert.Equal(0, ev.i);
    }

    [Fact]
    public void Check_SubAll_Unsubscribe_1()
    {
        EventBus bus = new();
        bus.Subscribe(this); // subs all methods with attributes (2)
        bus.Unsubscribe(this, OnRequest2); // unsub only OnRequest2
        Assert.Equal(1, bus.SubscriptionCount);

        var ev = new IncrementEvent(0);
        bus.Publish(ev);

        Assert.Equal(0, ev.i);
    }

    [Fact]
    public void Check_SubAll_Unsubscribe_Different()
    {
        EventBus bus = new();
        bus.Subscribe(this, OnRequest);
        bus.Unsubscribe(this, OnRequest2);
        Assert.Equal(1, bus.SubscriptionCount);

        var ev = new IncrementEvent(0);
        bus.Publish(ev);

        Assert.Equal(1, ev.i);
    }

    [Fact]
    public void Check_Unsubscribes_All()
    {
        EventBus bus = new();
        bus.Subscribe(this, OnRequest);
        bus.Unsubscribe(this);
        Assert.Empty(bus.Subscriptions);

        var ev = new IncrementEvent(0);
        bus.Publish(ev);

        Assert.Equal(0, ev.i);
    }

    [Fact]
    public async Task Check_SubUnsub_All()
    {
        EventBus bus = new();
        bus.Subscribe(this);
        bus.Unsubscribe(this);
        Assert.Empty(bus.Subscriptions);

        var ev = new IncrementEvent(0);
        await bus.PublishAsync(ev);

        Assert.Equal(0, ev.i);
    }

    [Fact]
    public async Task Check_Asynchronous()
    {
        EventBus bus = new();
        bus.Subscribe(this, OnRequest);
        bus.Subscribe(this, OnRequest2);
        Assert.Equal(2, bus.SubscriptionCount);

        var ev = new IncrementEvent(0);
        await bus.PublishAsync(ev);
        await bus.PublishAsync(ev);

        Assert.Equal(4, ev.i);
    }

    [Fact]
    public async Task Check_Subscribe_All_OnlyTagged()
    {
        EventBus bus = new();
        bus.Subscribe(this);
        bus.Subscribe(this);
        // 2 methods tagged with the attribute [Subscribe]
        Assert.Equal(2, bus.SubscriptionCount);

        var ev = new IncrementEvent(0);
        await bus.PublishAsync(ev);

        Assert.Equal(1, ev.i);
    }


    [Fact]
    public void Check_Scoped()
    {
        EventBus bus = new();
        bus.Subscribe(this);
        // Sub 2, 1 scoped, 1 unscoped
        Assert.Equal(2, bus.SubscriptionCount);

        var ev = new IncrementEvent(0);
        bus.PublishScoped(scope: this, ev);

        // Only the scoped one should have been called
        Assert.Equal(3, ev.i);
    }

    [Fact]
    public void Check_Unscoped()
    {
        EventBus bus = new();
        bus.Subscribe(this);
        // Sub 2, 1 scoped, 1 unscoped
        Assert.Equal(2, bus.SubscriptionCount);

        var ev = new IncrementEvent(0);
        bus.Publish(ev);

        // Only the unscoped one should have been called
        Assert.Equal(1, ev.i);
    }

    [Fact]
    public async Task Check_Scoped_Async()
    {
        EventBus bus = new();
        bus.Subscribe(this);
        // Sub 2
        Assert.Equal(2, bus.SubscriptionCount);

        var ev = new IncrementEvent(0);
        await bus.PublishAsyncScoped(scope: this, ev);

        // Only the scoped one should have been called
        Assert.Equal(3, ev.i);
    }
    [Fact]
    public async Task Check_Unscoped_Async()
    {
        EventBus bus = new();
        bus.Subscribe(this);
        // Sub 2
        Assert.Equal(2, bus.SubscriptionCount);

        var ev = new IncrementEvent(0);
        await bus.PublishAsync(ev);

        // Only the scoped one should have been called
        Assert.Equal(1, ev.i);
    }


    [Fact]
    public void Check_Subcribe_With_No_Subscriber()
    {
        EventBus bus = new();
        bus.Subscribe(OnRequest, OnRequest2, OnRequestScoped);
        // Sub 3
        Assert.Equal(3, bus.SubscriptionCount);

        // Check scope, there's only 1 scoped subscriber so i = 3
        var ev = new IncrementEvent(0);
        bus.PublishScoped(this, ev);
        Assert.Equal(3, ev.i);

        // Check unscope, there's 2 scoped subscriber so i = 2
        ev = new IncrementEvent(0);
        bus.Publish(ev);
        Assert.Equal(2, ev.i);
    }

    public int a = 0;
    public string asdf = "";
    [Fact]
    public void Check_DoesntLoseScope()
    {
        EventBus bus = new();
        a++;
        bus.Subscribe(this);
        // Sub 2
        Assert.Equal(2, bus.SubscriptionCount);
        a = 987987;
        asdf = "lkmasdlkmndsalnmfdlkamndslkfmalsdkmflkasdm";

        var ev = new IncrementEvent(0);
        bus.PublishScoped(this, ev);

        // The object changed, both the hashcode remains the same, so the scope is valid
        Assert.Equal(3, ev.i);
    }

    //[Subscribe]
    private void OnRequest(IncrementEvent req)
    {
        req.i += 1;
    }

    [Subscribe]
    private void OnRequest2(IncrementEvent req)
    {
        req.i += 1;
    }

    [Subscribe(true)]
    private void OnRequestScoped(IncrementEvent req)
    {
        req.i += 3;
    }

}
