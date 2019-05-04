using System;

namespace Test
{
  public interface IDriver      // interface for test driver
  {
    void display();
    bool test();
  }

  public interface ITestCode    // interface for tested code
  {
    bool test();
  }
}
