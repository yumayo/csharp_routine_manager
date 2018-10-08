using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    public class CRoutine : IEnumerable
    {
        private int _handle;
        private Stack<IEnumerator> _routine = new Stack<IEnumerator>();
        public IEnumerator enumerator;

        public CRoutine(int handle, IEnumerable enumerable)
        {
            this._handle = handle;
            _routine.Push(enumerable.GetEnumerator());
            enumerator = _CoUpdate();
        }

        private bool _isUpdate = true;

        public void Cancel()
        {
            _isUpdate = false;
        }

        private IEnumerator _CoUpdate()
        {
            while (true)
            {
                var top = _routine.Peek();
                if (top.MoveNext())
                {
                    if (top.Current is CRoutine r)
                    {
                        if (r._isUpdate)
                        {
                            _routine.Push(r.GetEnumerator());
                            yield return 0;
                        }
                        // 次にpushしようとしているものがすでにキャンセルされている場合は、
                        // 次のフレームで終了することがわかっているため、このフレーム内で更に先に進ませます。
                    }
                    else if (top.Current is IEnumerable enumerable)
                    {
                        _routine.Push(enumerable.GetEnumerator());
                        yield return 0;
                    }
                    else if (top.Current is IEnumerator enumerator)
                    {
                        _routine.Push(enumerator);
                        yield return 0;
                    }
                    else
                    {
                        yield return 0;
                    }
                }
                else
                {
                    if (_routine.Count > 1)
                    {
                        _routine.Pop();
                        // 取り出した場合には一フレーム次の処理が遅れてしまうのがいやなため、
                        // このフレーム内で更に先に進みます。
                    }
                    else
                    {
                        _isUpdate = false;
                        yield break;
                    }
                }
            }
        }

        public override int GetHashCode()
        {
            return _handle;
        }

        public IEnumerator GetEnumerator()
        {
            while (_isUpdate)
            {
                yield return 0;
            }
        }
    }
}
