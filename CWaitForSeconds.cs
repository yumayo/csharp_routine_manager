using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class CWaitForSeconds : IEnumerable
    {
        private DateTime _startTime;
        private float _seconds;
        public CWaitForSeconds(float seconds)
        {
            _startTime = DateTime.Now;
            this._seconds = seconds;
        }
        public IEnumerator GetEnumerator()
        {
            while ((float)(DateTime.Now - _startTime).TotalSeconds < _seconds)
            {
                yield return 0;
            }
        }
    }
}
