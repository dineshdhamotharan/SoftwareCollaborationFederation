///////////////////////////////////////////////////////////////////////////
// MockRepo.cs - transfer files using WCF.                               //
//                listens to the incoming messages for file transfer     // 
//                and sends files to the requested process location      //  
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


/* 
* Package Operations: 
* =================== 
* This Client GUI acts as the interface between all the components such as
* repository, build server and test harness. This allows user to browse through
* remote repo files such as adding files,viewing the contents of the files, deleting files
* and also it allows user to create and process test requests.
* 
* Public Interface 
* ---------------- 
* sendFiles -this method On request, sending files to the child builder temproary storage
sendFilenames- sending file names to client
sendContent- send file contents to client
saveXmlFiles-getting the xml string and storing it as files
deleteFiles-deleting the files in repo
* . getClientFileList -getting file names and actual directory of the files for posting the files using wcf

listen -this thread listen for the incoming message from any of the process
         when the mother process sends a file transfer request, it sends the 
         requested files to the build storage location
* . 
* . 
* Required Files:
* TestUtilities.cs, MPCommServices.cs,IMPCommService.cs
* 
* Maintenance History: 
* -------------------- 
* ver 1.0 : 06 Dec 2017 
* - first release 
*  
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MessagePassingComm;
using System.Threading;

namespace Federation
{
    


    public class MockRepoTest
    {   
        private static Comm comm { get; set; } = null;

        public void repoThread()
        {
            RepoEnvironment.verbose = true;
            TestUtilities.vbtitle("Testing Repo", '=');
            Thread listenTrd = null;
            comm = new Comm("http://localhost", 8095);
            listenTrd = new Thread(listen);
            listenTrd.Start();
        }
        /*this thread listen for the incoming message from any of the process
         when the mother process sends a file transfer request, it sends the 
         requested files to the build storage location*/
        static void listen()
        {
            Console.WriteLine("\n        =================================================");
            Console.WriteLine("\n        Repository Listener Started Listening for messages");
            Console.WriteLine("\n        =================================================");
            while (true)
            {
                CommMessage crcvMsg = null;
                crcvMsg = comm.getMessage();
                if (RepoEnvironment.verbose)
                    crcvMsg.show();
                if (crcvMsg.port !=0 && (crcvMsg.command.Equals("reqfromCbuilder")|| crcvMsg.command.Equals("reqfrombuilder")))
                {                           
                 sendFiles(crcvMsg.port,crcvMsg.body, crcvMsg.command);   
                }
                if (crcvMsg.port != 0 && (crcvMsg.command.Equals("deleterequest") || crcvMsg.command.Equals("deletefiles")))
                {
                    deleteFiles(crcvMsg.port, crcvMsg.body, crcvMsg.command);
                }
                if (crcvMsg.port != 0 && (crcvMsg.command.Equals("csfilenamesreq")|| crcvMsg.command.Equals("xmlfilenamesreq")))
                {
                 sendFilenames(crcvMsg.port, crcvMsg.command);
                }
                if (crcvMsg.port != 0 && crcvMsg.command.Equals("ContentReq"))
                {
                    sendContent(crcvMsg.port, crcvMsg.body);
                }
                if (crcvMsg.port != 0 && crcvMsg.command.Equals("xmlstring"))
                {
                    saveXmlFiles(crcvMsg.port, crcvMsg.body);
                }
                if (crcvMsg.port != 0 && crcvMsg.command.Equals("logTransferCompleted"))
                {
                    CommMessage sndMsg = new CommMessage(CommMessage.MessageType.connect);
                    sndMsg.command = "show";
                    sndMsg.author = "Dinesh Dhamotharan";
                    sndMsg.to = "http://localhost:" + crcvMsg.port + "/IPluggableComm";
                    sndMsg.from = "http://localhost:8095/IMessagePassingComm";
                    sndMsg.body = "logsreceived";
                    sndMsg.port = 8095;
                    comm.postMessage(sndMsg);
                }
                Thread.Sleep(2000);
            }
        }
        //deleting the files in repo
        private static void deleteFiles(int port, string body, string command)
        {
            try
            {
                string[] fnames = body.Split(' ');
                fnames = fnames.Where(arr => !String.IsNullOrEmpty(arr)).ToArray();
                List<string> names = new List<string>();
                List<string> files = new List<string>();
                foreach (string file in fnames)
                {
                    string[] list = Directory.GetFiles(RepoEnvironment.fileStorage, file);
                    try
                {
                    File.Delete(Path.GetFileName(list[0]));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                        CommMessage sndMsg1 = new CommMessage(CommMessage.MessageType.request);
                        sndMsg1.command = command;
                        sndMsg1.author = "Dinesh Dhamotharan";
                        sndMsg1.to = "http://localhost:8074/IPluggableComm";
                        sndMsg1.from = "http://localhost:8095/IMessagePassingComm";
                        sndMsg1.body = "Deletion failed with the exception" + ex.Message;
                        sndMsg1.port = 8095;
                        comm.postMessage(sndMsg1);
                    }
                }
                CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
                sndMsg.command = command;
                sndMsg.author = "Dinesh Dhamotharan";
                sndMsg.to = "http://localhost:8074/IPluggableComm";
                sndMsg.from = "http://localhost:8095/IMessagePassingComm";
                sndMsg.body = "success";
                sndMsg.port = 8095;
                comm.postMessage(sndMsg);
            }
            catch (Exception ex)
            {
                Console.Write("\n--{0}--\n", ex.Message);
                CommMessage sndMsg1 = new CommMessage(CommMessage.MessageType.request);
                sndMsg1.command = command;
                sndMsg1.author = "Dinesh Dhamotharan";
                sndMsg1.to = "http://localhost:8074/IPluggableComm";
                sndMsg1.from = "http://localhost:8095/IMessagePassingComm";
                sndMsg1.body = "Deletion failed with the exception"+ ex.Message;
                sndMsg1.port = 8095;
                comm.postMessage(sndMsg1);
            }
        }
        //getting the xml string and storing it as files
        public static void saveXmlFiles(int portno, string body)
        {
            try
            {
                string[] values = body.Split(',');
                string path = @RepoEnvironment.fileStorage + "/" + values[0];
                File.WriteAllText(path, values[1]);
                CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
                sndMsg.command = "xmlsaved";
                sndMsg.author = "Dinesh Dhamotharan";
                sndMsg.to = "http://localhost:8074/IPluggableComm";
                sndMsg.from = "http://localhost:8095/IMessagePassingComm";
                sndMsg.body = "xmlsaved";
                sndMsg.port = 8095;
                comm.postMessage(sndMsg);
            }
            catch (Exception ex)
            {
                Console.Write("\n--{0}--\n", ex.Message);
            }
        }
        //send file contents to client
        public static void sendContent(int portno, string fileName)
        {
            string body = "";
            try
            {
                string path = System.IO.Path.Combine(RepoEnvironment.fileStorage, fileName);
                body = File.ReadAllText(path);
            }
            catch (Exception ex)
            {
                body = ex.Message;
            }
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "fileContents";
            sndMsg.author = "Dinesh Dhamotharan";
            sndMsg.to = "http://localhost:8074/IPluggableComm";
            sndMsg.from = "http://localhost:8095/IMessagePassingComm";
            sndMsg.body = body;
            sndMsg.port = 8095;
            comm.postMessage(sndMsg);
        }
        // sending file names to client
        public static void sendFilenames(int portno,string type)
        { string ext = ""; string path = ""; string command = "";
            if (type.Equals("csfilenamesreq"))
            {
                ext = "*.cs"; path= RepoEnvironment.fileStorage; command = "csFileListFromRepo";
            }
            else
            {
                ext = "*.xml"; path = RepoEnvironment.fileStorage; command = "xmlFileListFromRepo";
            }
            string[] list = Directory.GetFiles(path, ext);
            string filenames = "";
            foreach (string fileone in list)
            {
                filenames = filenames + Path.GetFileName(fileone) + " ";
            }
            
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = command;
            sndMsg.author = "Dinesh Dhamotharan";
            sndMsg.to = "http://localhost:8074/IPluggableComm";
            sndMsg.from = "http://localhost:8095/IMessagePassingComm";
            sndMsg.body = filenames;
            sndMsg.port = 8095;
            comm.postMessage(sndMsg);
        }
            /*this method On request, sending files to the child builder temproary storage*/
        public static void sendFiles(int portno,string addrAndFiles,string flag)
        {   string[] values = addrAndFiles.Split(','); string addr = ""; string filenames = ""; string body = ""; string flg = "";
            if (flag.Equals("reqfrombuilder"))
            {
                addr = BuildEnvironment.fileStorage; filenames = values[0]; body = values[0]; flg = "r2b";
            }
            else
            {
                addr = values[0]; filenames = values[1]; body = "TransferCompleted"; flg = "r2cb";
            }
            
            Console.WriteLine("\n        =================================================");
            Console.WriteLine("\n    Repository started sending files to the requested location");
            Console.WriteLine("\n        =================================================");
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.connect);
            sndMsg.command = "show";
            sndMsg.author = "Dinesh Dhamotharan";
            sndMsg.to = "http://localhost:"+portno+"/IPluggableComm";
            sndMsg.from = "http://localhost:8095/IMessagePassingComm";
            sndMsg.body = "8095";
            comm.postMessage(sndMsg);
            List<string> names = getClientFileList(filenames);
            foreach (string file in names)
            {
                string fileSpec = file;
                string fileName = Path.GetFileName(fileSpec);
                Console.Write("\n sending \"{0}\" to \"{1}\"", fileName, addr);
                TestUtilities.putLine(string.Format("transferring file \"{0}\"", file));
                bool transferSuccess = true; int check = 1;
                do { transferSuccess = comm.postFile(file, flg, addr); check++; } while ((!transferSuccess)&&(check<=10));
                TestUtilities.checkResult(transferSuccess, "transfer");
                Thread.Sleep(2000);
            }
            Console.WriteLine("\n        =================================================");
            Console.WriteLine("\n                     File transfer completed");
            Console.WriteLine("\n        =================================================");
            sndMsg = new CommMessage(CommMessage.MessageType.connect);
            sndMsg.command = "TransferCompleted";
            sndMsg.author = "Dinesh Dhamotharan";
            sndMsg.to = "http://localhost:"+portno+"/IPluggableComm";
            sndMsg.from = "http://localhost:8095/IMessagePassingComm";
            sndMsg.body = body;
            sndMsg.port = 8095;
            comm.postMessage(sndMsg);
        }
        /*getting file names and actual directory of the files for posting the files using wcf*/
        public static List<string> getClientFileList(string filenames)
        {
            string[] fnames= filenames.Split(' ');
            fnames = fnames.Where(arr => !String.IsNullOrEmpty(arr)).ToArray();
            List<string> names = new List<string>();
            List<string> files = new List<string>();
            foreach (string file in fnames)
            {
                string[] list= Directory.GetFiles(RepoEnvironment.fileStorage, file);
                   if(list.Count()>0) names.Add(Path.GetFileName(list[0]));
            }
            return names;
        }

#if (TEST_MOCKREPO)
        static void Main(string[] args)
        {
            Console.Title = "Repository";
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Write("\n Starting Repository");
            Console.Write("\n===============================");
            
            RepoEnvironment.verbose = true;
            TestUtilities.vbtitle("Testing Repo", '=');
            Thread listenTrd = null;
            comm = new Comm("http://localhost", 8095);
            listenTrd = new Thread(listen);
            listenTrd.Start();
        }
#endif
    }

}


