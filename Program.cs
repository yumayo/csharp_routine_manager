using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Program
    {
        static CRoutineManager manager = new CRoutineManager();

        static CRoutine routine;

        static void Main(string[] args)
        {
            manager.Start(Co1());
            //manager.Start(Co3());

            routine = manager.StartParallel(Co1(), Co2());

            manager.Start(Co4());

            manager.Stop(routine);


            CTasker tasker = new CTasker();
            tasker.Run(manager.Update);

            while (true)
            {
                Thread.Sleep(100);
            }
        }

        static IEnumerable Co1()
        {
            //yield return manager.Start(Co2());

            for(int i = 0; i < 100; ++i)
            {
                Console.WriteLine($"[1]test{i}");
                yield return 0;
            }
        }

        static IEnumerable Co2()
        {
            for (int i = 0; i < 100; ++i)
            {
                Console.WriteLine($"[2]test{i}");
                yield return 0;
            }
        }

        static IEnumerable Co3()
        {
            for (int i = 0; i < 200; ++i)
            {
                Console.WriteLine($"[3]test{i % 100}");
                yield return 0;
            }
        }

        static IEnumerable Co4()
        {
            yield return routine;
            yield return manager.Start(Co3());
        }
    }

    class CWaitForSeconds : IEnumerable
    {
        public DateTime startTime;
        public float seconds;
        public CWaitForSeconds(float seconds)
        {
            startTime = DateTime.Now;
            this.seconds = seconds;
        }
        public IEnumerator GetEnumerator()
        {
            while((float)(DateTime.Now - startTime).TotalSeconds < seconds)
            {
                yield return 0;
            }
        }
    }

    class CRoutine : IEnumerable
    {
        public int handle;
        public Stack<IEnumerator> routine = new Stack<IEnumerator>();
        public IEnumerator enumerator;

        public enum OpCode
        {
            FINISHED = 1 << 0,
            CANCELED = 1 << 1,
        }

        public int opCode;

        public CRoutine(int handle, IEnumerable enumerable)
        {
            this.handle = handle;
            routine.Push(enumerable.GetEnumerator());
            enumerator = CoUpdate();
        }

        public void Cancel()
        {
            opCode |= (int)OpCode.CANCELED;
        }

        private IEnumerator CoUpdate()
        {
            while(true)
            {
                var top = routine.Peek();
                if (top.MoveNext())
                {
                    if (top.Current is CRoutine r)
                    {
                        if (r.opCode == 0)
                        {
                            routine.Push(r.GetEnumerator());
                            yield return 0;
                        }
                        // 次にpushしようとしているものがすでにキャンセルされている場合は、
                        // 次のフレームで終了することがわかっているため、このフレーム内で更に先に進ませます。
                    }
                    else if (top.Current is IEnumerable enumerable)
                    {
                        routine.Push(enumerable.GetEnumerator());
                        yield return 0;
                    }
                    else if (top.Current is IEnumerator enumerator)
                    {
                        routine.Push(enumerator);
                        yield return 0;
                    }
                    else
                    {
                        yield return 0;
                    }
                }
                else
                {
                    if (routine.Count > 1)
                    {
                        routine.Pop();
                        // 取り出した場合には一フレーム次の処理が遅れてしまうのがいやなため、
                        // このフレーム内で更に先に進みます。
                    }
                    else
                    {
                        opCode |= (int)OpCode.FINISHED;
                        yield break;
                    }
                }
            }
        }

        public override int GetHashCode()
        {
            return handle;
        }

        public IEnumerator GetEnumerator()
        {
            while(opCode == 0)
            {
                yield return 0;
            }
        }
    }


    class CRoutineManager : IEnumerable
    {
        public int handle;

        public HashSet<CRoutine> routines = new HashSet<CRoutine>();

        public enum OpCode
        {
            ADD,
            REMOVE,
        }

        public ConcurrentDictionary<CRoutine, OpCode> updates = new ConcurrentDictionary<CRoutine, OpCode>();

        public CRoutine Start(IEnumerable routine)
        {
            handle += 1;
            CRoutine r = new CRoutine(handle, routine);

            if(updateing)
            {
                if(r.enumerator.MoveNext())
                {
                    updates.TryAdd(r, OpCode.ADD);
                }
            }
            else
            {
                routines.Add(r);
            }

            return r;
        }

        public CRoutine StartParallel(params IEnumerable[] enumerables)
        {
            var manager = new CRoutineManager();
            foreach(var enumerable in enumerables)
            {
                manager.Start(enumerable);
            }
            return Start(manager);
        }

        public void Stop(CRoutine routine)
        {
            if (updateing)
            {
                if(updates.TryAdd(routine, OpCode.REMOVE))
                {
                    routine.Cancel();
                }
            }
            else
            {
                if(routines.Remove(routine))
                {
                    routine.Cancel();
                }
            }
        }

        public void StopAllCoroutines()
        {
            foreach(var routine in routines)
            {
                Stop(routine);
            }
        }

        public async Task Update(CancellationToken token)
        {
            var co = GetEnumerator();
            while(co.MoveNext())
            {
                token.ThrowIfCancellationRequested();
                await Task.Delay((int)(1.0F / 60.0F * 1000.0F));
            }
        }

        private bool updateing = false;

        public IEnumerator GetEnumerator()
        {
            while (routines.Count > 0)
            {
                try
                {
                    updateing = true;

                    foreach (var routine in routines)
                    {
                        if (routine.enumerator.MoveNext())
                        {

                        }
                        else
                        {
                            Stop(routine);
                        }
                    }

                    updateing = false;

                    foreach (var update in updates)
                    {
                        switch (update.Value)
                        {
                            case OpCode.ADD:
                                routines.Add(update.Key);
                                break;
                            case OpCode.REMOVE:
                                routines.Remove(update.Key);
                                break;
                            default:
                                break;
                        }
                    }

                    updates.Clear();
                }
                catch(Exception e)
                {
                    Console.WriteLine(e);
                }

                yield return new CWaitForSeconds(1.0F / 60.0F);
            }
        }
    }

    class CTasker
    {
        Task task;
        CancellationTokenSource tokenSource = new CancellationTokenSource();

        public void Run(Func<CancellationToken, Task> method)
        {
            task = Task.Run(async () => 
            {
                try
                {
                    await method?.Invoke(tokenSource.Token);
                }
                catch
                {

                }
            });
        }

        public void Clear()
        {
            tokenSource.Cancel();
        }

        public void Dispose()
        {
            tokenSource.Cancel();

            try
            {
                task?.Wait();
            }
            catch
            {

            }
            finally
            {
                task.Dispose();
            }
        }
    }
}
