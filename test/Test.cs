using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgramCode
{
    public class Test
    {
        private int x = 0;

        private bool printPositive = false;

        public void Inc()
        {
            this.x++;
            if (this.x > 0)
            {
                this.printPositive = true;
            }
        }

        public void Dec()
        {
            this.x--;
            if (this.x < 0)
            {
                this.printPositive = false;
            }
        }

        public async Task<bool> Operate()
        {
            var t1 = Task.Run(() => this.Inc());
            var t2 = Task.Run(() => this.Dec());

            await Task.WhenAll(t1, t2).ConfigureAwait(false);

            Console.WriteLine(this.printPositive ? "Positive!" : "Negative!");

            return this.printPositive;
        }
    }

}
