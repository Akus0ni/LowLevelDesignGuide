using Chain = Lld.Samples.Patterns.Behavioral.ChainOfResponsibility;
using Command = Lld.Samples.Patterns.Behavioral.Command;
using Interpreter = Lld.Samples.Patterns.Behavioral.Interpreter;
using Iterator = Lld.Samples.Patterns.Behavioral.Iterator;
using Mediator = Lld.Samples.Patterns.Behavioral.Mediator;
using Memento = Lld.Samples.Patterns.Behavioral.Memento;
using Observer = Lld.Samples.Patterns.Behavioral.Observer;
using State = Lld.Samples.Patterns.Behavioral.State;
using Strategy = Lld.Samples.Patterns.Behavioral.Strategy;
using TemplateMethod = Lld.Samples.Patterns.Behavioral.TemplateMethod;
using Visitor = Lld.Samples.Patterns.Behavioral.Visitor;

namespace Lld.Samples.Tests.Patterns;

/// <summary>What the Behavioral pattern lessons (spec 006 batches 3 and 4) claim about each design.</summary>
public sealed class BehavioralTests
{
    private static readonly Strategy.Order Parcel = new(Subtotal: 500m, WeightKg: 2m);

    [Theory]
    [InlineData("standard", 540)]
    [InlineData("express", 680)]
    [InlineData("pickup", 500)]
    public void Strategy_EveryVersionPricesShippingTheSame(string shipping, decimal total)
    {
        Strategy.After.IShippingCost strategy = shipping switch
        {
            "standard" => new Strategy.After.StandardShipping(),
            "express" => new Strategy.After.ExpressShipping(),
            _ => new Strategy.After.StorePickup(),
        };
        var function = shipping switch
        {
            "standard" => Strategy.Modern.ShippingCosts.Standard,
            "express" => Strategy.Modern.ShippingCosts.Express,
            _ => Strategy.Modern.ShippingCosts.Pickup,
        };

        Strategy.Before.Checkout.Total(Parcel, shipping).Should().Be(total);
        new Strategy.After.Checkout(strategy).Total(Parcel).Should().Be(total);
        new Strategy.Modern.Checkout(function).Total(Parcel).Should().Be(total);
    }

    [Fact]
    public void Strategy_After_ANewWayToShipIsANewClass_CheckoutUnchanged() =>
        new Strategy.After.Checkout(new SameDayShipping()).Total(Parcel).Should().Be(750m);

    [Fact]
    public void TemplateMethod_Before_TheCopiesHaveDrifted() =>
        TemplateMethod.Before.HtmlReport.Render([" pen "]).Should().Contain("<li> pen </li>", "the HTML copy never got the trim fix");

    [Fact]
    public void TemplateMethod_EveryReportFollowsTheSameSteps()
    {
        string[] rows = [" pen ", "", "ink"];

        new TemplateMethod.After.CsvReport().Render(rows).Should().Be(TemplateMethod.Before.CsvReport.Render(rows)).And.Be("item\npen\nink\n");
        new TemplateMethod.After.HtmlReport().Render(rows).Should().Be("<ul>\n<li>pen</li>\n<li>ink</li>\n</ul>\n");
        TemplateMethod.Modern.Reports.Render(rows, TemplateMethod.Modern.Reports.Html).Should().Be(new TemplateMethod.After.HtmlReport().Render(rows));
    }

    [Fact]
    public void Observer_EveryVersionTellsTheSameListeners()
    {
        var (mailer, sms, analytics) = (new Observer.Mailer(), new Observer.Sms(), new Observer.Analytics());
        new Observer.Before.OrderService(mailer, sms, analytics).Place("A1");

        var (mailer2, sms2, analytics2) = (new Observer.Mailer(), new Observer.Sms(), new Observer.Analytics());
        var orders = new Observer.After.OrderService();
        orders.Subscribe(new Observer.After.EmailOnOrder(mailer2));
        orders.Subscribe(new Observer.After.SmsOnOrder(sms2));
        orders.Subscribe(new Observer.After.TrackOrder(analytics2));
        orders.Place("A1");

        (mailer2.Sent, sms2.Sent, analytics2.Tracked).Should().BeEquivalentTo((mailer.Sent, sms.Sent, analytics.Tracked));
    }

    [Fact]
    public void Observer_After_ListenersComeAndGoWithoutTouchingTheOrderCode()
    {
        var orders = new Observer.After.OrderService();
        var points = new LoyaltyPoints();
        orders.Subscribe(points);

        orders.Place("A1");
        orders.Unsubscribe(points);
        orders.Place("A2");

        points.Orders.Should().Equal("A1");
    }

    [Fact]
    public void Observer_Modern_AnEventIsTheObserverList()
    {
        var orders = new Observer.Modern.OrderService();
        var heard = new List<string>();
        void Listener(object? sender, Observer.Modern.OrderPlacedEventArgs e) => heard.Add(e.OrderId);

        orders.Placed += Listener;
        orders.Place("A1");
        orders.Placed -= Listener;
        orders.Place("A2");

        heard.Should().Equal("A1");
    }

    [Fact]
    public void Command_Before_OnlyTheLastActionCanBeUndone()
    {
        var editor = new Command.Editor();
        var toolbar = new Command.Before.Toolbar(editor);

        toolbar.TypeClicked("Hello");
        toolbar.TypeClicked(" world");
        toolbar.UndoClicked();
        toolbar.UndoClicked();

        editor.Text.Should().Be("Hello", "the second undo has nothing left to restore");
    }

    [Fact]
    public void Command_After_EveryStepCanBeUndone_InOrder()
    {
        var editor = new Command.Editor();
        var history = new Command.After.CommandHistory();

        history.Run(new Command.After.TypeCommand(editor, "Hello world"));
        history.Run(new Command.After.DeleteCommand(editor, 6));
        editor.Text.Should().Be("Hello");

        history.Undo();
        editor.Text.Should().Be("Hello world");
        history.Undo();
        editor.Text.Should().BeEmpty();
        history.Count.Should().Be(0);
    }

    [Fact]
    public void Command_Modern_ARecordOfTwoDelegatesUndoesToo()
    {
        var editor = new Command.Editor();
        var history = new Command.Modern.History();

        history.Run(Command.Modern.Edits.Type(editor, "Hi"));
        history.Run(Command.Modern.Edits.Type(editor, "!"));
        history.Undo();

        editor.Text.Should().Be("Hi");
    }

    [Fact]
    public void Iterator_EveryVersionWalksTheSameSongs()
    {
        Iterator.Song[] songs = [new("One"), new("Two", Played: true), new("Three")];
        var before = new Iterator.Before.Playlist();
        before.Songs.AddRange(songs);
        var after = new Iterator.After.Playlist();
        foreach (var song in songs)
        {
            after.Add(song);
        }

        Iterator.After.Player.UnplayedTitles(after).Should().Equal(Iterator.Before.Player.UnplayedTitles(before)).And.Equal("One", "Three");
        new Iterator.Modern.Playlist(songs).Unplayed().Select(s => s.Title).Should().Equal("One", "Three");
        new Iterator.Modern.Playlist(songs).Backwards().Select(s => s.Title).Should().Equal("Three", "Two", "One");
    }

    [Fact]
    public void Iterator_Before_CallersCanEmptyThePlaylist()
    {
        var playlist = new Iterator.Before.Playlist();
        playlist.Songs.Add(new Iterator.Song("One"));

        playlist.Songs.Clear();

        playlist.Songs.Should().BeEmpty("the inner list was handed out");
    }

    [Fact]
    public void State_EveryVersionAllowsTheSameMoves()
    {
        var before = new State.Before.Document();
        var after = new State.After.Document();

        before.Edit("v1");
        after.Edit("v1");
        before.Submit();
        after.Submit();
        before.Reject();
        after.Reject();
        before.Submit();
        after.Submit();
        before.Approve();
        after.Approve();

        after.Status.Should().Be(before.Status.ToString()).And.Be("Published");
        after.Text.Should().Be("v1");
        State.Modern.DocumentFlow.Next(State.Modern.Status.InReview, State.Modern.DocumentAction.Approve).Should().Be(State.Modern.Status.Published);
    }

    [Fact]
    public void State_IllegalMovesAreRefusedEverywhere()
    {
        var published = new State.After.Document();
        published.Submit();
        published.Approve();

        published.Invoking(d => d.Edit("late")).Should().Throw<InvalidOperationException>().WithMessage("Can't edit a document that is Published.");
        new State.Before.Document().Invoking(d => d.Approve()).Should().Throw<InvalidOperationException>();
        FluentActions.Invoking(() => State.Modern.DocumentFlow.Next(State.Modern.Status.Draft, State.Modern.DocumentAction.Approve))
            .Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(500, "travel", "approved by team lead")]
    [InlineData(5_000, "travel", "approved by manager")]
    [InlineData(50_000, "hardware", "approved by director")]
    [InlineData(500_000, "hardware", "nobody can approve this")]
    [InlineData(100, "gift card", "rejected by fraud check")]
    public void Chain_EveryVersionRoutesTheExpenseTheSameWay(double amount, string category, string answer)
    {
        var expense = new Chain.Expense((decimal)amount, category);
        var pipeline = Chain.Modern.Pipeline.Build(
            Chain.Modern.Steps.FraudCheck,
            Chain.Modern.Steps.Approver("team lead", 1_000m),
            Chain.Modern.Steps.Approver("manager", 10_000m),
            Chain.Modern.Steps.Approver("director", 100_000m));

        Chain.Before.Approvals.Approve(expense).Should().Be(answer);
        Chain.After.Approvals.Chain().Handle(expense).Should().Be(answer);
        pipeline(expense).Should().Be(answer);
    }

    [Fact]
    public void Chain_After_ANewLinkIsInsertedWithoutTouchingTheOthers()
    {
        var withCfo = new Chain.After.Approver("director", 100_000m, new Chain.After.Approver("CFO", 1_000_000m, next: null));

        withCfo.Handle(new Chain.Expense(500_000m, "hardware")).Should().Be("approved by CFO");
    }

    [Theory]
    [InlineData("IN", "411001", true)]
    [InlineData("IN", "SW1A 1AA", false)]
    [InlineData("UK", "SW1A 1AA", true)]
    public void Mediator_EveryVersionEnablesSubmitTheSameWay(string country, string postcode, bool enabled)
    {
        var submit = new Mediator.Before.SubmitButton();
        var box = new Mediator.Before.PostcodeBox(submit);
        new Mediator.Before.CountryBox(box, submit).Select(country);
        box.Type(postcode);

        var form = new Mediator.After.AddressForm();
        form.Country.Select(country);
        form.Postcode.Type(postcode);

        form.Submit.Enabled.Should().Be(submit.Enabled).And.Be(enabled);
    }

    [Fact]
    public void Mediator_After_ChangingTheCountryReChecksThePostcode()
    {
        var form = new Mediator.After.AddressForm();
        form.Postcode.Type("SW1A 1AA");
        form.Submit.Enabled.Should().BeFalse();

        form.Country.Select("UK");

        form.Submit.Enabled.Should().BeTrue("the form, not the country box, knows the rule");
    }

    [Fact]
    public void Mediator_Modern_MessagesReachWhoeverRegistered()
    {
        var messenger = new Mediator.Modern.Messenger();
        var heard = new List<string>();
        messenger.Register<Mediator.Modern.CountryChanged>(m => heard.Add(m.Country));

        messenger.Send(new Mediator.Modern.CountryChanged("UK"));
        messenger.Send(new Mediator.Modern.PostcodeTyped("ignored"));

        heard.Should().Equal("UK");
    }

    [Fact]
    public void Memento_EveryVersionUndoesTheSameWay()
    {
        var before = new Memento.Before.Editor();
        var beforeHistory = new Memento.Before.History(before);
        var after = new Memento.After.Editor();
        var afterHistory = new Memento.After.History(after);
        var modern = new Memento.Modern.Editor();

        beforeHistory.Save();
        afterHistory.Save();
        before.Type("Hello");
        after.Type("Hello");
        modern.Type("Hello");
        beforeHistory.Save();
        afterHistory.Save();
        before.Type(" world");
        after.Type(" world");
        modern.Type(" world");

        beforeHistory.Undo();
        afterHistory.Undo();
        modern.Undo();

        (after.Text, after.Cursor).Should().Be((before.Text, before.Cursor)).And.Be(("Hello", 5));
        modern.State.Should().Be(new Memento.Modern.EditorState("Hello", 5));
    }

    [Fact]
    public void Memento_After_TheSnapshotShowsNothingOutside() =>
        typeof(Memento.After.Editor.Snapshot).GetProperties().Should().BeEmpty("only the editor can read a snapshot");

    [Fact]
    public void Visitor_EveryVersionComputesTheSameResults()
    {
        Visitor.Before.IShape[] before = [new Visitor.Before.Circle(1), new Visitor.Before.Square(2)];
        Visitor.After.IShape[] after = [new Visitor.After.Circle(1), new Visitor.After.Square(2)];
        Visitor.Modern.Shape[] modern = [new Visitor.Modern.Circle(1), new Visitor.Modern.Square(2)];

        after.Select(s => s.Accept(new Visitor.After.SvgExport())).Should().Equal(before.Select(s => s.ToSvg()));
        after.Select(s => s.Accept(new Visitor.After.AreaCalculator())).Should().Equal(before.Select(s => s.Area()));
        modern.Select(Visitor.Modern.Exports.ToSvg).Should().Equal(before.Select(s => s.ToSvg()));
    }

    [Fact]
    public void Visitor_After_ANewOperationIsANewVisitor() =>
        new Visitor.After.Square(3).Accept(new NameVisitor()).Should().Be("square");

    [Theory]
    [InlineData("member and total>1000", 1500, true, true)]
    [InlineData("member and total>1000", 1500, false, false)]
    [InlineData("member and total>1000", 500, true, false)]
    public void Interpreter_EveryVersionAgreesOnAndRules(string rule, double total, bool member, bool applies)
    {
        var cart = new Interpreter.Cart((decimal)total, member);
        var lambda = Interpreter.Modern.RuleExpressions.Lambda(System.Linq.Expressions.Expression.AndAlso(
            Interpreter.Modern.RuleExpressions.Member, Interpreter.Modern.RuleExpressions.TotalOver(1000m))).Compile();

        Interpreter.Before.Discounts.Applies(rule, cart).Should().Be(applies);
        Interpreter.After.RuleParser.Parse(rule).Interpret(cart).Should().Be(applies);
        lambda(cart).Should().Be(applies);
    }

    [Theory]
    [InlineData("member or total>1000", 1500, false, true)]
    [InlineData("not member", 10, false, true)]
    [InlineData("member and not total>1000", 1500, true, false)]
    public void Interpreter_After_OrAndNotJustWork(string rule, double total, bool member, bool applies) =>
        Interpreter.After.RuleParser.Parse(rule).Interpret(new Interpreter.Cart((decimal)total, member)).Should().Be(applies);

    [Fact]
    public void Interpreter_Before_OrIsNotUnderstood() =>
        Interpreter.Before.Discounts.Applies("member or total>1000", new Interpreter.Cart(1500m, IsMember: false))
            .Should().BeTrue("'member or total>1000' is one unknown part, so nothing is checked at all");

    private sealed class NameVisitor : Visitor.After.IShapeVisitor<string>
    {
        public string VisitCircle(Visitor.After.Circle circle) => "circle";

        public string VisitSquare(Visitor.After.Square square) => "square";
    }

    private sealed class SameDayShipping : Strategy.After.IShippingCost
    {
        public decimal For(Strategy.Order order) => 250m;
    }

    private sealed class LoyaltyPoints : Observer.After.IOrderObserver
    {
        public List<string> Orders { get; } = [];

        public void OrderPlaced(string orderId) => Orders.Add(orderId);
    }
}
