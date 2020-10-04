using ProgramCode;
using Microsoft.Coyote;
using Microsoft.Coyote.SystematicTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using System.IO;
using System.Linq;
using System.Diagnostics;

namespace test
{
    public class Program
    {
        //ITestOutputHelper output;

        //public Program(ITestOutputHelper output)
        //{
        //    this.output = output;
        //}


        //public static void Main()
        //{
        //    Debugger.Break();
        //    Console.WriteLine("hola!");
        //}

        [Fact]
        public static async Task GreetTest()
        {
            var greeter = new Greeter();
            var task2 = greeter.SayHelloWorld();
            var task3 = greeter.SayHelloWorld();
            var task1 = greeter.SayGoodMorning();
            var task4 = greeter.SayHelloWorld();
            var task5 = greeter.SayHelloWorld();

            await Task.WhenAll(task1, task2, task3, task4, task5);

            Console.WriteLine(greeter.Value);

            Assert.True(greeter.Value == Greeter.HelloWorld, $"Value is '{greeter.Value}' instead of '{Greeter.HelloWorld}'.");
        }


        //[Fact]
        //public void CoyoteGreeterTest()
        //{
        //    Func<Task> toRun = () => GreetTest();
        //    var configuration = Configuration.Create().WithTestingIterations(1000).WithRandomStrategy();
        //    var testingEngine = TestingEngine.Create(configuration, toRun);
        //    testingEngine.Run();

        //    var report = testingEngine.TestReport; 
        //    if (report.BugReports.Count > 0) 
        //    { 
        //        this.output.WriteLine("Found {0} bugs", report.BugReports.Count); 
        //        foreach (var r in report.BugReports) 
        //        { 
        //            this.output.WriteLine(r); 
        //        } 
        //        Assert.True(false, "Test failed"); 
        //    } 

        //    this.output.WriteLine("Test passed");
        //}

        [Theory]
        [InlineData(3, 4)]
        [InlineData(3, 0)]
        [InlineData(0, 1)]
        public static async Task GreetTestTheory(int hello, int goodMorning)
        {
            var greeter = new Greeter();

            var tasks = new List<Task>();

            for (int i = 0; i < hello; i++)
            {
                tasks.Add(greeter.SayHelloWorld());
            }

            for (int i = 0; i < goodMorning; i++)
            {
                tasks.Add(greeter.SayGoodMorning());
            }

            await Task.WhenAll(tasks);

            Console.WriteLine(greeter.Value);

            Assert.True(greeter.Value == Greeter.HelloWorld, $"Value is '{greeter.Value}' instead of '{Greeter.HelloWorld}'.");
            Assert.True(false, "Estoy en la interna!");
        }


        //[Theory]
        //[InlineData(3, 4)]
        //[InlineData(3, 0)]
        //[InlineData(0, 1)]
        //public void CoyoteGreeterTestTheory(int hello, int goodMorning)
        //{
        //    Func<Task> toRun = () => GreetTestTheory(hello, goodMorning);
        //    var configuration = Configuration.Create().WithTestingIterations(1000).WithRandomStrategy();
        //    var testingEngine = TestingEngine.Create(configuration, toRun);
        //    testingEngine.Run();

        //    var report = testingEngine.TestReport;
        //    if (report.BugReports.Count > 0)
        //    {
        //        this.output.WriteLine("Found {0} bugs", report.BugReports.Count);
        //        foreach (var r in report.BugReports)
        //        {
        //            this.output.WriteLine(r);
        //        }
        //        Assert.True(false, "Test failed");
        //    }

        //    this.output.WriteLine("Test passed");
        //}

        //[Theory]
        //[InlineData(3, 4)]
        //[InlineData(3, 0)]
        //[InlineData(0, 1)]
        //[InlineData("hola", "manola")]
        //public static async Task GenericGreetTestTheory<T>(T expected, T random)
        //{
        //    var genericGreeter = new GenericGreeter<T>();

        //    var tasks = new List<Task>();

        //    for (int i = 0; i < 10; i++)
        //    {
        //        tasks.Add(genericGreeter.Say(expected));
        //    }

        //    for (int i = 0; i < 2; i++)
        //    {
        //        tasks.Add(genericGreeter.Say(random));
        //    }

        //    await Task.WhenAll(tasks);

        //    Console.WriteLine(genericGreeter.Value);

        //    Assert.True(EqualityComparer<T>.Default.Equals(genericGreeter.Value, expected), $"Value is '{genericGreeter.Value}' instead of '{expected}'.");
        //}

        //[Theory]
        //[InlineData(3, 4)]
        //[InlineData(3, 0)]
        //[InlineData(0, 1)]
        //[InlineData("hola", "manola")]
        //public void CoyoteGenericGreetTestTheory<T>(T expected, T random)
        //{
        //    Func<Task> toRun = () => GenericGreetTestTheory<T>(expected, random);
        //    var configuration = Configuration.Create().WithTestingIterations(1000).WithRandomStrategy();
        //    var testingEngine = TestingEngine.Create(configuration, toRun);
        //    testingEngine.Run();

        //    var report = testingEngine.TestReport;
        //    if (report.BugReports.Count > 0)
        //    {
        //        this.output.WriteLine("Found {0} bugs", report.BugReports.Count);
        //        foreach (var r in report.BugReports)
        //        {
        //            this.output.WriteLine(r);
        //        }
        //        Assert.True(false, "Test failed");
        //    }

        //    this.output.WriteLine("Test passed");
        //}


        class LogAdapter : TextWriter
        {
            ITestOutputHelper output;

            public LogAdapter(ITestOutputHelper output)
            {
                this.output = output;
            }

            public override Encoding Encoding => Encoding.UTF8;

            public override void Write(string value)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    output.WriteLine(value.Trim('\n').Trim('\r'));
                }
            }
        }

    }
}
