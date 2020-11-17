using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using ShoppingCart = LoggerCodeGen.ShoppingCart;

namespace LoggerCodeGen
{
    class Program
    {
        static void Main(string[] args)
        {
            var container = new ServiceCollection();
            container.AddLogging(logging => logging.AddConsole());

            container.AddTransient<StepOne.PaymentCalculator>();

            container.AddTransient<StepTwo.PaymentCalculator>();
            container.AddLogger<StepTwo.IPaymentCalculatorLogger>();

            container.AddTransient<StepThree.PaymentCalculator>();
            container.AddLogger<StepThree.IPaymentCalculatorLogger>();

            container.AddTransient<StepFour.PaymentCalculator>();
            container.AddLogger<StepFour.IPaymentCalculatorLogger>();

            var services = container.BuildServiceProvider();

            var shoppingCart = new ShoppingCart
            {
                Region = "Antarctica"
            };

            services
                .GetRequiredService<StepOne.PaymentCalculator>()
                .DetermineTaxes(shoppingCart);

            services
                .GetRequiredService<StepTwo.PaymentCalculator>()
                .DetermineTaxes(shoppingCart);

            services
                .GetRequiredService<StepThree.PaymentCalculator>()
                .DetermineTaxes(shoppingCart);

            services
                .GetRequiredService<StepFour.PaymentCalculator>()
                .DetermineTaxes(shoppingCart);
        }
    }

    public class ShoppingCart
    {
        public string Region { get; set; }
    }
}

namespace StepOne
{
    // initial project which has typed loggers in constructor arguments

    public class PaymentCalculator
    {
        public PaymentCalculator(ILogger<PaymentCalculator> logger)
        {
            Logger = logger;
        }

        public ILogger<PaymentCalculator> Logger { get; }

        internal void DetermineTaxes(ShoppingCart shoppingCart)
        {
            Logger.LogInformation("User is in region {TaxRegionCode}", shoppingCart.Region);
        }
    }
}

namespace StepTwo
{
    // after a control-dot codefix on the `ILogger<PaymentCalculator> logger` warning "Create strongly typed logger interface"

    public interface IPaymentCalculatorLogger : ILogger<PaymentCalculator>
    {
    }

    public class PaymentCalculator
    {
        public PaymentCalculator(IPaymentCalculatorLogger logger)
        {
            Logger = logger;
        }

        public IPaymentCalculatorLogger Logger { get; }

        internal void DetermineTaxes(ShoppingCart shoppingCart)
        {
            Logger.LogInformation("User is in region {TaxRegionCode}", shoppingCart.Region);
        }
    }
}

namespace StepThree
{
    // after a control-dot codefix on the `Logger.LogInformation(...);` call site
    // warning "Create strongly typed log methods"
    // 
    // possibly on "entire project" or "entire solution" if log messages already have EventId argument
    // or one at a time to fix method name on each call site as you go

    public interface IPaymentCalculatorLogger : ILogger<PaymentCalculator>
    {
        [LogInformation("User is in region {TaxRegionCode}")]
        void UserRegion(string tagRegionCode);
    }

    public class PaymentCalculator
    {
        public PaymentCalculator(IPaymentCalculatorLogger logger)
        {
            Logger = logger;
        }

        public IPaymentCalculatorLogger Logger { get; }

        internal void DetermineTaxes(ShoppingCart shoppingCart)
        {
            Logger.UserRegion(shoppingCart.Region);
        }
    }
}

namespace StepFour
{
    // base interface technically option from that point forward, and can be removed by user
    // personally - I would keep it in place. there may be places which accept ILogger where it could be passed.
    //
    // but more importantly I would like to use .LogXxxxx syntax when I'm roughing-in any new messages I'm adding.
    // why? I don't want to leave the class I'm working on in order to add a log message, and I might adjust the 
    // text a few times before I'm ready to send a PR.
    //
    // when I'm ready to make a PR and done adding/removing/editing new messages 
    // I would control-dot "Create strongly typed log methods" on "entire solution"

    [LogCategory("StepFour.PaymentCalculator")]
    public interface IPaymentCalculatorLogger
    {
        [LogInformation(1086, "User is in region {TaxRegionCode}")]
        void UserRegion(string tagRegionCode);
    }

    public class PaymentCalculator
    {
        public PaymentCalculator(IPaymentCalculatorLogger logger)
        {
            Logger = logger;
        }

        public IPaymentCalculatorLogger Logger { get; }

        internal void DetermineTaxes(ShoppingCart shoppingCart)
        {
            Logger.UserRegion(shoppingCart.Region);
        }
    }
}
