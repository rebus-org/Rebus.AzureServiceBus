using System;
using System.Collections.Generic;

namespace Rebus.Internals;

static class DisposableExtensions
{
    public static void DisposeCollection(this IEnumerable<IDisposable> disposables)
    {
        foreach (var disposable in disposables)
        {
            disposable.Dispose();
        }
    }

    public static IDisposable AsDisposable<T>(this T instance, Action<T> disposeAction) => new Disposable(() => disposeAction(instance));

    class Disposable : IDisposable
    {
        readonly Action _action;
        public Disposable(Action action) => _action = action ?? throw new ArgumentNullException(nameof(action));
        public void Dispose() => _action();
    }
}