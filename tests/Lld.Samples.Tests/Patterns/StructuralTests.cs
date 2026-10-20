using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Adapter = Lld.Samples.Patterns.Structural.Adapter;
using Bridge = Lld.Samples.Patterns.Structural.Bridge;
using Composite = Lld.Samples.Patterns.Structural.Composite;
using Decorator = Lld.Samples.Patterns.Structural.Decorator;
using Facade = Lld.Samples.Patterns.Structural.Facade;
using Flyweight = Lld.Samples.Patterns.Structural.Flyweight;
using Proxy = Lld.Samples.Patterns.Structural.Proxy;

namespace Lld.Samples.Tests.Patterns;

/// <summary>What the Structural pattern lessons (spec 006 batch 2) claim about each design.</summary>
public sealed class StructuralTests
{
    [Fact]
    public void Adapter_EveryVersionChargesTheSame()
    {
        var expected = new Adapter.PaymentResult(true, "tok-1250");

        new Adapter.Before.Checkout(new Adapter.LegacyPayGateway()).Pay(12.50m, "tok").Should().Be(expected);
        new Adapter.After.Checkout(new Adapter.After.LegacyPayAdapter(new Adapter.LegacyPayGateway())).Pay(12.50m, "tok").Should().Be(expected);
        Adapter.Modern.LegacyPayExtensions.ChargeRupees(new Adapter.LegacyPayGateway(), 12.50m, "tok").Should().Be(expected);
    }

    [Fact]
    public void Adapter_After_TheClientCanBeGivenAnyGateway() =>
        new Adapter.After.Checkout(new FakeGateway()).Pay(1m, "tok").Should().Be(new Adapter.PaymentResult(true, "fake"));

    [Fact]
    public void Adapter_After_ADeclineComesBackPlain() =>
        new Adapter.After.LegacyPayAdapter(new Adapter.LegacyPayGateway()).Charge(0m, "tok").Should().Be(new Adapter.PaymentResult(false, null));

    [Theory]
    [InlineData("ok-card", true)]
    [InlineData("bad-card", false)]
    public void Facade_OneCallDoesWhatThePageDidStepByStep(string card, bool placed)
    {
        var before = Shop();
        var after = Shop();

        var expected = new Facade.Before.CheckoutPage(before.Inventory, before.Payments, before.Shipping, before.Mailer).Buy("pen", card, "Pune");
        var actual = new Facade.After.OrderFacade(after.Inventory, after.Payments, after.Shipping, after.Mailer).PlaceOrder("pen", card, "Pune");

        actual.Should().Be(expected);
        actual.Placed.Should().Be(placed);
        after.Inventory.Left("pen").Should().Be(before.Inventory.Left("pen")).And.Be(placed ? 4 : 5, "a failed payment puts the stock back");
    }

    [Fact]
    public void Facade_Modern_OneCallRegistersTheSubsystem()
    {
        using var services = Facade.Modern.OrderingRegistration.AddOrdering(new ServiceCollection()).BuildServiceProvider();

        services.GetRequiredService<Facade.After.OrderFacade>().PlaceOrder("pen", "ok-card", "Pune").Placed.Should().BeTrue();
        services.GetRequiredService<Facade.Mailer>().Sent.Should().ContainSingle();
    }

    [Fact]
    public void Decorator_Before_EachCombinationIsAnotherSubclass()
    {
        var sender = new Decorator.Before.RetryingLoggingEmailSender();

        sender.Send("asha", "hi");

        sender.Log.Should().Equal("sending to asha");
        typeof(Decorator.Before.EmailSender).Assembly.GetTypes()
            .Count(t => t.Namespace == typeof(Decorator.Before.EmailSender).Namespace && t.IsSubclassOf(typeof(Decorator.Before.EmailSender)))
            .Should().Be(2, "logging, and logging + retry, are separate classes; retry alone would be a third");
    }

    [Fact]
    public void Decorator_After_StackedWrappersEachAddTheirPart()
    {
        var log = new List<string>();
        var flaky = new FlakySender(failures: 2);

        new Decorator.After.LoggingSender(new Decorator.After.RetrySender(flaky, log), log).Send("asha", "hi");

        log.Should().Equal("sending to asha", "retry 1", "retry 2");
        flaky.Delivered.Should().Equal("asha: hi");
    }

    [Fact]
    public void Decorator_After_TheProductionStackStillSends()
    {
        var email = new Decorator.After.EmailSender();

        Decorator.After.Senders.Production(email, []).Send("ravi", "bill");

        email.Delivered.Should().Equal("ravi: bill");
    }

    [Fact]
    public async Task Decorator_Modern_ADelegatingHandlerRetriesTheRequest()
    {
        var server = new ScriptedHandler(HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK);
        using var client = new HttpClient(new Decorator.Modern.RetryHandler { InnerHandler = server });

        using var response = await client.GetAsync(new Uri("http://example.test/"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        server.Calls.Should().Be(2);
    }

    [Fact]
    public void Proxy_TheCachedRatesGiveTheSamePricesWithFarFewerCalls()
    {
        decimal[] prices = [10m, 20m, 30m];
        var direct = new Proxy.RemoteRates();
        var behindProxy = new Proxy.RemoteRates();

        var before = new Proxy.Before.PriceList(direct).InCurrency(prices, "INR");
        var after = new Proxy.After.PriceList(new Proxy.After.CachedRates(behindProxy)).InCurrency(prices, "INR");

        after.Should().Equal(before);
        direct.Calls.Should().Be(6);
        behindProxy.Calls.Should().Be(2, "one call per currency");
    }

    [Fact]
    public void Proxy_Modern_LazyLoadsOnFirstUseOnly()
    {
        var loads = 0;
        var report = new Proxy.Modern.Report(() =>
        {
            loads++;
            return ["a", "b"];
        });

        report.IsLoaded.Should().BeFalse();
        report.RowCount.Should().Be(2);
        report.RowCount.Should().Be(2);
        loads.Should().Be(1);
    }

    [Fact]
    public void Composite_EveryVersionAddsUpTheSameTree()
    {
        var oldDocs = new Composite.Before.Folder("docs");
        oldDocs.Items.Add(new Composite.Before.FileItem("a.txt", 10));
        var oldRoot = new Composite.Before.Folder("root");
        oldRoot.Items.AddRange([oldDocs, new Composite.Before.FileItem("b.txt", 5)]);

        var root = new Composite.After.Folder("root")
            .Add(new Composite.After.Folder("docs").Add(new Composite.After.FileItem("a.txt", 10)))
            .Add(new Composite.After.FileItem("b.txt", 5));

        var modern = new Composite.Modern.FolderNode("root",
            [new Composite.Modern.FolderNode("docs", [new Composite.Modern.FileNode("a.txt", 10)]), new Composite.Modern.FileNode("b.txt", 5)]);

        root.Size.Should().Be(Composite.Before.Sizes.SizeOf(oldRoot)).And.Be(15);
        Composite.Modern.Nodes.Size(modern).Should().Be(15);
    }

    [Fact]
    public void Composite_Before_AnUnknownKindOfItemBreaksTheWalk() =>
        FluentActions.Invoking(() => Composite.Before.Sizes.SizeOf("a shortcut")).Should().Throw<ArgumentException>();

    [Fact]
    public void Composite_After_ANewKindOfItemJustJoinsTheTree() =>
        new Composite.After.Folder("root").Add(new Shortcut()).Add(new Composite.After.FileItem("a", 3)).Size.Should().Be(4);

    [Fact]
    public void Bridge_KindsAndChannelsCombineFreely()
    {
        new Bridge.After.UrgentAlert(new Bridge.After.SmsChannel()).Send("disk full").Should().Be(new Bridge.Before.UrgentSmsAlert().Send("disk full"));
        new Bridge.After.ReminderAlert(new Bridge.After.EmailChannel()).Send("pay").Should().Be(new Bridge.Before.ReminderEmailAlert().Send("pay"));
        new Bridge.After.UrgentAlert(new PushChannel()).Send("down").Should().Be("push: URGENT down", "a new channel is one class, for every kind");
    }

    [Fact]
    public void Bridge_Modern_TheContainerPairsKindsWithChannels()
    {
        using var services = Bridge.Modern.AlertRegistration.AddAlerts(new ServiceCollection()).BuildServiceProvider();

        services.GetRequiredService<Bridge.After.UrgentAlert>().Send("down").Should().Be("sms: URGENT down");
        services.GetRequiredService<Bridge.After.ReminderAlert>().Send("pay").Should().Be("email: reminder: pay");
    }

    [Fact]
    public void Flyweight_TheForestSharesTwoTypesInsteadOfAThousandTextures()
    {
        var before = Flyweight.Before.Forest.Plant(1000);
        var types = new Flyweight.After.TreeTypes();
        var after = Flyweight.After.Forest.Plant(1000, types);

        before.Select(t => t.Texture).Distinct().Should().HaveCount(1000);
        after.Select(t => t.Type).Distinct().Should().HaveCount(2);
        types.Count.Should().Be(2);
        after.Select(t => (t.X, t.Type.Kind)).Should().Equal(before.Select(t => (t.X, t.Kind)));
    }

    [Fact]
    public void Flyweight_Modern_KnownTypesAreBuiltOnce() =>
        Flyweight.Modern.TreeKinds.Of("oak").Should().BeSameAs(Flyweight.Modern.TreeKinds.Of("oak"));

    private static (Facade.Inventory Inventory, Facade.Payments Payments, Facade.Shipping Shipping, Facade.Mailer Mailer) Shop() =>
        (new Facade.Inventory(), new Facade.Payments(), new Facade.Shipping(), new Facade.Mailer());

    private sealed class FakeGateway : Adapter.After.IPaymentGateway
    {
        public Adapter.PaymentResult Charge(decimal rupees, string cardToken) => new(true, "fake");
    }

    /// <summary>Fails with an I/O error the first <c>failures</c> times, then delivers.</summary>
    private sealed class FlakySender(int failures) : Decorator.After.IMessageSender
    {
        private int _failuresLeft = failures;

        public List<string> Delivered { get; } = [];

        public void Send(string to, string text)
        {
            if (_failuresLeft-- > 0)
            {
                throw new IOException("network down");
            }

            Delivered.Add($"{to}: {text}");
        }
    }

    /// <summary>Answers each request with the next status in the script.</summary>
    private sealed class ScriptedHandler(params HttpStatusCode[] script) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(script[Math.Min(Calls++, script.Length - 1)]));
    }

    private sealed class Shortcut : Composite.After.IFileSystemItem
    {
        public string Name => "shortcut";

        public long Size => 1;
    }

    private sealed class PushChannel : Bridge.After.IChannel
    {
        public string Deliver(string message) => $"push: {message}";
    }
}
