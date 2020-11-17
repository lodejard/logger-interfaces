using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Ignore this implementation, it's based on an earlier dynamic proxy prototype
    /// </summary>
    public static class LoggerExtensions
    {
        private static readonly ProxyGenerator Generator = new ProxyGenerator();

        public static IServiceCollection AddLogger<TLogger>(this IServiceCollection services) where TLogger : class
        {
            var categoryAttribute = typeof(TLogger).GetTypeInfo().GetCustomAttribute<LogCategoryAttribute>();
            var categoryType = typeof(TLogger).GetTypeInfo()
                .FindInterfaces((type, _) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ILogger<>), null)
                .SingleOrDefault()
                ?.GenericTypeArguments
                ?.SingleOrDefault();

            var categoryName =
                categoryAttribute?.Name ??
                (categoryType != null ? TypeNameHelper.GetTypeDisplayName(categoryType, includeGenericParameters: false, nestedTypeDelimiter: '.') : null) ??
                typeof(TLogger).FullName;

            var messages = typeof(TLogger).GetTypeInfo()
                .DeclaredMethods
                .Select(method => (method, attribute: method.GetCustomAttribute<LogMessageAttribute>()))
                .Where(item => item.attribute != null)
                .Select(item => (item.method, logDelegate: MakeLogDelegate(item.method, item.attribute)))
                .ToDictionary(kv => kv.method, kv => kv.logDelegate);

            services.AddTransient(sp =>
            {
                var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger(categoryName);

                return (TLogger)Generator.CreateInterfaceProxyWithTarget(
                    typeof(ILogger),
                    new[] { typeof(TLogger) },
                    logger,
                    new Interceptor(logger, messages));
            });

            return services;
        }

        public delegate void LogDelegate(ILogger logger, object[] parameters);

        static MD5 md5 = MD5.Create();

        static LogDelegate MakeLogDelegate(MethodInfo method, LogMessageAttribute attribute)
        {
            var level = attribute.Level;
            var eventId = MakeEventId(attribute.EventIdNumber, method.Name);
            var message = attribute.Message;

            return LogMethod;

            void LogMethod(ILogger logger, object[] properties)
            {
                logger.Log(level, eventId, message, properties);
            }
        }

        private static EventId MakeEventId(int eventIdNumber, string eventIdName)
        {
            if (eventIdNumber == 0)
            {
                var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(eventIdName)).Take(4).Aggregate(0u, (a, b) => a * 0x100 + b);
                var number = (int)(hash % 9000000) + 1000000;
                return new EventId(number, eventIdName);
            }
            else
            {
                return new EventId(eventIdNumber, eventIdName);
            }
        }

        public class Interceptor : IInterceptor
        {
            private Dictionary<MethodInfo, LogDelegate> _messages;
            private ILogger _logger;

            public Interceptor(ILogger logger, Dictionary<MethodInfo, LogDelegate> messages)
            {
                _logger = logger;
                _messages = messages;
            }

            public void Intercept(IInvocation invocation)
            {
                if (_messages.TryGetValue(invocation.Method, out var logDelegate))
                {
                    logDelegate(_logger, invocation.Arguments);
                }
                else
                {
                    invocation.Proceed();
                }
            }
        }
    }
}
