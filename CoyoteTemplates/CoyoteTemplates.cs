using Microsoft.Coyote;
using Microsoft.Coyote.SystematicTesting;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Templates
{
    public class CoyoteTemplates
    {
        public static void FactTemplate()
        {
            Func<Task> toRun = null;
            var configuration = Configuration.Create().WithTestingIterations(1000).WithRandomStrategy();
            var testingEngine = TestingEngine.Create(configuration, toRun);
            testingEngine.Run();

            var report = testingEngine.TestReport;
            var bugCount = report.BugReports.Count;

            if (bugCount > 0)
            {
                var reports = new string[bugCount];

                report.BugReports.CopyTo(reports);

                Assert.True(false, $"Test failed. Found {bugCount} bugs. Errors: {string.Join(",", reports)}");
            }

            Console.WriteLine("Test passed");
        }

        public static void TheoryTemplate(params object[] args)
        {
            Func<Task> toRun = null;
            var configuration = Configuration.Create().WithTestingIterations(1000).WithRandomStrategy();
            var testingEngine = TestingEngine.Create(configuration, toRun);
            testingEngine.Run();

            var report = testingEngine.TestReport;
            var bugCount = report.BugReports.Count;

            if (bugCount > 0)
            {
                var reports = new string[bugCount];

                report.BugReports.CopyTo(reports);

                Assert.True(false, $"Test failed. Found {bugCount} bugs. Errors: {string.Join(",", reports)}");
            }

            Console.WriteLine("Test passed");
        }
    }
}
