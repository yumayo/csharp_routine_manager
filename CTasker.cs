using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    public class CTasker
    {
        private Task _task;
        private CancellationTokenSource _tokenSource = new CancellationTokenSource();

        public void Run(Func<CancellationToken, Task> method)
        {
            _task = Task.Run(async () =>
            {
                try
                {
                    await method?.Invoke(_tokenSource.Token);
                }
                catch
                {

                }
            });
        }

        public void Cancel()
        {
            _tokenSource.Cancel();
        }

        public void Dispose()
        {
            _tokenSource.Cancel();

            try
            {
                _task?.Wait();
            }
            catch
            {

            }
            finally
            {
                _task.Dispose();
            }
        }
    }
}
