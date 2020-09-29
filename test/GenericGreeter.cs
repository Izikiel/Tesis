using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ProgramCode
{
    public class GenericGreeter<T>
    {
        public T Value { get; private set; }

        public async Task Say(T value)
        {
            await this.WriteWithDelayAsync(value);
        }

        public async Task WriteWithDelayAsync(T value)
        {
            await Task.Delay(100);
            this.Value = value;
        }
    }
}
