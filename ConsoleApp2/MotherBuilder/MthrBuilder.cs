///////////////////////////////////////////////////////////////////////
// MthrBuilder.cs - mother builder                                   //
// ver 1.0                                                           //
// Author:Dinesh kumar Dhamotharan          Source:Prof.Jim Fawcett  //
// Mail: ddhamoth@syr.edu                                            //
// SUID: 586563818                                                   //
//CSE681 - Software Modeling and Analysis, Fall 2017                 //
///////////////////////////////////////////////////////////////////////
/*
 * Started this project with C# Console Project wizard
 * - Added references to:
 *   - System.ServiceModel
 *   - System.Runtime.Serialization
 *   - System.Threading;
 *   - System.IO;
 *   - MessagePassingComm
* Package Operations:
 * -------------------
 * This package defines one class:
 * - MtherBuilder which implements the public methods:
 *   -------------------------------------------
createProcess -creating the child builder processes according to the user input

startMotherBuilder -starts the comm obj for the mother builder and starts listener thread for itself

initiateChildProcess -helper funtion for create process method used to create the child processes

loadRequestFiles -loading the xml files from the build storage and converts it into xml strings
                  and these strings will be enqueued in the buildrequest queue BrQ

allocateChild - this method acts as process pooling 
                this thread keeps on checking the buildrequest queue and child process ready queue
                if build requests are there in BrQ, it enqueues the first request and sends it to 
                the first process in the ready queue

listen - This threads listens for the message from any receiver 
         when the "processready" message received from the child processes, that child process
         will be enqueued in the ready queue

loadRequestFiles - loading the xml files and will be enqueued in the buildrequest queue BrQ
sendFileRequestToRepo-sending file request to repo
 * . 
* . 
* Required Files:
* TestUtilities.cs, MPCommServices.cs,IMPCommService.cs.
* 
* Maintenance History: 
* -------------------- 
* ver 1.0 : 06 Dec 2017 
* - first release 
*  
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.ServiceModel.Channels;
using System.Runtime.InteropServices;
using MessagePassingComm;

namespace MotherBuilder
{

    public class MthrBuilder
    {

        private static Comm comm { get; set; } = null;
        private static Thread listenTrd { get; set; } = null;
        private static Thread buildTrd { get; set; } = null;
        private static Thread quitTrd { get; set; } = null;
        public static SWTools.BlockingQueue<string> BrQ { get; set; } = null;
        public static SWTools.BlockingQueue<int> ReadyQ { get; set; } = null;
        private static bool isQuitMessage { get; set; } = false;
        private static int totalProcess { get; set; }
        private static int qprocess { get; set; } = 0;
        private static int initialport = 8081;
        public MthrBuilder()
        {
            if (!Directory.Exists(BuildEnvironment.fileStorage))
                Directory.CreateDirectory(BuildEnvironment.fileStorage);
            if (BrQ == null)
                BrQ = new SWTools.BlockingQueue<string>();
            if (ReadyQ == null)
                ReadyQ = new SWTools.BlockingQueue<int>();
            comm = new Comm("http://localhost", 8080);
            
        }

        /* creating the child builder processes according to the user input*/
        static bool createProcess(int i)
        {
            
            Process proc = new Process();
            string fileName = "..\\..\\..\\Buider\\bin\\debug\\Builder.exe";
            Console.WriteLine("\n        =================================================");
            Console.WriteLine("\n            Creating child process with port as {0}",i);
            Console.WriteLine("\n        =================================================");
            string absFileSpec = Path.GetFullPath(fileName);

            Console.Write("\n  attempting to start {0}", absFileSpec);
            string commandline = i.ToString();
            try
            {
                Process.Start(fileName, commandline);
            }
            catch (Exception ex)
            {
                Console.Write("\n  {0}", ex.Message);
                return false;
            }
            return true;
        }

        /*starts the comm obj for the mother builder and starts listener thread for itself*/
        static void Main(string[] args)
        {
            Console.Title = "Mother Builder";
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.Write("\n Mother Builder Process Starts");
            Console.Write("\n ==============================");
            BuildEnvironment.verbose = true;
                MthrBuilder builder = new MthrBuilder();
            //totalProcess = Convert.ToInt32(args[0]);
                ClientEnvironment.verbose = true;
                if (comm == null)
                {
                    comm = new Comm("http://localhost", 8080);
                }
                listenTrd = new Thread(listen);
                listenTrd.Start();
                buildTrd = new Thread(allocateChild);
                buildTrd.Start();  
                Console.ReadLine();
            Console.Write("\n  Press key to exit");
            Console.ReadKey();
            Console.Write("\n  ");
        }

        static void quitproc()
        {
            Console.WriteLine("totalprocess---"+totalProcess);
            while (true)
            {
                if (totalProcess==qprocess)
                {
                    try
                    {
                        Console.WriteLine("qprocess---"+qprocess);
                        Console.WriteLine("\n------------------------------------");
                        Console.WriteLine("\n--Killing Mother Builder--");
                        comm.closeConnection();
                        foreach (Process proc in Process.GetProcessesByName("MotherBuilder"))
                        {
                            proc.Kill();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }
        /*loading the xml files from the build storage and converts it into xml strings
         and these strings will be enqueued in the buildrequest queue BrQ*/
        public static void loadRequestFiles(string fnames)
        {
            string[] filenames = fnames.Split(' ');
            filenames = filenames.Where(arr => !String.IsNullOrEmpty(arr)).ToArray();
            Console.WriteLine("---"+filenames.Count());
            Console.WriteLine("\n        ==================================================");
            Console.WriteLine("\n         Loading xml test requests from the build storage");
            Console.WriteLine("\n        ==================================================");
            Console.WriteLine(fnames);
            List <string> names = new List<string>();
        foreach(string fil in filenames)
            {
                Console.WriteLine(fil);
                string[] files =Directory.GetFiles(BuildEnvironment.fileStorage, fil);
                Console.WriteLine(files[0]);
                
                var xmlString = File.ReadAllText(files[0]);
                Console.WriteLine(xmlString);
                BrQ.enQ(xmlString);
        }
        }
        /*helper funtion for create process method used to create the child processes*/
        public static void initiateChildProcess(string pcount)
        {
            isQuitMessage = false;
            int count = Int32.Parse(pcount);
            
            for (int i = 0; i < count; ++i)
            {
                if (createProcess(initialport))
                {
                    Console.Write(" - succeeded");
                }
                else
                {
                    Console.Write(" - failed");
                }
                initialport = initialport + 1;
            }
        }
        /*this method acts as process pooling 
         this thread keeps on checking the buildrequest queue and child process ready queue
         if build requests are there in BrQ, it enqueues the first request and sends it to 
         the first process in the ready queue*/
        static void allocateChild()
        {
            Console.WriteLine("\n        =================================================");
            Console.WriteLine("\n                    Process Pooling started");
            Console.WriteLine("\n        =================================================");
            while (true)
            {
                if(ReadyQ.size()!=0&&isQuitMessage)
                {
                    CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
                    sndMsg.author = "Dinesh Dhamotharan";
                    sndMsg.to = "http://localhost:" + ReadyQ.deQ() + "/IPluggableComm";
                    sndMsg.from = "http://localhost:8080/IMessagePassingComm";
                        sndMsg.body = "Quit"; sndMsg.command = "Quit";
                    sndMsg.port = 8080;
                    comm.postMessage(sndMsg);
                    Console.WriteLine("\n sending message to child:{0}", sndMsg.body);
                }
                if (BrQ.size() != 0)
                {
                    if (ReadyQ.size() != 0)
                    {
                        CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
                        sndMsg.command = "request";
                        sndMsg.author = "Dinesh Dhamotharan";
                        sndMsg.to = "http://localhost:" + ReadyQ.deQ() + "/IPluggableComm";
                        sndMsg.from = "http://localhost:8080/IMessagePassingComm";
                            sndMsg.body = BrQ.deQ(); sndMsg.command = "request";
                        sndMsg.port = 8080;
                        Console.WriteLine("\n reached here");
                        comm.postMessage(sndMsg);
                        Console.WriteLine("\n sending message to child:{0}", sndMsg.body);
                    }
                }
            }
        }
        /*This threads listens for the message from any receiver 
         when the "processready" message received from the child processes, that child process
         will be enqueued in the ready queue*/
         static void listen()
        {
            Console.WriteLine("\n ---------------------------------");
            Console.WriteLine("\n Starting Mother Builder Listener");
            Console.WriteLine("\n ---------------------------------");
            while (true)
            {
                CommMessage crcvMsg = null;
                crcvMsg = comm.getMessage();
                if (crcvMsg.body != null)
                {
                    if (BuildEnvironment.verbose)
                        crcvMsg.show(); 
                    if (crcvMsg.body.Equals("processReady"))
                    {
                        Console.WriteLine("\n ----------------------------------------------------------");
                        Console.WriteLine("\n Received a Process Ready message from child process {0} and enqueued in the ready queue", crcvMsg.port);
                        Console.WriteLine("\n ----------------------------------------------------------");
                        ReadyQ.enQ(crcvMsg.port); 
                    }
                    if (crcvMsg.body.Equals("Exit"))
                    {
                        Console.WriteLine("\n Received an Exit message from child process {0} ", crcvMsg.port);
                        qprocess = qprocess + 1;
                    }
                    if (crcvMsg.port == 8074&& crcvMsg.command.Equals("xmlfilenames")  )
                    {
                        sendFileRequestToRepo(crcvMsg.body);    
                    }
                    if (crcvMsg.port == 8074 && crcvMsg.command.Equals("childno"))
                    {
                        totalProcess = Convert.ToInt32(crcvMsg.body);
                        initiateChildProcess(crcvMsg.body);
                    }
                    if (crcvMsg.port == 8074 && crcvMsg.command.Equals("Quit"))
                    {
                        isQuitMessage = true;
                    }
                    if (crcvMsg.port == 8095 && crcvMsg.command.Equals("TransferCompleted"))
                    {
                        loadRequestFiles(crcvMsg.body);
                    }
                }
            }
        }
        //sending file request to repo
        private static void sendFileRequestToRepo(string body)
        {
            Console.WriteLine("\n            Sending connection request to the repository");
            Console.WriteLine("\n        =================================================");
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.connect);
            sndMsg.command = "confrombuilder";
            sndMsg.author = "Dinesh Dhamotharan";
            sndMsg.to = "http://localhost:8095/IPluggableComm";
            sndMsg.from = "http://localhost:8080/IMessagePassingComm";
            sndMsg.body = "8080";
            sndMsg.port = 8080;
            comm.postMessage(sndMsg);
            Console.WriteLine("\n        =================================================");
            Console.WriteLine("\n             Sending file request to the repository");
            Console.WriteLine("\n        =================================================");
            CommMessage csndMsg = new CommMessage(CommMessage.MessageType.request);
            csndMsg.command = "reqfrombuilder";
            csndMsg.author = "Dinesh Dhamotharan";
            csndMsg.to = "http://localhost:8095/IPluggableComm";
            csndMsg.from = "http://localhost:8080/IMessagePassingComm";
            csndMsg.body = body;
            csndMsg.port = 8080;
            comm.postMessage(csndMsg);
        }
    }
}

  







