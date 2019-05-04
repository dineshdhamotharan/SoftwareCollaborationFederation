using System;

namespace Test
{
  public class Tested : ITestCode
  {
    public Tested()
    {
      Console.Write("\n    constructing instance of Test code");
    }
    public bool test()
    {
      Console.Write("\n    Production code - TestedLib");
      Dependency tld = new Dependency();
      Console.Write("\n    Testing Multiplication: 20*10:");
            int result = tld.Multiply(20, 11);
           Console.Write("Expected result: "+ (20 * 10) + "  Actual result: "+ result + "\n");
      if(result==200)
            {
                return true;
            }
      else
            {
                return false;
            }
    }
  }
}
