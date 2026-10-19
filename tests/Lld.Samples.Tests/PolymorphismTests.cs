using Lld.Samples.OopSolid.Polymorphism.After;
using Lld.Samples.OopSolid.Polymorphism.Language;
using Before = Lld.Samples.OopSolid.Polymorphism.Before;

namespace Lld.Samples.Tests;

/// <summary>What the Polymorphism lesson claims about each design.</summary>
public sealed class PolymorphismTests
{
    [Fact]
    public void Before_AnUnknownCarrier_FailsOnlyAtRuntime()
    {
        var drone = (Before.Carrier)3;

        var cost = () => Before.Shipping.Cost(drone, 1);

        cost.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void After_EachMethodAnswersForItself()
    {
        IShippingMethod[] methods = [new Post(), new Courier(), new Pickup()];

        methods.Select(m => m.Cost(2)).Should().Equal(5.10m, 11.40m, 0m);
        methods.Select(m => m.Cost(2)).Should().Equal(
            [Before.Carrier.Post, Before.Carrier.Courier, Before.Carrier.Pickup],
            (after, carrier) => after == Before.Shipping.Cost(carrier, 2));
    }

    [Fact]
    public void After_ANewMethod_WorksWithCodeWrittenBeforeIt()
    {
        IShippingMethod[] methods = [new Post(), new Courier(), new Drone()];

        Checkout.Fastest(methods).Should().BeOfType<Drone>();
        Checkout.Cheapest(methods, weightKg: 5).Should().BeOfType<Post>();
    }

    [Fact]
    public void PatternMatching_AddsOperationsWithoutTouchingTheTypes()
    {
        Shape[] shapes = [new Circle(1), new Rectangle(2, 3)];

        shapes.Select(Geometry.Area).Should().Equal(Math.PI, 6);
        shapes.Select(Geometry.Perimeter).Should().Equal(2 * Math.PI, 10);
    }

    [Fact]
    public void Overloads_AreChosenAtCompileTime()
    {
        var circle = new Circle(1);
        Shape asShape = circle;

        Describer.Describe(circle).Should().Be("a circle");
        Describer.Describe(asShape).Should().Be("a shape", "the variable is a Shape, so the Shape overload was chosen");
    }

    [Fact]
    public void Generics_OneMethodForManyTypes()
    {
        Generic.Largest([3, 9, 4]).Should().Be(9);
        Generic.Largest(["pear", "apple"]).Should().Be("pear");
        Generic.Sum([1.5m, 2.5m]).Should().Be(4m);
        Generic.Sum([1, 2, 3]).Should().Be(6);
    }
}
