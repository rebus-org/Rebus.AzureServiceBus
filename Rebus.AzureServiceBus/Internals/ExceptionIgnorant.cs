using System;
using System.Threading.Tasks;

namespace Rebus.Internals
{
    class ExceptionIgnorant
    {
        readonly int _maxAttemps;

        public ExceptionIgnorant(int maxAttemps = 5)
        {
            _maxAttemps = maxAttemps;
        }

        public ExceptionIgnorant Ignore<TException>(Func<TException, bool> criteria = null)
        {
            return this;
        }

        public async Task Execute(Func<Task> function)
        {
            var attempt = 0;

            while (attempt < _maxAttemps)
            {
                try
                {
                    await function();
                    break;
                }
                catch (Exception exception)
                {

                }
            }
        }
    }
}