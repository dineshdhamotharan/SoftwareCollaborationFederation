//////////////////////////////////////////////////////////////////////////////
// Buildserver.cs - Demonstrate Build Server operations                     //
// ver 1.0                                                                  //
//                                                                          //
// Author: Dinesh Kumar Dhamotharan, ddhamoth@syr.edu                       //
// Application: CSE681 Project 4-MainWindow.xaml.cs                         //
// Environment: C# console                                                  //
//////////////////////////////////////////////////////////////////////////////
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
* Function name 1 - what it does in 1 line 
* Function name 2 - what it does in 1 line 
* . 
* . 
* . 
* All other functions used in this package... 
*  
* Required Files:
* TestUtilities.cs, MPCommServices.cs,IMPCommService.cs,serialization.cs
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Utilities;

namespace ClientGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static int repocheck { get; set; } = 0;
        private static int Thcheck { get; set; } = 0;
        private static int buildcheck { get; set; } = 0;
        private static bool restartcheck { get; set; } = false;
        static Thread repThread = null;
        static Thread thThread = null;
        private int testno { get; set; }  = 0;
        private static int requestcount { get; set; } = 1;
        private static int proc { get; set; }  = 0;
        private static int tccnt { get; set; }  = 0;
        private static List<String> testlist { get; set; }  = new List<String>();
        private static List<String> tcfilenames { get; set; }  = new List<String>();
        private static int xmlcnt { get; set; }  = 1;
        private static Comm comm { get; set; } = null;
        public MainWindow()
        {
            InitializeComponent();
            comm = new Comm("http://localhost", 8074);
            Thread listenTrd = null;
            listenTrd = new Thread(listen);
            listenTrd.Start();
            refreshCsList();
            refreshXmlList();
            //createChildMessage(2);
            //sendTestRequestToBuild("TestRequest_3.xml");
        }
        /*----< listen thread of Client which listens to the incoming message >-----------------------------------------*/
        void listen()
        {
            while (true)
            {
                    ClientEnvironment.verbose = true;
                    CommMessage crcvMsg = comm.getMessage();
                    if (crcvMsg.body != null)
                    {
                        if (ClientEnvironment.verbose)
                            crcvMsg.show();
                    if (crcvMsg.command.Equals("csFileListFromRepo"))
                    {
                        Console.WriteLine(crcvMsg.body);
                        csFileListFromRepo(crcvMsg.body);
                    }
                    if (crcvMsg.command.Equals("xmlsaved"))
                    {
                        refreshXmlList();
                    }
                    if (crcvMsg.command.Equals("deleterequest"))
                    {
                        if(crcvMsg.body.Equals("success")) refreshXmlList();
                        else MessageBox.Show(String.Format(crcvMsg.body));
                    }
                    if (crcvMsg.command.Equals("deletefiles"))
                    {
                        if (crcvMsg.body.Equals("success")) refreshCsList();
                        else MessageBox.Show(String.Format(crcvMsg.body));
                    }
                    if (crcvMsg.command.Equals("xmlFileListFromRepo"))
                    {
                        xmlFileListFromRepo(crcvMsg.body);
                    }
                    if (crcvMsg.command.Equals("notification"))
                    {
                            Action act = () => { AddInNotificationList(crcvMsg.body); };
                            string[] args = new string[] { };
                            Dispatcher.Invoke(act, args);
                    }
                    if (crcvMsg.command.Equals("fileContents"))
                    {
                        showcontents(crcvMsg.body);
                    }
                }  
                Thread.Sleep(1000);
            }
        }
        /*----< used to show the contents of the file in a pop up >-----------------------------------------*/
        void showcontents(string contents)
        {
            Thread t = new Thread(() => PopupthreadProc(contents));
            t.SetApartmentState(ApartmentState.STA);

            t.Start();
            
        }
        /*----< used to show the contents of the file in a pop up >-----------------------------------------*/
        private void PopupthreadProc(string contents)
        {
            CodePopUp popup = new CodePopUp();
            popup.codeView.Text = contents;
            popup.ShowDialog();
        }
        /*----< used to add the list of cs files in the GUI >-----------------------------------------*/
        void csFileListFromRepo(string names)
        {
            string[] fnames = names.Split(' ');
            fnames = fnames.Where(arr => !String.IsNullOrEmpty(arr)).ToArray();
            foreach (string file in fnames)
            {
                Action act = () => { AddInRepoCsList(file); };
                string[] args = new string[] { };
                Dispatcher.Invoke(act, args);
            }
        }
        /*----< used to add the list of xml files in the GUI >-----------------------------------------*/
        void xmlFileListFromRepo(string names)
        {
            string[] fnames = names.Split(' ');
            fnames = fnames.Where(arr => !String.IsNullOrEmpty(arr)).ToArray();
            foreach (string file in fnames)
            {
                Action act = () => { AddInRepoXmlList(file); };
                string[] args = new string[] { };
                Dispatcher.Invoke(act, args);
            }
        }
        /*----< used to add the incoming notification in the GUI >-----------------------------------------*/
        void AddInNotificationList(string file)
        {
            notification.Items.Add(file);
        }
        /*----< used to add the list of cs files in the GUI >-----------------------------------------*/
        void AddInRepoCsList(string file)
        {
           if(!(repo.Items.Contains(file))) repo.Items.Insert(0, file);
        }
        /*----< used to add the list of xml files in the GUI >-----------------------------------------*/
        void AddInRepoXmlList(string file)
        {
            if (!(repoXml.Items.Contains(file))) repoXml.Items.Insert(0, file);
        }
        //Browse and upload files to repository via comm service
        private void Browse(object sender, RoutedEventArgs e)
        {
            var dialog_box = new Microsoft.Win32.OpenFileDialog();
            dialog_box.Multiselect = true;
            dialog_box.DefaultExt = ".cs";
            dialog_box.Filter = "CS Files (*.cs) | *.cs";
            var result = dialog_box.ShowDialog();
            string str = System.IO.Path.GetFullPath(@RepoEnvironment.fileStorage);
            if (result == true)
            {
                string[] filelist = dialog_box.FileNames;
                string fdirectory = System.IO.Path.GetDirectoryName(filelist[0]);
                Console.WriteLine("\n    Client sending files to the repository");
                Console.WriteLine("\n        =================================================");
                CommMessage sndMsg = new CommMessage(CommMessage.MessageType.connect);
                sndMsg.command = "show";
                sndMsg.author = "Dinesh Dhamotharan";
                sndMsg.to = "http://localhost:8095/IPluggableComm";
                sndMsg.from = "http://localhost:8074/IMessagePassingComm";
                sndMsg.body = "8074";
                sndMsg.port = 8074;
                comm.postMessage(sndMsg);
                foreach (var file in filelist)
                {
                    string fileSpec = file;
                    string fileName = System.IO.Path.GetFileName(fileSpec);
                    Console.Write("\n sending \"{0}\" to \"{1}\"", fileName,RepoEnvironment.fileStorage);
                    TestUtilities.putLine(string.Format("transferring file \"{0}\"", file));
                    bool transferSuccess = comm.postFile(fileName, "c2r", fdirectory);
                    Console.WriteLine("\n"+fdirectory+"\n "+"--------");
                    TestUtilities.checkResult(transferSuccess, "transfer");
                    Thread.Sleep(2000);
                }
                Console.WriteLine("\n                     File transfer completed");
                Console.WriteLine("\n        =================================================");
                refreshCsList();
            }

        }
        /*----< request message to repo for list of cs files >-----------------------------------------*/
        private void refreshCsList()
        {
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "csfilenamesreq";
            sndMsg.author = "Dinesh Dhamotharan";
            sndMsg.to = "http://localhost:8095/IPluggableComm";
            sndMsg.from = "http://localhost:8074/IMessagePassingComm";
            sndMsg.body = "csfilenamesreq";
            sndMsg.port = 8074;
            comm.postMessage(sndMsg);
        }
        /*----< request message to repo for list of xml files>-----------------------------------------*/
        private void refreshXmlList()
        {
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "xmlfilenamesreq";
            sndMsg.author = "Dinesh Dhamotharan";
            sndMsg.to = "http://localhost:8095/IPluggableComm";
            sndMsg.from = "http://localhost:8074/IMessagePassingComm";
            sndMsg.body = "xmlfilenamesreq";
            sndMsg.port = 8074;
            comm.postMessage(sndMsg);
        }
        //creating tests for making the test requests 
        private void Createtest(object sender, RoutedEventArgs e)
        {
            String filenames = "";
            if (repo.SelectedItems.Count != 0)
            {
                bool check = false;
                foreach (var str in repo.SelectedItems)
                {
                    if(str.ToString().StartsWith("testdriver"))
                    {
                        check = true;
                    }
                }
                if (check)
                {
                    foreach (var str in repo.SelectedItems)
                    {
                        filenames = filenames + str.ToString() + " ";
                    }
                    tcfilenames.Add(filenames);
                    testno = testno + 1;
                    test.Items.Add("TestCase" + testno.ToString());
                    testlist.Add("TestCase" + testno.ToString());
                }
                else
                {
                    MessageBox.Show("Please select atleast one test driver for a test!");
                }
            }
            else
            {
                MessageBox.Show("Please select files for creating test request");
            }
        }
        /*----< used to create test request >-----------------------------------------*/
        private void Createrequest(object sender, RoutedEventArgs e)
        {
            TestRequest tr = new TestRequest();
            string[] fname;
            if (test.Items.Count != 0)
            {
                for (int i = 1; i <= test.Items.Count; i++)
                {
                    TestElement te = new TestElement();
                    te.testName = testlist[tccnt];
                    fname = tcfilenames[tccnt].Split(new char[0]);
                    for (int j = 0; j < fname.Length; j++)
                    {
                        if (fname[j].ToString().StartsWith("testdriver"))
                        {
                            te.addDriver(fname[j]);
                        }
                        else
                        {
                            te.addCode(fname[j]);
                        }
                    }
                    tr.author = "Dinesh";
                    tr.tests.Add(te);
                    string xml = tr.ToXml();
                    string filename = "TestRequest_" + xmlcnt + "_" + DateTime.Now.ToString("MMddyyyyHHmmssfff") + ".xml";
                    sendTestRequestToRepo(filename + ","+ xml);
                    request.Items.Add(filename);
                    test.Items.Clear();
                    tccnt = tccnt + 1;
                }
                xmlcnt = xmlcnt + 1;
                requestcount = requestcount + 1;            
            }
            else
            {
                MessageBox.Show(String.Format("No tests available to create request.! Please create test!"));
            }
        }
        /*----< used to send the test request string to the repository>-----------------------------------------*/
        private void sendTestRequestToRepo(string body)
        {
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "xmlstring";
            sndMsg.author = "Dinesh Dhamotharan";
            sndMsg.to = "http://localhost:8095/IPluggableComm";
            sndMsg.from = "http://localhost:8074/IMessagePassingComm";
            sndMsg.body = body;
            sndMsg.port = 8074;
            comm.postMessage(sndMsg);
        }
    
        //creating mother builder process 
        static bool createMotherBuilder(int prcess)
        {
                Process proc = new Process();
                string fileName = "..\\..\\..\\MotherBuilder\\bin\\debug\\MotherBuilder.exe";
                string absFileSpec = System.IO.Path.GetFullPath(fileName);

                Console.Write("\n  attempting to start {0}", absFileSpec);
                string commandline = prcess.ToString();
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
        // kill button action performed. sending quit message to the mother build server
        private void Kill(object sender, RoutedEventArgs e)
        {
            if (buildcheck == 1)
            {
                //bool check = true;
                CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
                sndMsg.command = "Quit";
                sndMsg.author = "Dinesh Dhamotharan";
                sndMsg.to = "http://localhost:8080/IPluggableComm";
                sndMsg.from = "http://localhost:8074/IMessagePassingComm";
                sndMsg.body = "Quit";
                sndMsg.port = 8074;
                comm.postMessage(sndMsg);
                    buildcheck = 0;
                    repocheck = 0;
                    Console.WriteLine("\n---------Killed Successfully-------");
                    MessageBox.Show(String.Format("Process killed successfully!"));
            }
            else
            {
                MessageBox.Show(String.Format("No active process to kill!"));
            }
        }
        
       
        /*----< used to send the create child process request to mother builder>-----------------------------------------*/
        private void InitiateComponents(object sender, RoutedEventArgs e)
        {
            if (buildcheck == 0)
            {
                try
                {
                    proc = Int32.Parse(process.Text);
                }
                catch (Exception Ex)
                {
                    MessageBox.Show(String.Format(Ex.Message));
                }
                if (proc > 0)
                {
                    createChildMessage(proc);
                }
                else
                {
                    MessageBox.Show(String.Format("Please enter valid number of process"));
                }
            }
            else
            {
                MessageBox.Show(String.Format("Please kill the process before building again!!"));
            }
        }
        private void createChildMessage(int proc)
        {
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "childno";
            sndMsg.author = "Dinesh Dhamotharan";
            sndMsg.to = "http://localhost:8080/IPluggableComm";
            sndMsg.from = "http://localhost:8074/IMessagePassingComm";
            sndMsg.body = proc.ToString();
            sndMsg.port = 8074;
            buildcheck = 1;
            comm.postMessage(sndMsg);
        }
        /*----< used to select test request files for processing>-----------------------------------------*/
        private void ProcessExisting(object sender, RoutedEventArgs e)
        {
            if (buildcheck == 0)
            {
                MessageBox.Show(String.Format("Please initiate the builder component to process the request!!"));
            }
            else
            {
                String filenames = "";
                if (repoXml.SelectedItems.Count != 0)
                {
                    foreach (var str in repoXml.SelectedItems)
                    {
                        filenames = filenames + str.ToString() + " ";
                    }
                    sendTestRequestToBuild(filenames);
                }
                else
                {
                    MessageBox.Show("Please select requests for processing");
                }
            }
        }
        /*----< used to select test request files for processing>-----------------------------------------*/
        private void processRequest(object sender, RoutedEventArgs e)
        {
            if (buildcheck == 0)
            {
                MessageBox.Show(String.Format("Please initiate the builder component to process the request!!"));
            }
            else
            {
                String filenames = "";
                if (request.SelectedItems.Count != 0)
                {                    
                        foreach (var str in request.SelectedItems)
                        {
                            filenames = filenames + str.ToString() + " ";
                        }
                    sendTestRequestToBuild(filenames);
                }
                else
                {
                    MessageBox.Show("Please select requests for processing");
                }
            }
        }
        /*----< used to send the selected test requests for processing>-----------------------------------------*/
        private void sendTestRequestToBuild(string body)
        {
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "xmlfilenames";
            sndMsg.author = "Dinesh Dhamotharan";
            sndMsg.to = "http://localhost:8080/IPluggableComm";
            sndMsg.from = "http://localhost:8074/IMessagePassingComm";
            sndMsg.body = body;
            sndMsg.port = 8074;
            comm.postMessage(sndMsg);
        }
        /*----< used to open the selected cs files>-----------------------------------------*/
        private void Repo_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string fileName = repo.SelectedValue as string;
            Console.WriteLine("filename----" + repo.SelectedValue);
            Console.WriteLine("filename----" + fileName);

            sendContentRequest(fileName);
        }
        /*----< used to open the selected test request files>-----------------------------------------*/
        private void NewReq_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string fileName = request.SelectedValue as string;
            sendContentRequest(fileName);
        }
        /*----< used to open the selected xml files>-----------------------------------------*/
        private void OldReq_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string fileName = repoXml.SelectedValue as string;
            sendContentRequest(fileName);
        }
        /*----< used to send content read message to the repository>-----------------------------------------*/
        private void sendContentRequest(string body)
        {
            Console.WriteLine("filename----"+body);
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "ContentReq";
            sndMsg.author = "Dinesh Dhamotharan";
            sndMsg.to = "http://localhost:8095/IPluggableComm";
            sndMsg.from = "http://localhost:8074/IMessagePassingComm";
            sndMsg.body = body;
            sndMsg.port = 8074;
            comm.postMessage(sndMsg);
        }
        /*----< used to delete the selected xml files from the list>-----------------------------------------*/
        private void DeleteNew(object sender, RoutedEventArgs e)
        {
            if (request.SelectedItems.Count != 0)
            {
                foreach (var str in request.SelectedItems)
                {
                    request.Items.Remove(str);
                }
                
            }
            else
            {
                MessageBox.Show("Please select requests to Delete!");
            }
        }
        /*----< used to delete the selected xml files from the repository>-----------------------------------------*/
        private void DeleteExisting(object sender, RoutedEventArgs e)
        {
            String filenames = "";
            if (repoXml.SelectedItems.Count != 0)
            {
                foreach (var str in repoXml.SelectedItems)
                {
                    filenames = filenames + str.ToString() + " ";
                    repoXml.Items.Remove(str);
                }
                sendDeleteRequestToBuild(filenames,"request");
            }
            else
            {
                MessageBox.Show("Please select requests to Delete!");
            }
        }
        /*----< used to send deleterequest to repository>-----------------------------------------*/
        private void sendDeleteRequestToBuild(string body,string type)
        {
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "delete"+type;
            sndMsg.author = "Dinesh Dhamotharan";
            sndMsg.to = "http://localhost:8095/IPluggableComm";
            sndMsg.from = "http://localhost:8074/IMessagePassingComm";
            sndMsg.body = body;
            sndMsg.port = 8074;
            comm.postMessage(sndMsg);
        }
        /*----< used to delete the selected cs files from repository>-----------------------------------------*/
        private void DeleteCs(object sender, RoutedEventArgs e)
        {
            String filenames = "";
            if (repo.SelectedItems.Count != 0)
            {
                foreach (var str in repo.SelectedItems)
                {
                    filenames = filenames + str.ToString() + " ";
                    repo.Items.Remove(str);
                }
                sendDeleteRequestToBuild(filenames, "files");
            }
            else
            {
                MessageBox.Show("Please select files to delete");
            }
        }
    }
}
