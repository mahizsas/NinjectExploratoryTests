using System;
using System.ComponentModel.DataAnnotations;
using System.Security;
using FluentAssertions;
using Ninject;
using Ninject.Extensions.Interception;
using Ninject.Extensions.Interception.Infrastructure.Language;
using NUnit.Framework;

namespace NinjectExploratoryTests
{
    [TestFixture]
    public class NinjectInterceptionApiTests
    {
        private const int InvalidProduct = -1;
        private const int NotAllowedProduct = 1;

        [Test]
        public void AddingErrorHandlingInterceptor()
        {
            // arrange
            var kernel = new StandardKernel();
            var exceptionHandler = new ErrorHandlingInterceptor();
            kernel
                .Bind<IProductManagementService>()
                .To<ProductManagementService>()
                .Intercept()
                .With(exceptionHandler);
            var service = kernel.Get<IProductManagementService>();

            // act & assert
            service.InsertProduct(InvalidProduct).Should().BeFalse();
            exceptionHandler.CatchedException
                .Should().BeOfType<ValidationException>();
        }

        [Test]
        public void AddingMultipleOrderedInterceptors()
        {
            // arrange
            var kernel = new StandardKernel();
            var binding = kernel
                .Bind<IProductManagementService>()
                .To<ProductManagementService>();
            var exceptionHandler = new ErrorHandlingInterceptor();
            binding.Intercept().With(exceptionHandler).InOrder(1);
            binding.Intercept().With<SecurityInterceptor>().InOrder(2);
            var service = kernel.Get<IProductManagementService>();

            // act & assert
            service.InsertProduct(NotAllowedProduct).Should().BeFalse();
            exceptionHandler.CatchedException
                .Should().BeOfType<SecurityException>();
        }

        public class ErrorHandlingInterceptor : IInterceptor
        {
            public Exception CatchedException { get; private set; }

            public void Intercept(IInvocation invocation)
            {
                try
                {
                    invocation.Proceed();
                }
                catch (Exception ex)
                {
                    CatchedException = ex;
                    // logging etc.
                    invocation.ReturnValue = false;
                }
            }
        }

        public class SecurityInterceptor : IInterceptor
        {
            public void Intercept(IInvocation invocation)
            {
                if ((int)invocation.Request.Arguments[0] == NotAllowedProduct)
                    throw new SecurityException();
            }
        }

        public class ProductManagementService : IProductManagementService
        {
            public bool InsertProduct(int product)
            {
                if (product == InvalidProduct)
                    throw new ValidationException();

                return true;
            }
        }

        public interface IProductManagementService
        {
            bool InsertProduct(int product);
        }
    }
}