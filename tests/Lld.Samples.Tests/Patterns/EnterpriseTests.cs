using Microsoft.Extensions.DependencyInjection;
using Di = Lld.Samples.Patterns.Enterprise.DiVsServiceLocator;
using NullObject = Lld.Samples.Patterns.Enterprise.NullObject;
using Repository = Lld.Samples.Patterns.Enterprise.Repository;
using Spec = Lld.Samples.Patterns.Enterprise.Specification;
using Uow = Lld.Samples.Patterns.Enterprise.UnitOfWork;

namespace Lld.Samples.Tests.Patterns;

/// <summary>What the Enterprise & .NET pattern lessons (spec 006 batch 5) claim about each design.</summary>
public sealed class EnterpriseTests
{
    private static readonly Spec.Customer[] Customers =
    [
        new("Asha", "IN", IsActive: true, Spent: 25_000m),
        new("Ravi", "IN", IsActive: false, Spent: 50_000m),
        new("Mia", "UK", IsActive: true, Spent: 15_000m),
        new("Kiran", "IN", IsActive: true, Spent: 2_000m),
    ];

    [Fact]
    public void Repository_HandlersGiveTheSameAnswers_ThroughTheRepository()
    {
        var (before, after, id) = (Orders(), Orders(), Guid.NewGuid());
        before.Orders.Add(new Repository.Order(id, "Asha"));
        after.Orders.Add(new Repository.Order(id, "Asha"));
        var repository = new Repository.After.OrderRepository(after);

        new Repository.After.ShipOrder(repository).Execute(id).Should().Be(new Repository.Before.ShipOrder(before).Execute(id)).And.BeTrue();
        new Repository.After.PendingOrdersReport(repository).Customers()
            .Should().Equal(new Repository.Before.PendingOrdersReport(before).Customers()).And.Equal("Ravi");
    }

    [Fact]
    public void Repository_After_HandlersCanBeTestedWithoutTheDatabase()
    {
        var order = new Repository.Order(Guid.NewGuid(), "Mia");

        new Repository.After.ShipOrder(new OneOrderRepository(order)).Execute(order.Id).Should().BeTrue();

        order.Shipped.Should().BeTrue();
    }

    [Fact]
    public void Repository_Modern_NamedQueriesComposeOnAnyQueryable()
    {
        var db = Orders();

        Repository.Modern.OrderQueries.ForCustomer(Repository.Modern.OrderQueries.Pending(db.Orders.AsQueryable()), "Ravi")
            .Select(o => o.Customer).Should().Equal("Ravi");
    }

    [Fact]
    public void UnitOfWork_Before_AFailureHalfwayLeavesHalfTheChangesSaved()
    {
        var store = new Uow.Store { FailNextStockWrite = true };
        var placeOrder = new Uow.Before.PlaceOrder(new Uow.Before.OrderRepository(store), new Uow.Before.StockRepository(store));

        placeOrder.Invoking(p => p.Execute("pen")).Should().Throw<IOException>();

        store.Orders.Should().Equal(["pen"], "the order was saved");
        store.Stock["pen"].Should().Be(3, "but the stock never went down");
    }

    [Fact]
    public void UnitOfWork_After_AFailureSavesNothing_ASuccessSavesEverything()
    {
        var store = new Uow.Store { FailNextStockWrite = true };
        var work = new Uow.After.UnitOfWork(store);
        var placeOrder = new Uow.After.PlaceOrder(new Uow.After.OrderRepository(work), new Uow.After.StockRepository(work), work);

        placeOrder.Invoking(p => p.Execute("pen")).Should().Throw<IOException>();
        (store.Orders.Count, store.Stock["pen"]).Should().Be((0, 3));

        var retry = new Uow.After.UnitOfWork(store);
        new Uow.After.PlaceOrder(new Uow.After.OrderRepository(retry), new Uow.After.StockRepository(retry), retry).Execute("pen");
        (store.Orders.Count, store.Stock["pen"]).Should().Be((1, 2));
    }

    [Fact]
    public void UnitOfWork_Modern_OneUnitOfWorkPerScope()
    {
        using var services = Uow.Modern.OrderingRegistration.AddOrdering(new ServiceCollection()).BuildServiceProvider();
        using var first = services.CreateScope();
        using var second = services.CreateScope();

        first.ServiceProvider.GetRequiredService<Uow.After.UnitOfWork>()
            .Should().BeSameAs(first.ServiceProvider.GetRequiredService<Uow.After.UnitOfWork>())
            .And.NotBeSameAs(second.ServiceProvider.GetRequiredService<Uow.After.UnitOfWork>());
        first.ServiceProvider.GetRequiredService<Uow.After.PlaceOrder>().Execute("pen");
        services.GetRequiredService<Uow.Store>().Orders.Should().Equal("pen");
    }

    [Fact]
    public void Specification_CombinedRulesFindWhatTheExtraMethodsFound()
    {
        var before = new Spec.Before.CustomerRepository(Customers);
        var after = new Spec.After.CustomerRepository(Customers);
        var wanted = new Spec.After.ActiveCustomer().And(new Spec.After.InCountry("IN")).And(new Spec.After.SpentOver(10_000m));

        after.Find(wanted).Should().Equal(before.ActiveInCountryWhoSpentOver("IN", 10_000m)).And.ContainSingle(c => c.Name == "Asha");
        after.Find(new Spec.After.InCountry("UK").Or(new Spec.After.SpentOver(40_000m))).Select(c => c.Name).Should().Equal("Ravi", "Mia");
    }

    [Fact]
    public void Specification_Modern_TheSameRuleRunsInMemoryAndAsAnExpression()
    {
        var activeIndians = Spec.Modern.CustomerSpecs.Active.And(Spec.Modern.CustomerSpecs.InCountry("IN"));

        Customers.Where(activeIndians.IsSatisfiedBy).Select(c => c.Name).Should().Equal("Asha", "Kiran");
        Customers.AsQueryable().Where(activeIndians.Criteria).Select(c => c.Name).Should().Equal("Asha", "Kiran");
    }

    [Fact]
    public void NullObject_Before_EveryNullMustBeChecked() =>
        new NullObject.Before.Checkout(discount: null, log: null).Total(100m).Should().Be(100m);

    [Fact]
    public void NullObject_After_DoNothingObjectsReplaceTheChecks()
    {
        new NullObject.After.Checkout(NullObject.After.NoDiscount.Instance, NullObject.After.SilentLog.Instance).Total(100m).Should().Be(100m);
        new NullObject.After.Checkout(new TenOff(), NullObject.After.SilentLog.Instance).Total(100m).Should().Be(90m);
    }

    [Fact]
    public void NullObject_Modern_TextWriterNullSwallowsOutput()
    {
        new NullObject.Modern.Receipts().Invoking(r => r.Print(10m)).Should().NotThrow();

        using var output = new StringWriter();
        new NullObject.Modern.Receipts(output).Print(10m);
        output.ToString().Should().Be("Total: 10");
    }

    [Fact]
    public void ServiceLocator_Before_TheMissingDependencyOnlyShowsUpWhenCalled()
    {
        var sender = new Di.Before.InvoiceSender();

        sender.Invoking(s => s.Send("Asha")).Should().Throw<InvalidOperationException>().WithMessage("IMailer was never registered.");
    }

    [Fact]
    public void DependencyInjection_After_TheDependencyIsInTheConstructor()
    {
        var mailer = new Di.OutboxMailer();

        new Di.After.InvoiceSender(mailer).Send("Asha");

        mailer.Sent.Should().Equal("Asha: Your invoice is ready.");
    }

    [Fact]
    public void DependencyInjection_Modern_TheContainerWiresItAtTheCompositionRoot()
    {
        using var services = Di.Modern.InvoicingRegistration.AddInvoicing<Di.OutboxMailer>(new ServiceCollection()).BuildServiceProvider();

        services.GetRequiredService<Di.After.InvoiceSender>().Send("Ravi");

        ((Di.OutboxMailer)services.GetRequiredService<Di.IMailer>()).Sent.Should().Equal("Ravi: Your invoice is ready.");
    }

    private static Repository.Database Orders()
    {
        var db = new Repository.Database();
        db.Orders.Add(new Repository.Order(Guid.NewGuid(), "Ravi"));
        return db;
    }

    private sealed class OneOrderRepository(Repository.Order order) : Repository.After.IOrderRepository
    {
        public Repository.Order? FindPending(Guid id) => id == order.Id && !order.Shipped ? order : null;

        public IReadOnlyList<Repository.Order> Pending() => order.Shipped ? [] : [order];

        public void Add(Repository.Order order) => throw new NotSupportedException();
    }

    private sealed class TenOff : NullObject.After.IDiscount
    {
        public decimal For(decimal total) => 10m;
    }
}
