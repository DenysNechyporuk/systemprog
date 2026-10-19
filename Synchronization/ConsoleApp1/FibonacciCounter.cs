using System.Threading;

namespace Synchronization;

class FibonacciCounter
{
    private readonly object locker = new object();

    public int N1 { get; set; }
    public int N2 { get; set; }
    public int EvenCount { get; set; }
    public int Step { get; set; }

    public FibonacciCounter()
    {
        N1 = 0;
        N2 = 1;
        EvenCount = 0;
        Step = 0;
    }

    public void NextIteration()
    {
        Monitor.Enter(locker);
        try
        {
            int next = N1 + N2;
            N1 = N2;
            N2 = next;

            if (next % 2 == 0)
            {
                EvenCount++;
            }

            Step++;
            Console.WriteLine(Step + "\t" + N1 + "\t" + N2 + "\t" + EvenCount);
        }
        finally
        {
            Monitor.Exit(locker);
        }
    }
}