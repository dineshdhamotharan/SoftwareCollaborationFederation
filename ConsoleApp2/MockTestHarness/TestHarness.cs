///////////////////////////////////////////////////////////////////////////
// TestHarness.cs - Demonstrate Robust loading and dynamic invocation of //
//                Dynamic Link Libraries found in specified location     //
//                send notification to client and logs to repo           //
//                                                                       //
// Author:Dinesh kumar Dhamotharan          Source:Prof.Jim Fawcett      //
// Mail: ddhamoth@syr.edu                                                //
// SUID: 586563818                                                       //
//CSE681 - Software Modeling and Analysis, Fall 2017                     //
///////////////////////////////////////////////////////////////////////////
/* 
* Package Operations: 
* =================== 
* This test harness used to test all the test libraries for the requested test requests.
* it gets files from child builder and tests libraries
* Public Interface 
* ---------------- 
LoadFromComponentLibFolder-loading dll files from location
runSimulatedTest-run tester t from assembly asm
notifyclient-send notification to client 
allocateprocess-dequeueing process queue and start testing first test request
listen thread ussed to listen for messages
processlisten-start testing first dll
sendTestlog-sending test logs to repo
getlogList-taking logs from local location
deletefiles-deleting dll and logs from temp stroage
* . 
* . 
* Required Files:
* TestUtilities.cs, MPCommServices.cs,IMPCommService.cs serialization.cs
* 
* Maintenance History: 
* -------------------- 
* ver 1.0 : 06 Dec 2017 
* - first release 
*  
*/
using MessagePassingComm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MockTestHarness
{
    public class TestHarness
    {
        private static Thread TestTrd { get; set; } = null;
        private static Thread ProcessTrd { get; set; } = null;
        public static SWTools.BlockingQueue<string> TrQ { get; set; } = null;
        private StreamWriter _LogBuilder;
        private static Comm comm { get; set; } = null;
        public string Log { get { return _LogBuilder.ToString(); } }
        private static string testersLocation { get; set; } = "../../../MockTestHarness/TestStorage";
        private static bool isHarnessAvailable { get; set; } = true;
        private static bool isDllReceived { get; set; } = false;

        /*----< library binding error event handler >------------------*/
        /*
         *  This function is an event handler for binding errors when
         *  loading libraries.  These occur when a loaded library has
         *  dependent libraries that are not located in the directory
         *  where the Executable is running.
         */
        public TestHarness()
        {
            if (TrQ == null)
                TrQ = new SWTools.BlockingQueue<string>();
            if (!Directory.Exists(testersLocation))
                Directory.CreateDirectory(testersLocation);
            
            
        }
        //loading dll files from location
        static Assembly LoadFromComponentLibFolder(object sender, ResolveEventArgs args)
        {
            Console.Write("\n  called binding error event handler");
            string folderPath = testersLocation;
            string assemblyPath = Path.Combine(folderPath, new AssemblyName(args.Name).Name + ".dll");
            if (!File.Exists(assemblyPath)) return null;
            Assembly assembly = Assembly.LoadFrom(assemblyPath);
            return assembly;
        }
        //----< load assemblies from testersLocation and run their tests >-----

        string loadAndExerciseTesters()
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyResolve += new ResolveEventHandler(LoadFromComponentLibFolder);
            try
            {
                TestHarness loader = new TestHarness();
                // load each assembly found in testersLocation
                string[] files = Directory.GetFiles(testersLocation, "*.dll");
                if (files.Count() > 0)
                {
                    foreach (string file in files)
                    {
                        Assembly asm = Assembly.Load(File.ReadAllBytes(file));
                        string fileName = Path.GetFileName(file);
                        Console.Write("\n  loaded {0}", fileName);
                        notifyclient("Loaded "+ fileName);
                        Type[] types = asm.GetTypes();
                        foreach (Type t in types)
                        {
                            if (t.GetInterface("Test.IDriver", true) != null)
                                if (!loader.runSimulatedTest(t, asm))
                                {
                                    Console.Write("\n  test {0} failed to run", t.ToString());
                                    notifyclient("Test "+ t.ToString()+"failed to run");
                                }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("\nNo test libraries generated for testing\n");
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            return "Simulated Testing completed";
            
        }
        //
        //----< run tester t from assembly asm >-------------------------------
        bool runSimulatedTest(Type t, Assembly asm)
        {
            FileStream fs = new FileStream("../../../MockTestHarness/TestStorage/testlog" + DateTime.Now.ToString("MMddyyyyHHmmssfff") + ".txt", FileMode.Create);
            _LogBuilder = new StreamWriter(fs);
            // save the original output stream for Console
            TextWriter _old = Console.Out;
            // flush whatever was (if anything) in the log builder
            _LogBuilder.Flush();
            try
            {
                Console.Write(
                  "\n  attempting to create instance of {0}", t.ToString()
                  );
                object obj = asm.CreateInstance(t.ToString());
                MethodInfo method = t.GetMethod("display");
                if (method != null)
                    method.Invoke(obj, new object[0]);
                bool status = false;
                method = t.GetMethod("test");
                Console.SetOut(_LogBuilder);
                if (method != null)
                status = (bool)method.Invoke(obj, new object[0]);
                Func<bool, string> act = (bool pass) =>
                {
                    if (pass)
                        return "passed";
                    return "failed";
                };
                Console.Write("\n  test {0}", act(status));
                notifyclient(t.ToString()+"-"+"test_"+ act(status));
                Console.WriteLine("\n------------------------------------------------");
                Console.SetOut(_old);
               _LogBuilder.Close();
            }
            catch (Exception ex)
            {
                Console.Write("\n  test failed with message \"{0}\"", ex.Message);
                notifyclient(t.ToString() + "-" + "test_failed_with_message_" + ex.Message);
                Console.SetOut(_old);
                return false;
            }
            return true;
        }
       
        //----< send notification to client ---------
        private void notifyclient(string body)
        {
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "notification";
            sndMsg.author = "Dinesh Dhamotharan";
            sndMsg.to = "http://localhost:8074/IPluggableComm";
            sndMsg.from = "http://localhost:8077/IMessagePassingComm";
            sndMsg.body = body;
            sndMsg.port = 8077;
            comm.postMessage(sndMsg);
        }
        
        //----< run demonstration >--------------------------------------------

        [STAThread]
        static void Main(string[] args)
        {
            Console.Title = "TestHarness";
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Thread listenTrd = null;
            Thread PlistenTrd = null;
            Console.Write("\n  Demonstrating Robust Test Loader");
            Console.Write("\n ==================================\n");

            TestHarness loader = new TestHarness();  
            comm = new Comm("http://localhost", 8077);
            listenTrd = new Thread(listen);
            listenTrd.Start();
            PlistenTrd = new Thread(processlisten);
            PlistenTrd.IsBackground = true;
            PlistenTrd.Start();
            TestTrd = new Thread(allocateprocess);
            TestTrd.Start();
            TestHarness.testersLocation = Path.GetFullPath(TestHarness.testersLocation);
           
        }
        //dequeueing process queue and start testing first test request
        static void allocateprocess()
        {
            Console.WriteLine("\n                    Test harness waiting for test requests");
            Console.WriteLine("\n        =================================================");
            while (true)
            {
                if (TrQ.size() > 0)
                {
                    if(isHarnessAvailable){ string request = TrQ.deQ();
                    int index = request.IndexOf(' ');
                        if (index != -1)
                        {   
                            string portno = request.Substring(0, index);
                            string files = request.Substring(index + 1);
                            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
                            sndMsg.command = "dllrequest";
                            sndMsg.author = "Dinesh Dhamotharan";
                            sndMsg.to = "http://localhost:" + portno + "/IPluggableComm";
                            sndMsg.from = "http://localhost:8077/IMessagePassingComm";
                            sndMsg.body = files;
                            sndMsg.port = 8077;
                            Console.WriteLine("\n reached here");
                            comm.postMessage(sndMsg);
                            Console.WriteLine("\n sending message:" + files + "to child:" + portno);
                        }
                    }
                }
            }
        }
        //listen thread ussed to listen for messages
        static void listen()
        {
            TestHarness loader = new TestHarness();
            while (true)
            { 
                ClientEnvironment.verbose = true;
                CommMessage crcvMsg = comm.getMessage();
                if (crcvMsg.body != null&& crcvMsg.command!=null)
                {
                    if (ClientEnvironment.verbose)
                        crcvMsg.show();
                    if (crcvMsg.command.Equals("testrequest"))
                    {
                        Console.WriteLine("\n Received Test Request Message from child process {0} and enqueued in the test request queue", crcvMsg.port);
                        Console.WriteLine("\n ----------------------------------------------------------");
                        TrQ.enQ(crcvMsg.port + " " + crcvMsg.body);
                    }
                    else if (crcvMsg.body != null && crcvMsg.body.Equals("DllTransferCompleted"))
                    {
                        isDllReceived = true;                      
                    }
                    else if (crcvMsg.port == 8095 && crcvMsg.body != null && crcvMsg.body.Equals("logsreceived"))
                    {
                        deletefiles();
                        isHarnessAvailable = true;
                    } 
                    }        
                Thread.Sleep(1000);
            }
        }
        // start testing first dll
        static void processlisten()
        {
            TestHarness loader = new TestHarness();
            while (true)
            {
                if (isDllReceived)
                {
                    isHarnessAvailable = false;
                    TestHarness.testersLocation = Path.GetFullPath(TestHarness.testersLocation);
                    Console.Write("\n  Loading Test Modules from:\n    {0}\n", TestHarness.testersLocation);
                    // run load and tests
                    string result = loader.loadAndExerciseTesters();
                    if (result.Equals("Simulated Testing completed"))
                        sendTestlog();
                    isDllReceived = false;
                }
                Thread.Sleep(1000);
            }
        }
        //sending test logs to repo
        static void sendTestlog()
        {
                
                Console.WriteLine("\n        =================================================");
                Console.WriteLine("\n            sending Testlogs to the Repository");
                Console.WriteLine("\n        =================================================");
                CommMessage sndMsg = new CommMessage(CommMessage.MessageType.connect);
                sndMsg.command = "show";
                sndMsg.author = "Dinesh Dhamotharan";
                sndMsg.to = "http://localhost:8095/IPluggableComm";
                sndMsg.from = "http://localhost:8077/IMessagePassingComm";
                sndMsg.body = "logtransferstarted";
                comm.postMessage(sndMsg);
                List<string> names = getlogList();
                foreach (string file in names)
                {
                    string fileSpec = file;
                    string fileName = Path.GetFileName(fileSpec);
                    Console.Write("\n sending \"{0}\" to \"{1}\"", fileName, RepoEnvironment.fileStorage);
                    TestUtilities.putLine(string.Format("transferring file \"{0}\"", file));
                bool transferSuccess = comm.postFile(file, "th2r", TestEnvironment.fileStorage);
                TestUtilities.checkResult(transferSuccess, "transfer");
                Thread.Sleep(2000);
            }
                Console.WriteLine("\n        =================================================");
                Console.WriteLine("\n                   Test logs sent to repository");
                Console.WriteLine("\n        =================================================");
                sndMsg = new CommMessage(CommMessage.MessageType.connect);
                sndMsg.command = "logTransferCompleted";
                sndMsg.author = "Dinesh Dhamotharan";
                sndMsg.to = "http://localhost:8095/IPluggableComm";
                sndMsg.from = "http://localhost:8077/IMessagePassingComm";
                sndMsg.body = "logTransferCompleted";
                sndMsg.port = 8077;
                comm.postMessage(sndMsg);
            
        }
        //taking logs from local location
        public static List<string> getlogList()
        {
            List<string> names = new List<string>();
            List<string> files = new List<string>();
            string[] list = Directory.GetFiles(TestEnvironment.fileStorage, "*.txt");
            foreach (string file in list)
            {
                names.Add(Path.GetFileName(file));
            }
            return names;
        }
        //deleting dll and logs from temp stroage
        private static void deletefiles()
        {
            Console.WriteLine("\n            Files in the temporary storage deleted");
            Console.WriteLine("\n        =================================================");

            string[] logfiles = Directory.GetFiles(TestEnvironment.fileStorage,"*.txt");
            foreach (string file in logfiles)
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            string[] dllfiles = Directory.GetFiles(TestEnvironment.fileStorage, "*.dll");
            foreach (string file in dllfiles)
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

    }
}
