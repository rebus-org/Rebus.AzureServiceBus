using System;
using System.ComponentModel;

namespace Rebus.Internals
{
    static class DisposableExtensions
    {
        public static IDisposable AsDisposable<T>(this T instance, Action<T> disposeAction) => new Disposable(() => disposeAction(instance));

        class Disposable : IDisposable
        {
            readonly Action _action;
            public Disposable(Action action) => _action = action;
            public void Dispose() => _action();
        }
    }
}