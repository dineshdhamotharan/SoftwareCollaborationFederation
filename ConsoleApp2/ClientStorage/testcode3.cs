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
      Console.Write("\n    Testing Division: 10/1:");
            
                int result =tld.Divide( 10 , 0);
                Console.Write("Expected result: ", 10 /1, "  Actual result: ", result, "\n");
                if (result == 10)
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
