using System.Threading.Tasks;

namespace ProgramCode
{
    public class Greeter
    {
        public const string HelloWorld = "Hello World!";
        public const string GoodMorning = "Good Morning";

        public string Value { get; private set; }

        public async Task SayHelloWorld()
        {
            await this.WriteWithDelayAsync(Greeter.HelloWorld);
        }

        public async Task SayGoodMorning()
        {
            await this.WriteWithDelayAsync(Greeter.GoodMorning);
        }

        public async Task WriteWithDelayAsync(string value)
        {
            await Task.Delay(100);
            this.Value = value;
        }
    }
}
