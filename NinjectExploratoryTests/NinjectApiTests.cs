using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Ninject;
using Ninject.Extensions.Conventions;
using Ninject.Modules;
using NUnit.Framework;

namespace NinjectExploratoryTests
{
    [TestFixture]
    public class NinjectApiTests
    {
        [Test]
        public void ResolvingConcreteTypesWithoutPriorBinding()
        {
            // arrange
            var kernel = new StandardKernel();

            // act & assert
            kernel.Get<SauceBéarnais>()
                .Should().NotBeNull().And.BeAssignableTo<SauceBéarnais>();
        }

        [Test]
        public void ResolvingAbstractTypes()
        {
            // arrange
            var kernel = new StandardKernel();
            kernel.Bind<IIngredient>().To<SauceBéarnais>();

            // act & assert
            kernel.Get<IIngredient>()
                .Should().NotBeNull().And.BeAssignableTo<SauceBéarnais>();
        }

        [Test]
        public void ResolvingWeaklyTypedServices()
        {
            // arrange
            var kernel = new StandardKernel();

            // act & assert
            kernel.Get(typeof(SauceBéarnais))
                .Should().NotBeNull().And.BeAssignableTo<SauceBéarnais>();
        }

        [Test]
        public void WhenMulptipleBindingsExistExceptionIsThrown()
        {
            // arrange
            var kernel = new StandardKernel();
            kernel.Bind<IIngredient>().To<SauceBéarnais>();
            kernel.Bind<IIngredient>().To<Steak>();

            // act
            Action action = () => kernel.Get<IIngredient>();

            // assert
            action.ShouldThrow<ActivationException>();
        }

        [Test]
        public void AutoRegistration_BindSingleInterface()
        {
            // arrange
            var kernel = new StandardKernel();
            kernel.Bind(
                scanner =>
                    scanner.From(typeof(Steak).Assembly)
                        .SelectAllClasses()
                        .InheritedFrom(typeof(IIngredient))
                        .BindSingleInterface());

            // act & assert
            kernel.GetAll<IIngredient>().Should().NotBeEmpty();
        }

        [Test]
        public void AutoRegistrationWithFilter()
        {
            // arrange
            var kernel = new StandardKernel();
            kernel.Bind(
                scanner =>
                    scanner.From(typeof(Steak).Assembly)
                        .SelectAllClasses()
                        .InheritedFrom(typeof(IIngredient))
                        .Where(o => o.Name.StartsWith("Sauce"))
                        .BindSingleInterface());

            // act & assert
            kernel.GetAll<IIngredient>()
                .Select(o => o.GetType()).Should().NotContain(typeof(Steak));
        }

        [Test]
        public void PackagingConfigurationIntoModules()
        {
            // arrange
            var kernel = new StandardKernel();
            kernel.Load<IngredientModule>();

            // act & assert
            kernel.GetAll<IIngredient>().Should().HaveCount(2);
        }

        [Test]
        public void ConfiguringInstanceScope()
        {
            // arrange
            var kernel = new StandardKernel();
            kernel.Bind<IIngredient>().To<SauceBéarnais>().InSingletonScope();

            // act & assert
            var firstInstance = kernel.Get<IIngredient>();
            kernel.Get<IIngredient>().Should().NotBeNull().And.Be(firstInstance);
        }

        [Test]
        public void DefaultScopeIsTransient()
        {
            // arrange
            var kernel = new StandardKernel();
            kernel.Bind<IIngredient>().To<SauceBéarnais>();

            // act & assert
            var firstInstance = kernel.Get<IIngredient>();
            kernel.Get<IIngredient>().Should().NotBe(firstInstance);
        }

        [Test]
        public void ReleasingComponentsWithDefaultTransientScopeDoesNotDisposeThem()
        {
            // arrange
            var kernel = new StandardKernel();
            var component = kernel.Get<DisposableComponent>();

            // act
            kernel.Release(component);

            // assert
            component.IsDisposed.Should().BeFalse();
        }

        [Test]
        public void ReleasingComponentsWithNonTransientScopeDisposeThem()
        {
            // arrange
            var kernel = new StandardKernel();
            kernel.Bind<DisposableComponent>().ToSelf().InSingletonScope();
            var component = kernel.Get<DisposableComponent>();

            // act
            kernel.Release(component);

            // assert
            component.IsDisposed.Should().BeTrue();
        }

        [Test]
        public void DisposingKernelDoesNotDisposeComponentsWithDefaultTransientScope()
        {
            // arrange
            var kernel = new StandardKernel();
            var component = kernel.Get<DisposableComponent>();

            // act
            kernel.Dispose();

            // assert
            component.IsDisposed.Should().BeFalse();
        }

        [Test]
        public void DisposingKernelDisposesComponentsWithNonTransientScope()
        {
            // arrange
            var kernel = new StandardKernel();
            kernel.Bind<DisposableComponent>().ToSelf().InSingletonScope();
            var component = kernel.Get<DisposableComponent>();

            // act
            kernel.Dispose();

            // assert
            component.IsDisposed.Should().BeTrue();
        }

        [Test]
        public void WiringAmbiguousDependenciesWithConstructorArgument()
        {
            // arrange
            var kernel = new StandardKernel();
            kernel.Bind<IIngredient>().To<SauceBéarnais>().Named("sauce");
            kernel.Bind<IIngredient>().To<Steak>().Named("steak");
            kernel.Bind<TwoIngredientCourse>()
                .ToSelf()
                .WithConstructorArgument("sauce", ctx => ctx.Kernel.Get<IIngredient>("sauce"))
                .WithConstructorArgument("steak", ctx => ctx.Kernel.Get<IIngredient>("steak"));

            // act
            var course = kernel.Get<TwoIngredientCourse>();

            // assert
            course.SauceIngredient.Should().BeAssignableTo<SauceBéarnais>();
            course.SteakIngredient.Should().BeAssignableTo<Steak>();
        }

        [Test]
        public void WiringAmbiguousDependenciesWithFilteredBinding()
        {
            // arrange
            var kernel = new StandardKernel();
            kernel.Bind<IIngredient>().To<SauceBéarnais>()
                .When(r => r.Target != null && r.Target.Name == "sauce");
            kernel.Bind<IIngredient>().To<Steak>()
                .When(r => r.Target != null && r.Target.Name == "steak");

            // act
            var course = kernel.Get<TwoIngredientCourse>();

            // assert
            course.SauceIngredient.Should().BeAssignableTo<SauceBéarnais>();
            course.SteakIngredient.Should().BeAssignableTo<Steak>();
        }

        [Test]
        public void AutoWirinigSequences()
        {
            // arrange
            var kernel = new StandardKernel();
            kernel.Bind<IIngredient>().To<SauceBéarnais>();
            kernel.Bind<IIngredient>().To<Steak>();

            // act
            kernel.Get<MultiIngredientCourse>().Ingredients.Should().HaveCount(2);
        }

        [Test]
        public void PickingOnlySomeComponentsFromALargerSet()
        {
            // arrange
            var kernel = new StandardKernel();
            kernel.Bind<IIngredient>().To<SauceBéarnais>().Named("sauce");
            kernel.Bind<IIngredient>().To<Steak>().Named("steak");
            kernel.Bind<MultiIngredientCourse>()
                .ToSelf()
                .WithConstructorArgument(
                    "ingredients",
                    ctx => new[] { ctx.Kernel.Get<IIngredient>("sauce") });

            // act
            kernel.Get<MultiIngredientCourse>().Ingredients.Should().HaveCount(1);
        }

        [Test]
        public void WiringDecorators()
        {
            // arrange
            var kernel = new StandardKernel();
            kernel.Bind<IIngredient>().To<VealCutlet>().WhenInjectedInto<Breading>();
            kernel.Bind<IIngredient>().To<Breading>();

            // act & assert
            kernel.Get<IIngredient>().Should().BeAssignableTo<Breading>();
        }

        [Test]
        public void ConfiguringPrimitiveDependencies()
        {
            // arrange
            var kernel = new StandardKernel();
            kernel.Bind<Spiciness>().ToConstant(Spiciness.Hot);

            // act & assert
            kernel.Get<ChiliConCarne>().Spiciness.Should().Be(Spiciness.Hot);
        }

        [Test]
        public void ConfiguringPrimitiveDependenciesWithConstructorArgument()
        {
            // arrange
            var kernel = new StandardKernel();
            kernel.Bind<ChiliConCarne>().ToSelf()
                .WithConstructorArgument("spiciness", Spiciness.Medium);

            // act & assert
            kernel.Get<ChiliConCarne>().Spiciness.Should().Be(Spiciness.Medium);
        }

        [Test]
        public void ConfiguringPrimitiveDependenciesWithCodeBlock()
        {
            // arrange
            var kernel = new StandardKernel();
            kernel.Bind<ChiliConCarne>()
                .ToMethod(_ => new ChiliConCarne(Spiciness.Medium));

            // act & assert
            kernel.Get<ChiliConCarne>().Spiciness.Should().Be(Spiciness.Medium);
        }

        [Test]
        public void WiringPropertyInjection()
        {
            // arrange
            var kernel = new StandardKernel();
            kernel.Bind<CaesarSalad>().ToSelf()
                .WithPropertyValue("Extra", ctx => ctx.Kernel.Get<Chicken>());

            // act & assert
            kernel.Get<CaesarSalad>().Extra.Should().BeAssignableTo<Chicken>();
        }

        [Test]
        public void WiringPropertyInjectionWithAttributes()
        {
            // arrange
            var kernel = new StandardKernel();
            kernel.Bind<IIngredient>().To<Chicken>();

            // act & assert
            kernel.Get<AttributedCaesarSalad>().Extra.Should().BeAssignableTo<Chicken>();
        }

        public class IngredientModule : NinjectModule
        {
            public override void Load()
            {
                Bind<IIngredient>().To<Steak>();
                Bind<IIngredient>().To<SauceBéarnais>();
            }
        }

        public class TwoIngredientCourse : ICourse
        {
            public IIngredient SteakIngredient { get; private set; }
            public IIngredient SauceIngredient { get; private set; }

            public TwoIngredientCourse(IIngredient steak, IIngredient sauce)
            {
                SteakIngredient = steak;
                SauceIngredient = sauce;
            }
        }

        public class MultiIngredientCourse : ICourse
        {
            public IEnumerable<IIngredient> Ingredients { get; private set; }

            public MultiIngredientCourse(IEnumerable<IIngredient> ingredients)
            {
                Ingredients = ingredients;
            }
        }

        public class ChiliConCarne : ICourse
        {
            public Spiciness Spiciness { get; private set; }

            public ChiliConCarne(Spiciness spiciness)
            {
                Spiciness = spiciness;
            }
        }

        public class CaesarSalad : ICourse
        {
            public IIngredient Extra { get; set; }
        }

        public class AttributedCaesarSalad : ICourse
        {
            [Inject]
            public IIngredient Extra { get; set; }
        }

        public class Chicken : IIngredient
        {
        }

        public enum Spiciness
        {
            Mild = 0,
            Medium,
            Hot
        }

        public interface ICourse
        {
        }

        public class SauceBéarnais : IIngredient
        {
        }

        public class Steak : IIngredient
        {
        }

        public class VealCutlet : IIngredient
        {
        }

        public class Breading : IIngredient
        {
            public IIngredient Ingredient { get; private set; }

            public Breading(IIngredient ingredient)
            {
                Ingredient = ingredient;
            }
        }

        public interface IIngredient
        {
        }

        public class DisposableComponent : IDisposable
        {
            public bool IsDisposed { get; set; }

            public void Dispose()
            {
                IsDisposed = true;
            }
        }
    }
}