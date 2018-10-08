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
            //manager.StartCoroutine(Co1());
            //manager.Start(Co3());

            manager.StartCoroutine(Co5());

            routine = manager.StartCoroutineSequence(Co1(), Co2());

            manager.StartCoroutine(Co4());

            //manager.StopCoroutine(routine);

            CTasker tasker = new CTasker();
            tasker.Run(manager.Update);

            while (true)
            {
                Thread.Sleep(1000);
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
            yield return manager.StartCoroutine(Co3());
        }

        static IEnumerable Co5()
        {
            for (int i = 0; i < 500; ++i)
            {
                Console.WriteLine($"[5]test{i % 100}");
                yield return 0;
            }
        }
    }
}
