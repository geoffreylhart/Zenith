using System;
using System.Collections.Generic;
using System.Text;

namespace ZEditor.ZComponents.Data
{
    interface IIntListHashObserver
    {
        public void Add(int[] intList);
        public void Remove(int[] intList);
    }
}
