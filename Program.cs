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
}
