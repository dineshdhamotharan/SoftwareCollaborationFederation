using System;
using System.Reflection;
using System.IO;

namespace Test
{
  public class Test : IDriver
  {
    public Test()
    {
      Console.Write("\n  constructing instance of Test3");
    }
    public virtual void display()
    {
      Console.Write("\n  Test #3:");
    }
    private ITestCode getTested()
    {
      Tested tested = new Tested();
      return tested;
    }
    public virtual bool test()
    {
      ITestCode tested = getTested();
      return tested.test();
    }
  }
}
