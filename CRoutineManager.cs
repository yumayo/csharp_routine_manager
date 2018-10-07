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
    public class CRoutineManager : IEnumerable
    {
        private int _handle;

        private HashSet<CRoutine> _routines = new HashSet<CRoutine>();

        private enum _OpCode
        {
            ADD,
            REMOVE,
        }

        private ConcurrentDictionary<CRoutine, _OpCode> _updates = new ConcurrentDictionary<CRoutine, _OpCode>();

        public CRoutine Start(IEnumerable routine)
        {
            _handle += 1;
            CRoutine r = new CRoutine(_handle, routine);

            if (_updateing)
            {
                if (r.enumerator.MoveNext())
                {
                    _updates.TryAdd(r, _OpCode.ADD);
                }
            }
            else
            {
                _routines.Add(r);
            }

            return r;
        }

        public CRoutine StartParallel(params IEnumerable[] enumerables)
        {
            var manager = new CRoutineManager();
            foreach (var enumerable in enumerables)
            {
                manager.Start(enumerable);
            }
            return Start(manager);
        }

        public void Stop(CRoutine routine)
        {
            if (_updateing)
            {
                if (_updates.TryAdd(routine, _OpCode.REMOVE))
                {
                    routine.Cancel();
                }
            }
            else
            {
                if (_routines.Remove(routine))
                {
                    routine.Cancel();
                }
            }
        }

        public void StopAllCoroutines()
        {
            foreach (var routine in _routines)
            {
                Stop(routine);
            }
        }

        public async Task Update(CancellationToken token)
        {
            var co = GetEnumerator();
            while (co.MoveNext())
            {
                token.ThrowIfCancellationRequested();
                await Task.Delay((int)(1.0F / 60.0F * 1000.0F));
            }
        }

        private bool _updateing = false;

        public IEnumerator GetEnumerator()
        {
            while (_routines.Count > 0)
            {
                try
                {
                    _updateing = true;

                    foreach (var routine in _routines)
                    {
                        if (!routine.enumerator.MoveNext())
                        {
                            Stop(routine);
                        }
                    }

                    _updateing = false;

                    foreach (var update in _updates)
                    {
                        switch (update.Value)
                        {
                            case _OpCode.ADD:
                                _routines.Add(update.Key);
                                break;
                            case _OpCode.REMOVE:
                                _routines.Remove(update.Key);
                                break;
                            default:
                                break;
                        }
                    }

                    _updates.Clear();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                yield return new CWaitForSeconds(1.0F / 60.0F);
            }
        }
    }
}
