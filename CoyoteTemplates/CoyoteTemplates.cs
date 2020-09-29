using Microsoft.Coyote;
using Microsoft.Coyote.SystematicTesting;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Templates
{
    public class CoyoteTemplates
    {
        public static void FactTemplate()
        {
            Func<Task> toRun = new Func<Task>(bob);
            var configuration = Configuration.Create().WithTestingIterations(1000).WithRandomStrategy();
            var testingEngine = TestingEngine.Create(configuration, toRun);
            testingEngine.Run();

            var report = testingEngine.TestReport;
            if (report.BugReports.Count > 0)
            {
                Console.WriteLine("Found {0} bugs", report.BugReports.Count);

                var reports = report.BugReports.ToArray();

                for (int i = 0; i < reports.Length; i++)
                {
                    Console.WriteLine(reports[i]);
                }
                Assert.True(false, "Test failed");
            }

            Console.WriteLine("Test passed");
        }

        public class GeneratedLambda_GreetTestTheory
        {
            private object[] args;
            private Func<System.Int32, System.Int32, Task> method;

            public GeneratedLambda_GreetTestTheory(Func<System.Int32, System.Int32, Task> method, params object[] args)
            {
                this.args = args;
                this.method = method;
            }

            public Task ToFuncTask() => this.method((Int32)args[0], (Int32)args[1]);
        }

        static Task bob() => Task.CompletedTask;

        public static void TheoryTemplate(params object[] args)
        {
            Func<Task> toRun = null;
            var configuration = Configuration.Create().WithTestingIterations(1000).WithRandomStrategy();
            var testingEngine = TestingEngine.Create(configuration, toRun);
            testingEngine.Run();

            var report = testingEngine.TestReport;
            if (report.BugReports.Count > 0)
            {
                Console.WriteLine("Found {0} bugs", report.BugReports.Count);
                foreach (var r in report.BugReports)
                {
                    Console.WriteLine(r);
                }
                Assert.True(false, "Test failed");
            }

            Console.WriteLine("Test passed");
        }
    }
}
