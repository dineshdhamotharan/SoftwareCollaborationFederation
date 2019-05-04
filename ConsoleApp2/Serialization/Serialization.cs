///////////////////////////////////////////////////////////////////////
// Serialization.cs - XML Serializer used to form and parse xml files//
//                    (test requests)                                //
// Author:Dinesh kumar Dhamotharan      Source:Prof.Jim Fawcett      //
// Mail: ddhamoth@syr.edu                                            //
// SUID: 586563818                                                   //
// Software Modeling and Analysis, Fall 2017                         //
///////////////////////////////////////////////////////////////////////
/*
 * Demonstrate serializing an instance of some type to an XML string
 * and deserialize back to an instance of that type.
 *
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace Utilities
{
    public class TestElement  /* information about a single test */
    {
        public string testName { get; set; }
        public string testDriver { get; set; }
        public List<string> testCodes { get; set; } = new List<string>();

        public TestElement() { }
        public TestElement(string name)
        {
            testName = name;
        }
        public void addDriver(string name)
        {
            testDriver = name;
        }
        public void addCode(string name)
        {
            testCodes.Add(name);
        }
        public override string ToString()
        {
            string temp = "\n    test: " + testName;
            temp += "\n      testDriver: " + testDriver;
            foreach (string testCode in testCodes)
                temp += "\n      testCode:   " + testCode;
            return temp;
        }
    }

    public class TestReq  /* information about a single test */
    {
        
        public List<string> testLibraries { get; set; } = new List<string>();

        public TestReq() { }
        
        
        public void addLibraries(string name)
        {
            testLibraries.Add(name);
        }
        public override string ToString()
        {
            string temp = "";
            foreach (string testLibrary in testLibraries)
                temp += "\n      testLibrary:   " + testLibraries;
            return temp;
        }
    }

    public class TestRequest  /* a container for one or more TestElements */
    {
        public string author { get; set; }
        public List<TestElement> tests { get; set; } = new List<TestElement>();

        public TestRequest() { }
        public TestRequest(string auth)
        {
            author = auth;
        }
        public override string ToString()
        {
            string temp = "\n  author: " + author;
            foreach (TestElement te in tests)
                temp += te.ToString();
            return temp;
        }
    }

    public static class ToAndFromXml
    {
        //----< serialize object to XML >--------------------------------

        static public string ToXml(this object obj)
        {
            // suppress namespace attribute in opening tag

            XmlSerializerNamespaces nmsp = new XmlSerializerNamespaces();
            nmsp.Add("", "");

            var sb = new StringBuilder();
            try
            {
                var serializer = new XmlSerializer(obj.GetType());
                using (StringWriter writer = new StringWriter(sb))
                {
                    serializer.Serialize(writer, obj, nmsp);
                }
            }
            catch (Exception ex)
            {
                Console.Write("\n  exception thrown:");
                Console.Write("\n  {0}", ex.Message);
            }
            return sb.ToString();
        }
        //----< deserialize XML to object >------------------------------

        static public T FromXml<T>(this string xml)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(T));
                return (T)serializer.Deserialize(new StringReader(xml));
            }
            catch (Exception ex)
            {
                Console.Write("\n  deserialization failed\n  {0}", ex.Message);
                return default(T);
            }
        }
    }
    public static class Utilities
    {
        public static void title(this string aString, char underline = '-')
        {
            Console.Write("\n  {0}", aString);
            Console.Write("\n {0}", new string(underline, aString.Length + 2));
        }
    }
    ///////////////////////////////////////////////////////////////////
    // Widget class - used to test serialization and deserialization
    //
    public class Widget
    {
        /*
         * comment out the parameterless constructor to make non-serializable
         */
        public Widget() { }
        public Widget(string nm)
        {
            name = nm;
        }
        public string getName()
        {
            return name;
        }
        public string name { get; set; }
    }

    class TestXmlSerialization
    {
        static void Main(string[] args)
        {
            "Demonstrating XML Serialization and Deserialization".title('=');
            Console.WriteLine();

            Console.Write("\n  attempting to serialize Widget object:");
            Widget widget = new Widget("Jim");
            string xml = widget.ToXml();
            if (xml.Count() > 0)
                Console.Write("\n  widget:\n{0}\n", xml);

            Console.Write("\n  attempting to deserialize Widget object:");
            Widget newWidget = xml.FromXml<Widget>();
            if (newWidget == null)
            {
                Console.Write("\n  deserialized object is null");
            }
            else
            {
                string type = newWidget.GetType().Name;
                Console.Write("\n  retrieved object of type: {0}", type);

                string test = newWidget.getName();
                Console.Write("\n  reconstructed widget's name = \"{0}\"", test);
            }
            Console.Write("\n\n");
        }
    }
}
