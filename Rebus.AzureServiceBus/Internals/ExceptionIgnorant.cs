using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Rebus.Internals
{
    class ExceptionIgnorant
    {
        readonly List<ExceptionToIgnore> _exceptionsToIgnore = new List<ExceptionToIgnore>();
        readonly TimeSpan _delayBetweenAttempts;
        readonly int _maxAttemps;

        public ExceptionIgnorant(int maxAttemps = 5, TimeSpan delayBetweenAttempts = default(TimeSpan))
        {
            _maxAttemps = maxAttemps;
            _delayBetweenAttempts = delayBetweenAttempts;
        }

        public ExceptionIgnorant Ignore<TException>(Func<TException, bool> ignoreCriteria = null) where TException : Exception
        {
            _exceptionsToIgnore.Add(new ExceptionToIgnore(typeof(TException), exception => ignoreCriteria?.Invoke((TException)exception) ?? true));
            return this;
        }

        public async Task Execute(Func<Task> function, CancellationToken cancellationToken = default(CancellationToken))
        {
            var attempt = 0;

            bool TriedTooManyTimes() => attempt >= _maxAttemps;

            bool ShouldIgnoreException(Exception ex) => _exceptionsToIgnore.Any(e => e.ShouldIgnore(ex));

            while (true)
            {
                attempt++;

                try
                {
                    await function();
                    break;
                }
                catch (Exception exception)
                {
                    if (TriedTooManyTimes() || !ShouldIgnoreException(exception))
                    {
                        ExceptionDispatchInfo.Capture(exception).Throw();
                    }

                    await Task.Delay(_delayBetweenAttempts, cancellationToken);
                }
            }
        }

        class ExceptionToIgnore
        {
            readonly Type _exceptionType;
            readonly Func<Exception, bool> _ignoreCriteria;

            public ExceptionToIgnore(Type exceptionType, Func<Exception, bool> ignoreCriteria)
            {
                _exceptionType = exceptionType ?? throw new ArgumentNullException(nameof(exceptionType));
                _ignoreCriteria = ignoreCriteria ?? throw new ArgumentNullException(nameof(ignoreCriteria));
            }

            public bool ShouldIgnore(Exception exception) => _exceptionType.IsInstanceOfType(exception)
                                                             && _ignoreCriteria(exception);
        }
    }
}