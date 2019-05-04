///////////////////////////////////////////////////////////////////////////
// Builder.cs - Deserialize the build request                            //
//                make build and store it in the temp dir.               //
//                create build logs and sends it to repository           //
// Ver 1.2                                                               //
// Author:Dinesh kumar Dhamotharan          Source:Prof.Jim Fawcett      //
// Mail: ddhamoth@syr.edu                                                //
// SUID: 586563818                                                       //
//CSE681 - Software Modeling and Analysis, Fall 2017                     //
///////////////////////////////////////////////////////////////////////////
/* Package Operations:
 * -------------------
 * This package defines one class:
 * - Builder which implements the public methods:
 *   -------------------------------------------
processRequest -deserialize the test request and starts the build process

deletefiles - deleting files in the temp storage after finishing all process

buildProcess - dll files will be generated for each of the tests in a test request

listen -This thread keeps on listening for the messages
         If a request message comes from the mother builder, it completes all the build process and 
         sends a ready message to mother builder for enqueueing into the ready queue

sendFiles-this method On request, sending files to the mother builder temproary storage

loadFilesToBuild-loading files from the repository to the temparory directory

notifyclient-send notification to client

createbuildlog-creating build log files and sending to repo

getlogList-getting file names and actual directory of the files for posting the files using wcf

sendQuitMessage-sending exit acknowledgement to mother builder

sendReadyMessage-sending ready message to mother builder

sendDllFiles-this method On request, sending files to the mother builder temproary storage

* Required Files:
* TestUtilities.cs, MPCommServices.cs,IMPCommService.cs. serialization.cs
* 
* Maintenance History: 
* -------------------- 
* ver 1.0 : 06 Dec 2017 
* - first release 
*
 */
using Federation;
using MessagePassingComm;
using MockTestHarness;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utilities;

namespace Buider
{
    public class Builder
    {
        private List<string> fname { get; set; } = new List<string>();
        private List<string> fnames { get; set; } = new List<string>();
        private static Comm comm { get; set; } = null;
        private static string port { get; set;} = "";
        private static string files { get; set; } = "";
        private static string allfiles { get; set; } = "";
        private static string receivePath { get; set; } = "";
        private static string request { get; set; } = "";
        public Builder(string portno)
        {
            
            if (receivePath == "")
            {
                
                if (!Directory.Exists("../../../Buider/buildStorage"+portno))
                    Directory.CreateDirectory("../../../Buider/buildStorage"+portno);
                    receivePath = receivePath + "../../../Buider/buildStorage"+portno;
            }
            else
            {
                if (!Directory.Exists(receivePath))
                    Directory.CreateDirectory(receivePath);
            }
        }

        /*----<deserialize the test request and starts the build process>--------- */
        public bool processRequest(string request)
        {
            TestRequest newRequest = request.FromXml<TestRequest>();
            string typeName = newRequest.GetType().Name;
                Console.WriteLine("          ====================================");
              Console.WriteLine("\n             Parsing the test request");
                Console.WriteLine("          ====================================");
            Console.Write("\n\nDeserializing xml string results in type: {0}", typeName);
            Console.Write(newRequest);
            Console.WriteLine("\n**********************************************");
            Console.WriteLine();
              
            if (newRequest.tests.Count()>0)
            {
                foreach (TestElement test in newRequest.tests)
                {
                    if(test.testName!=null)
                    {
                        files = files + test.testName+" ";
                    }
                    if (test.testDriver != null)
                    {
                        files = files + test.testDriver;
                        allfiles = allfiles + test.testDriver;
                        fname.Add(test.testDriver);
                    }
                    if (test.testCodes != null)
                    {
                        foreach (string testcode in test.testCodes)
                        {
                            files = files + " " + testcode + " ";
                            allfiles = allfiles + " " + testcode + " ";
                            fname.Add(testcode);
                        }
                        fnames.Add(files);
                        files = "";
                    }
                }
                       loadFilesToBuild(allfiles);
                       allfiles = "";
                       return true;       
            }
            else
            {
                Console.WriteLine("\n\nNo test cases found");
                Console.WriteLine("\n**********************************************");
                return false;
            }
        }

        /*----< deleting files in the temp storage after finishing all process >--------- */
        private void deletefiles()
        {
            Console.WriteLine("\n        =================================================");
            Console.WriteLine("\n            Files in the temporary storage deleted");
            Console.WriteLine("\n        =================================================");

            //string[] files = Directory.GetFiles(repo.receivePath);
            //foreach (string file in files)
            //{
            //    try
             //   {
             //       File.Delete(file);
             //   }
             //   catch (Exception ex)
              //  {
              //       Console.WriteLine(ex.Message);
              //  }
            // }
        }

        /*----< loading files from the repository to the temparory directory>--------- */
        private void loadFilesToBuild(string allfiles)
        {
            Console.WriteLine("\n             Loading files from repository to temporary build storage");
            Console.WriteLine("          =================================================================");
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.connect);
            sndMsg.command = "confromCbuilder";
            sndMsg.author = "Dinesh Dhamotharan";
            sndMsg.to = "http://localhost:8095/IPluggableComm";
            sndMsg.from = "http://localhost:"+port+"/IMessagePassingComm";
            sndMsg.body = "c2rconnect";
            sndMsg.port = Convert.ToInt32(port);
            comm.postMessage(sndMsg);
            Console.WriteLine("\n             Sending file request to the repository");
            Console.WriteLine("\n        =================================================");
            CommMessage csndMsg = new CommMessage(CommMessage.MessageType.request);
            csndMsg.command = "reqfromCbuilder";
            csndMsg.author = "Dinesh Dhamotharan";
            csndMsg.to = "http://localhost:8095/IPluggableComm";
            csndMsg.from = "http://localhost:"+port+"/IMessagePassingComm";
            csndMsg.body = receivePath+","+allfiles;
            csndMsg.port = Convert.ToInt32(port);
            comm.postMessage(csndMsg);
            Console.WriteLine("\n**********************************************");
            
        }


        /*----< dll files will be generated for each of the tests in a test request>--------- */
        private bool buildProcess(List<string> filesnames)
        {
            try
            {   string buildlog = "";
                string totaltests = "";
                foreach (string file in filesnames)
                {
                    int index = file.IndexOf(' ');
                    if (index != -1)
                    {
                        string tname = file.Substring(0, index);
                        string files = file.Substring(index+1);
                        Process p = new Process();
                        p.StartInfo.FileName = "cmd.exe";
                        Console.WriteLine("\n             Building test library");
                        Console.WriteLine("          ===========================");
                        p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        p.StartInfo.Arguments = "/Ccsc /target:library /out:"+tname+".dll " + files;
                        p.StartInfo.WorkingDirectory = @receivePath;
                        p.StartInfo.RedirectStandardError = true;
                        p.StartInfo.RedirectStandardOutput = true;
                        p.StartInfo.UseShellExecute = false;
                        p.Start();
                        p.WaitForExit();
                        string errors = p.StandardError.ReadToEnd();
                        string output = p.StandardOutput.ReadToEnd();
                        Console.WriteLine(errors);
                        Console.WriteLine(output);
                        buildlog += output + errors + "\n";
                        if (output.Contains("error"))
                        {   Console.WriteLine("Build failed");
                            notifyclient("Build_Failed_for_"+ tname);}
                        else
                        {  Console.WriteLine("Build succeed");
                            totaltests = totaltests + tname + ".dll " + " ";}
                        Console.WriteLine(" ----------------------------------------------");
                    }
                }
                request = totaltests;
                createbuildlog(buildlog);
                return true;
            }catch (Exception ex)
            {
                Console.WriteLine( ex.Message);
                notifyclient(ex.Message);
                return false;
            }
        }
        //send notification to client
        private void notifyclient(string body)
        {
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "notification";
            sndMsg.author = "Dinesh Dhamotharan";
            sndMsg.to = "http://localhost:8074/IPluggableComm";
            sndMsg.from = "http://localhost:"+port+"/IMessagePassingComm";
            sndMsg.body = body;
            sndMsg.port = 8077;
            comm.postMessage(sndMsg);
        }
        //creating build log files and sending to repo
        static void createbuildlog(string buildlog)
        {
            if (buildlog != "")
            {
                StreamWriter stream = new StreamWriter(receivePath+"/Buildlog" + DateTime.Now.ToString("MMddyyyyHHmmssfff") + ".txt");
                stream.WriteLine(buildlog);
                stream.Close();
                Console.WriteLine("\n            sending buildlogs to the Repository");
                Console.WriteLine("\n        =================================================");
                CommMessage sndMsg = new CommMessage(CommMessage.MessageType.connect);
                sndMsg.command = "show";
                sndMsg.author = "Dinesh Dhamotharan";
                sndMsg.to = "http://localhost:8095/IPluggableComm";
                sndMsg.from = "http://localhost:" + port + "/IMessagePassingComm";
                sndMsg.body = "logtransferstarted";
                comm.postMessage(sndMsg);
                List<string> names = getlogList();
                foreach (string file in names)
                {
                    string fileSpec = file;
                    string fileName = Path.GetFileName(fileSpec);
                    Console.Write("\n sending \"{0}\" to \"{1}\"", fileName, RepoEnvironment.fileStorage);
                    TestUtilities.putLine(string.Format("transferring file \"{0}\"", file));
                    bool transferSuccess = comm.postFile(file, "cb2r", receivePath);
                    TestUtilities.checkResult(transferSuccess, "transfer");
                }
                Console.WriteLine("\n            Build logs captured and sent to repository");
                Console.WriteLine("\n        =================================================");
                sndMsg = new CommMessage(CommMessage.MessageType.connect);
                sndMsg.command = "logTransferCompleted";
                sndMsg.author = "Dinesh Dhamotharan";
                sndMsg.to = "http://localhost:8095/IPluggableComm";
                sndMsg.from = "http://localhost:" + port + "/IMessagePassingComm";
                sndMsg.body = "logTransferCompleted";
                sndMsg.port = Convert.ToInt32(port);
                comm.postMessage(sndMsg);
            }
        }
        /*getting file names and actual directory of the files for posting the files using wcf*/
        public static List<string> getlogList()
        {
            List<string> names = new List<string>();
            List<string> files = new List<string>();
            string[] list = Directory.GetFiles(receivePath, "*.txt");
            foreach (string file in list)
            {         
                    names.Add(Path.GetFileName(file));
            }
            return names;
        }
        /*----< starting the child builder 
         * sends initial ready message to the mother builder for adding the process to the process pool ready queue>--------- */
        static void Main(string[] args)
        {
            Thread listenTrd = null;
            Console.Title = "Child process";
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.DarkRed;
            TestUtilities.vbtitle("Child Process Started");
            Builder builder = new Builder(args[0]);
            port=args[0];
            comm = new Comm("http://localhost", Convert.ToInt32(port));
            listenTrd = new Thread(listen);
            listenTrd.Start();
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "show";
            sndMsg.author = "Dinesh Dhamotharan";
            sndMsg.to = "http://localhost:8080/IPluggableComm";
            sndMsg.from = "http://localhost:"+ port + "/IMessagePassingComm";
            sndMsg.body = "processReady";
            sndMsg.port = Convert.ToInt32(port);
            Console.WriteLine("\n reached here");
            comm.postMessage(sndMsg);

        }


        /*This thread keeps on listening for the messages
         If a request message comes from the mother builder, it completes all the build process and 
         sends a ready message to mother builder for enqueueing into the ready queue*/
        static void listen()
        {
            Builder builder = new Builder(port);
            while (true)
            {
                ClientEnvironment.verbose = true;
                CommMessage crcvMsg = comm.getMessage();
                if(crcvMsg.body != null)
                { 
                if (ClientEnvironment.verbose)
                    crcvMsg.show();
                    if (crcvMsg.port == 8080&& crcvMsg.command.Equals("request"))
                    {
                        bool result = builder.processRequest(crcvMsg.body);
                    }
                    if (crcvMsg.port == 8080 && crcvMsg.command.Equals("Quit"))
                    {
                        sendQuitMessage();
                        try
                        {
                            Console.WriteLine("\n------------------------------------");
                            Console.WriteLine("\n--Killing child Process--");
                            comm.closeConnection();
                            foreach (Process proc in Process.GetProcessesByName("Builder"))
                            {
                                proc.Kill();
                            }
                        }catch (Exception ex)
                        { Console.WriteLine(ex.Message); }
                    }
                    else if (crcvMsg.port == 8095 && crcvMsg.body != null && crcvMsg.body.Equals("TransferCompleted"))
                    {
                        builder.buildProcess(builder.fnames);
                    }
                    else if (crcvMsg.port == 8095 && crcvMsg.body != null && crcvMsg.body.Equals("logsreceived"))
                    {
                    if(request!=null&&!(request.Equals("")))
                    { 
                        CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
                        sndMsg.command = "testrequest";
                        sndMsg.author = "Dinesh Dhamotharan";
                        sndMsg.to = "http://localhost:8077/IPluggableComm";
                        sndMsg.from = "http://localhost:" + port + "/IMessagePassingComm";
                        sndMsg.body = request;
                        sndMsg.port = Convert.ToInt32(port);
                        comm.postMessage(sndMsg);
                    }
                    }
                    else if(crcvMsg.port == 8077 && crcvMsg.command.Equals("dllrequest"))
                    {
                        sendDllFiles(crcvMsg.port, crcvMsg.body); sendReadyMessage();
                    }
                }  
                Thread.Sleep(1000);
            }
        }
        //sending exit acknowledgement to mother builder
        public static void sendQuitMessage()
        {
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "Exit";
            sndMsg.author = "Dinesh Dhamotharan";
            sndMsg.to = "http://localhost:8080/IPluggableComm";
            sndMsg.from = "http://localhost:" + port + "/IMessagePassingComm";
            sndMsg.body = "Exit";
            sndMsg.port = Convert.ToInt32(port);
            comm.postMessage(sndMsg);
        }
        //sending ready message to mother builder
        public static void sendReadyMessage()
        {
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "show";
            sndMsg.author = "Dinesh Dhamotharan";
            sndMsg.to = "http://localhost:8080/IPluggableComm";
            sndMsg.from = "http://localhost:" + port + "/IMessagePassingComm";
            sndMsg.body = "processReady";
            sndMsg.port = Convert.ToInt32(port);
            comm.postMessage(sndMsg);
        }
            /*this method On request, sending files to the mother builder temproary storage*/
        public static void sendDllFiles(int portno, string Files)
        {
            Console.WriteLine("\n     started sending files to the Test harness local storage");
            Console.WriteLine("\n        =================================================");
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.connect);
            sndMsg.command = "show";
            sndMsg.author = "Dinesh Dhamotharan";
            sndMsg.to = "http://localhost:" + portno + "/IPluggableComm";
            sndMsg.from = "http://localhost:"+ port + "/IMessagePassingComm";
            sndMsg.body = port;
            comm.postMessage(sndMsg);
            List<string> names = getDllFileList(Files);
            foreach (string file in names)
            {
                string fileSpec = file;
                string fileName = Path.GetFileName(fileSpec);
                Console.Write("\n sending \"{0}\" to \"{1}\"", fileName, TestEnvironment.fileStorage);
                TestUtilities.putLine(string.Format("transferring file \"{0}\"", file));
                bool transferSuccess = comm.postFile(file, "cb2th", receivePath);
                TestUtilities.checkResult(transferSuccess, "transfer");
                Thread.Sleep(2000);
            }
            Console.WriteLine("\n                     File transfer completed");
            Console.WriteLine("\n        =================================================");
            sndMsg = new CommMessage(CommMessage.MessageType.connect);
            sndMsg.command = "show";
            sndMsg.author = "Dinesh Dhamotharan";
            sndMsg.to = "http://localhost:" + portno + "/IPluggableComm";
            sndMsg.from = "http://localhost:"+port+"/IMessagePassingComm";
            sndMsg.body = "DllTransferCompleted";
            sndMsg.port = Convert.ToInt32(port);
            comm.postMessage(sndMsg);
            TestUtilities.putLine("last message received\n");
        }

        /*getting file names and actual directory of the files for posting the files using wcf*/
        public static List<string> getDllFileList(string filenames)
        {
            string[] fnames = filenames.Split(' ');
            fnames = fnames.Where(arr => !String.IsNullOrEmpty(arr)).ToArray();
            List<string> names = new List<string>();
            List<string> files = new List<string>();
            foreach (string file in fnames)
            {
                string[] list = Directory.GetFiles(receivePath, file);
                    names.Add(Path.GetFileName(list[0]));
            }
            return names;
        }
    }
}
