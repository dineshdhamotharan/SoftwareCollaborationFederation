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
      Console.Write("\n    Testing Addition: 50+50:");
            int result = tld.Addition(50, 50);
            Console.Write("Expected result: " + (50+50) + "  Actual result: " + result + "\n");
            if (result == 100)
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
