using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;
using DelftTools.Utils.IO;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using Newtonsoft.Json;
using NUnit.Framework;

namespace DelftTools.TestUtils
{
    public class TestHelper
    {
        private static readonly ILog log = LogManager.GetLogger(typeof (TestHelper));
        private static string solutionRoot;
        private static int assertInTestMethod;
        private static string lastTestName;
        private static Color[] colors;

        public static string SolutionRoot
        {
            get
            {
                if (solutionRoot == null)
                {
                    solutionRoot = GetSolutionRoot();
                }
                return solutionRoot;
            }
        }

        public static string TestDataDirectory
        {
            get
            {
                string testDataPath = SolutionRoot + @"\test-data\";
                return Path.GetDirectoryName(testDataPath);
            }
        }

        public static string GetProjectNameForCurrentMethod()
        {
            MethodBase callingMethod = new StackFrame(1, false).GetMethod(); //.Name;
            return callingMethod.DeclaringType.Name + "." + callingMethod.Name + ".dsproj";
        }
        public static string GetCurrentMethodName()
        {
            MethodBase callingMethod = new StackFrame(1, false).GetMethod(); //.Name;
            return callingMethod.DeclaringType.Name + "." + callingMethod.Name;
            //return "";
        }

        private static string GetCurrentTestClassMethodName()
        {
            var stackTrace = new StackTrace(false);

            for (int i = 1; i < stackTrace.FrameCount; i++)
            {
                StackFrame stackFrame = stackTrace.GetFrame(i);
                if (stackFrame.GetMethod().GetCustomAttributes(true).OfType<TestAttribute>().Count() != 0)
                {
                    MethodBase method = stackFrame.GetMethod();
                    return method.DeclaringType.Name + "." + method.Name;
                }
            }

            return "<unknown test method>";
        }

        /// <summary>
        /// Does an XML compare based on the xml documents. 
        /// TODO: get a more precise assert.
        /// </summary>
        /// <param name="xml1"></param>
        /// <param name="xml2"></param>
        /// <returns></returns>
        public static void AssertXmlEquals(string xml1, string xml2)
        {
            //TODO: get a nicer assert with more info about what is different.
            /*
             * does not work on the build server :( */
            XDocument doc1 = XDocument.Parse(xml1);
            XDocument doc2 = XDocument.Parse(xml2);
            //XmlUnit.XmlAssertion.AssertXmlEquals(xml1,xml2);
            Assert.IsTrue(XNode.DeepEquals(doc1, doc2));


            //do a string compare for now..not the issue the issue is in datetime conversion
            //Assert.AreEqual(xml1,xml2);
        }

        //TODO: get this near functiontest. This is not a general function.

        /// <summary>
        /// Writes an xml file for the given content. Gives a 'nice' layout
        /// </summary>
        /// <param name="path"></param>
        /// <param name="xml"></param>
        public static void WriteXml(string path, string xml)
        {
            /*XDocument doc = XDocument.Parse(xml);
            doc.Save(path);
             */
            //todo: get the formatting nice again. Now 
            File.WriteAllText(path, xml);
        }

        /// <summary>
        /// Returns full path to the file or directory in "test-data"
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetTestDataPath(TestDataPath path)
        {
            return Path.Combine(TestDataDirectory, path.Path);
        }

        /// <summary>
        /// Returns full path to the file or directory in "test-data"
        /// </summary>
        /// <param name="testDataPath"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetTestDataPath(TestDataPath testDataPath, string path)
        {
            return Path.Combine(Path.Combine(TestDataDirectory, testDataPath.Path), path);
        }

        private static string GetSolutionRoot()
        {
            const string solutionName = "DeltaShell-Spatial.sln";
            //get the current directory and scope up
            //TODO find a faster safer method 
            string curDir = ".";
            while (Directory.Exists(curDir) && !File.Exists(curDir + @"\"+solutionName))
            {
                curDir += "/../";
            }
                
            if (!File.Exists(Path.Combine(curDir, solutionName)))
                throw new InvalidOperationException("Solution file not found.");
             
            return Path.GetFullPath(curDir);
        }


        public static string GetDataDir()
        {
            //string solutionRoot = SolutionRoot;
            string testPath = Path.GetFullPath(@".." + Path.DirectorySeparatorChar + "..");
            string testRelativepath = FileUtils.GetRelativePath(SolutionRoot + "test", testPath);
            return Path.Combine(solutionRoot + @"test-data", testRelativepath) + Path.DirectorySeparatorChar;
        }


        /// <summary>
        /// Get's the path in test-data tree section
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static string GetTestFilePath(string filename)
        {
            string path = Path.Combine(GetDataDir(), filename);
            var uri = new UriBuilder(path);

            path = Uri.UnescapeDataString(uri.Path);
            if (File.Exists(path))
                return path;

            // file not found..exception
            throw new FileNotFoundException(String.Format("File not found: {0}", path), path);
        }

        public static double GetRunTime(Action action)
        {
            DateTime startTime = DateTime.Now;
            action();
            return (DateTime.Now - startTime).TotalMilliseconds;
        }

        /// <summary>
        /// TODO: add machine benchmark/rank and use it as a weight when comparing test times, write rank to results file
        /// </summary>
        /// <param name="maxMilliseconds"></param>
        /// <param name="action"></param>
        public static double AssertIsFasterThan(float maxMilliseconds, Action action)
        {
            return AssertIsFasterThan(maxMilliseconds, action, false);
        }

        public static double AssertIsFasterThan(float maxMilliseconds, Action action, bool rankHddAccess)
        {
            return AssertIsFasterThan(maxMilliseconds, null, action, rankHddAccess);
        }

        public static double AssertIsFasterThan(float maxMilliseconds, string message, Action action)
        {
            return AssertIsFasterThan(maxMilliseconds, message, action, false);
        }

        public static double AssertIsFasterThan(float maxMilliseconds, string message, Action action, bool rankHddAccess)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            action();
            stopwatch.Stop();

            var actualMillisecond = stopwatch.ElapsedMilliseconds;

            string testName = GetCurrentTestClassMethodName();

            if (testName == lastTestName) // check if there are more than one assert in a single test
            {
                assertInTestMethod++;
                testName += assertInTestMethod;
            }
            else
            {
                lastTestName = testName;
                assertInTestMethod = 1; // reset
            }

            float machinePerformanceRank = GetMachinePerformanceRank();

            float machineHddPerformanceRank = GetMachineHddPerformanceRank();

            var reportDirectory = GetSolutionRoot() + Path.DirectorySeparatorChar + "target/";
            FileUtils.CreateDirectoryIfNotExists(reportDirectory);
            
            var path = reportDirectory + "performance-times.html";
            WriteTimesToLogFile(maxMilliseconds, (int) actualMillisecond, machinePerformanceRank,
                                machineHddPerformanceRank, rankHddAccess, testName, false, path);

            path = reportDirectory + "performance-times-charts.html";
            WriteTimesToLogFile(maxMilliseconds, (int)actualMillisecond, machinePerformanceRank,
                                machineHddPerformanceRank, rankHddAccess, testName, true, path);


            float rank = machineHddPerformanceRank;

            if (rankHddAccess) // when test relies a lot on HDD - multiply rank by hdd speed factor
            {
                rank *= machineHddPerformanceRank;
            }

            var userMessage = String.IsNullOrEmpty(message) ? "" : message + ". ";
            if (rank != 1.0f)
            {
                Assert.IsTrue(rank*actualMillisecond < maxMilliseconds, userMessage + "Maximum of {0} milliseconds exceeded. Actual was {1}, machine performance weighted actual was {2}",
                              maxMilliseconds, actualMillisecond, actualMillisecond*rank);
                Console.WriteLine(userMessage + String.Format("Test took {1} milliseconds (machine performance weighted {2}). Maximum was {0}",
                                                                                     maxMilliseconds, actualMillisecond, actualMillisecond*rank));
            }
            else
            {
                Assert.IsTrue(actualMillisecond < maxMilliseconds, userMessage + "Maximum of {0} milliseconds exceeded. Actual was {1}", maxMilliseconds,
                              actualMillisecond);
                Console.WriteLine(userMessage + String.Format("Test took {1} milliseconds. Maximum was {0}", maxMilliseconds, actualMillisecond));
            }

            return actualMillisecond;
        }

        private static float GetMachineHddPerformanceRank()
        {
            string rank = Environment.GetEnvironmentVariable("MACHINE_HDD_PERFORMANCE_RANK");
            if (!String.IsNullOrEmpty(rank))
            {
                return Single.Parse(rank);
            }

            return 1.0f;
        }

        private static float GetMachinePerformanceRank()
        {
            string rank = Environment.GetEnvironmentVariable("MACHINE_PERFORMANCE_RANK");
            if (!String.IsNullOrEmpty(rank))
            {
                return Single.Parse(rank);
            }

            return 1.0f;
        }

        private static void WriteTimesToLogFile(float maxMilliseconds, float actualMilliseconds, float machineRank, float machineHddRank, bool useHddAccessRank, string testName, bool includeCharts, string path)
        {
            if (!File.Exists(path))
            {
                if(!includeCharts)
                {
                    File.AppendAllText(path, "<a href=\"performance-times-charts.html\" target=\"_blank\">View with charts</a><br />");
                }

                if (machineRank != 1.0f)
                {
                    File.AppendAllText(path, "Machine performance rank (multiplier):" + machineRank + "<br />");
                    File.AppendAllText(path, "Time is in milliseconds<br /><br />");
                    File.AppendAllText(path, String.Format("<table border=\"1\">\n<tr><th>Time</th><th>Name</th>{0}<th>MaxTime</th><th>ActualTime</th><th>RankedActualTime</th><th>Percentage</th></tr>", includeCharts ? "<th>Chart</th>" : ""));
                }
                else
                {
                    File.AppendAllText(path, "Time is in milliseconds<br /><br />");
                    File.AppendAllText(path, String.Format("<table border=\"1\">\n<tr><th>Time</th><th>Name</th>{0}<th>MaxTime</th><th>ActualTime</th><th>Percentage</th></tr>", includeCharts ? "<th>Chart</th>" : ""));
                }
            }

            string contents = "";

            float rank = machineRank*(useHddAccessRank ? machineHddRank : 1.0f);

            var chartContent = includeCharts ? String.Format("<td><iframe style=\"width:950px;height:350px;border:none\" src=\"performace-test-reports\\{0}.json.html\"></iframe></td>", testName) : "";

            float fraction;
            if (machineRank != 1.0f)
            {
                contents = String.Format(CultureInfo.InvariantCulture,
                                         "<tr><td>{0}</td><td><a href=\"performace-test-reports/{1}.json.html\">{1}</a></td>{2}<td>{3:G5}</td><td>{4:G5}</td><td>{5:G5}</td>",
                                         DateTime.Now, testName, chartContent, maxMilliseconds, actualMilliseconds, actualMilliseconds * rank);
                fraction = (maxMilliseconds - actualMilliseconds*rank)/maxMilliseconds;
            }
            else
            {
                contents = String.Format(CultureInfo.InvariantCulture,
                                         "<tr><td>{0}</td><td><a href=\"performace-test-reports/{1}.json.html\">{1}</a></td>{2}<td>{3:G5}</td><td>{4:G5}</td>", DateTime.Now, testName, chartContent,
                                         maxMilliseconds, actualMilliseconds);
                fraction = (maxMilliseconds - actualMilliseconds)/maxMilliseconds;
            }

            string color = ColorTranslator.ToHtml(GetPerformanceColor(fraction));
            contents += String.Format(CultureInfo.InvariantCulture, "<td bgcolor=\"{0}\">{1:G5}%</td>", color, (100 - fraction*100));

            contents += "</tr>\n";
            File.AppendAllText(path, contents);

            // update test reports in JSON files on build server (tests statistics)
            // TODO: find way to write it somewhere so that it will be shared between build agents

            int buildNumber = 0;
            string s = Environment.GetEnvironmentVariable("BUILD_NUMBER");
            if (!String.IsNullOrEmpty(s))
            {
                buildNumber = Int32.Parse(s); // defined on build server
            }

            // generate JSON files locally
            string testHistoryDirectoryPath = GetSolutionRoot() + "/target/performace-test-reports";
            FileUtils.CreateDirectoryIfNotExists(testHistoryDirectoryPath);

            string testHistoryFilePath = testHistoryDirectoryPath + Path.DirectorySeparatorChar + testName + ".json";

            var testInfos = new List<TestRunInfo>();

            if (File.Exists(testHistoryFilePath))
            {
                testInfos = JsonConvert.DeserializeObject<List<TestRunInfo>>(File.ReadAllText(testHistoryFilePath));
            }

            if(buildNumber == 0)
            {
                if(testInfos == null || testInfos.Count == 0)
                {
                    testInfos = new List<TestRunInfo>();
                }
                else
                {
                    var maxBuildNumber = testInfos.Select(i => i.BuildNumber).Max();

                    // reset build numbers if they were not set
                    foreach (var testInfo in testInfos)
                    {
                        if(testInfo.BuildNumber == 0)
                        {
                            testInfo.BuildNumber = buildNumber;
                            buildNumber++;
                        }
                    }

                    buildNumber = maxBuildNumber + 1;
                }
            }

            int maxTestInfoCount = 100; // max number of tests locally

            while (testInfos.Count >= maxTestInfoCount)
            {
                testInfos.RemoveAt(0);
            }

            testInfos.Add(new TestRunInfo
                              {
                                  TestName = testName,
                                  Actual = actualMilliseconds,
                                  ActualWeighted =
                                      (int) (actualMilliseconds*machineRank*(useHddAccessRank ? machineHddRank : 1.0)),
                                  Max = maxMilliseconds,
                                  MachineHddRank = machineHddRank,
                                  MachineRank = machineRank,
                                  Time = DateTime.Now.ToString(CultureInfo.InvariantCulture),
                                  BuildNumber = buildNumber,
                                  UseMachineHddRank = useHddAccessRank
                              });

            CreateChart(testInfos, testHistoryFilePath + ".html");

            string json = JsonConvert.SerializeObject(testInfos, Formatting.Indented);
            File.WriteAllText(testHistoryFilePath, json);
        }

        private static void CreateChart(List<TestRunInfo> testInfos, string filePath)
        {
            var templateFilePath = GetSolutionRoot() + Path.DirectorySeparatorChar + "build" + Path.DirectorySeparatorChar + "tools" + Path.DirectorySeparatorChar + "flot" + Path.DirectorySeparatorChar + "chart.template.html";

            var content = File.ReadAllText(templateFilePath);

            var seriesPassed = "";
            var seriesFailed = "";
            var seriesThreshold = "";
            for (var i = 0; i < testInfos.Count; i++)
            {
                var info = testInfos[i];
                if(info.Actual > info.Max)
                {
                    seriesFailed += "[" + info.BuildNumber + ", " + info.ActualWeighted + "], ";
                }
                else
                {
                    seriesPassed += "[" + info.BuildNumber + ", " + info.ActualWeighted + "], ";
                }

                seriesThreshold += "[" + info.BuildNumber + ", " + info.Max + "], ";
            }

            content = content.Replace("$$SERIES_PASSED$$", seriesPassed);
            content = content.Replace("$$SERIES_FAILED$$", seriesFailed);
            content = content.Replace("$$SERIES_THRESHOLD$$", seriesThreshold);

            File.WriteAllText(filePath, content);
        }

        private static Color GetPerformanceColor(double fraction)
        {
            if (fraction < 0)
            {
                return Color.Red;
            }


            if (colors == null)
            {
                var bitmap = new Bitmap(101, 1);
                Graphics graphics = Graphics.FromImage(bitmap);

                var rectangle = new Rectangle(0, 0, 101, 1);

                var brush = new LinearGradientBrush(rectangle, Color.Green, Color.Yellow, 0.0f);

                graphics.FillRectangle(brush, rectangle);

                colors = new Color[101];
                for (int i = 0; i < 101; i++)
                {
                    colors[i] = bitmap.GetPixel(i, 0);
                }
            }

            // 25% is the best result GREEN, less or greater than goes to yellow
            double localValue;
            if (fraction >= 0.25)
            {
                localValue = Math.Min(1, (fraction - 0.25) / 0.75);
            }
            else
            {
                localValue = Math.Max(0, (0.25 - fraction)/0.25);
            }

            return colors[(int)(localValue * 100.0)];
        }

        public static void AssertLogMessageIsGenerated(Action action, string message)
        {
            var memoryAppender = new MemoryAppender();
            BasicConfigurator.Configure(memoryAppender);
            LogHelper.SetLoggingLevel(Level.All);
            
            action();

            IEnumerable<string> renderedMessages = memoryAppender.GetEvents().Select(le => le.RenderedMessage).ToList();
            if (!renderedMessages.Any(s => s == message))
            {
                Assert.Fail(String.Format("Message \"{0}\" not found in messages of log4net", message));
            }

            memoryAppender.Close();
            LogHelper.ResetLogging();
        }

        #region Nested type: TestRunInfo

        internal class TestRunInfo
        {
            public string Time;
            public string TestName;
            public float Actual; // millis
            public float ActualWeighted; // millis
            public float Max; // millis
            public int BuildNumber;
            public float MachineHddRank;
            public float MachineRank;
            public bool UseMachineHddRank;
        }

        #endregion

        /// <summary>
        /// Checks if propertychanged is fired expectedCallCount times when action is exectuted
        /// </summary>
        /// <param name="npc"></param>
        /// <param name="expectedCallCount"></param>
        /// <param name="action"></param>
        public static void AssertPropertyChangedIsFired(INotifyPropertyChanged npc, int expectedCallCount, Action action)
        {
            int callCount = 0;
            //create a local delegate so when can unsubscribe
            PropertyChangedEventHandler npcOnPropertyChanged = (s, e) => { callCount++; };
            npc.PropertyChanged += npcOnPropertyChanged;
            action();

            Assert.AreEqual(expectedCallCount,callCount);
            //clean up 
            npc.PropertyChanged -= npcOnPropertyChanged;
        }

        private static bool suppressionInitialized;

        public static void SuppressUIForUnhandledExceptions()
        {
            if (suppressionInitialized)
            {
                return; //call once
            }

            suppressionInitialized = true;

            //unregister any previous calls
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            
            Application.ThreadException -= ApplicationThreadException;
            Application.ThreadException += ApplicationThreadException;
            
            AppDomain.CurrentDomain.UnhandledException -= CurrentDomainUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;
        }

        static void ApplicationThreadException(object sender, ThreadExceptionEventArgs e)
        {
            ThrowExceptionOnCallingThread(e.Exception);
        }

        static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ThrowExceptionOnCallingThread(new Exception(e.ExceptionObject.ToString()));
        }

        static void ThrowExceptionOnCallingThread(Exception innerException)
        {
            Debug.WriteLine(innerException.ToString());
            Thread.CurrentThread.Interrupt();
        }
    }
}