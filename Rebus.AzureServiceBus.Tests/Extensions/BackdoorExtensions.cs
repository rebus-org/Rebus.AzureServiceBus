using System;
using System.Reflection;
using Rebus.Bus;

namespace Rebus.AzureServiceBus.Tests.Extensions
{
    static class BackdoorExtensions
    {
        public static void RaiseBusStartedBackdoor(this BusLifetimeEvents busLifetimeEvents)
        {
            var methodInfo = busLifetimeEvents.GetType()
                                 .GetMethod("RaiseBusStarting", BindingFlags.NonPublic | BindingFlags.Instance)
                             ?? throw new ArgumentException("Could not find the method");

            methodInfo.Invoke(busLifetimeEvents, new object[0]);
        }
    }
}