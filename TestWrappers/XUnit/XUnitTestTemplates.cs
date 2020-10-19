
using Microsoft.Coyote;
using Microsoft.Coyote.SystematicTesting;
using System;
using System.Threading.Tasks;
using Xunit;

namespace TestWrappers.XUnit
{
    public static class XUnitTestTemplates
    {
        public static void FactTemplate()
        {
            using var testWrapper = new XUnitTestWrapper(null, null, null);
            XUnitTestTemplates.RunTestInCoyote(testWrapper.Invoke);
        }

        public static void TheoryTemplate(params object[] args)
        {
            using var testWrapper = new XUnitTestWrapper(null, null, args);
            XUnitTestTemplates.RunTestInCoyote(testWrapper.Invoke);
        }

        public static void RunTestInCoyote(Func<Task> toRun)
        {
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
