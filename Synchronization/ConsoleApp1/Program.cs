using System.Threading;

namespace Synchronization;

class Program
{
    static void Main(string[] args)
    {
        FibonacciCounter counter = new FibonacciCounter();

        Console.Write("Enter start :: ");
        Console.ReadLine();

        Console.WriteLine("Step\tn1\tn2\tevenCount");

        Thread[] threads = new Thread[3];
        for (int i = 0; i < threads.Length; i++)
        {
            threads[i] = new Thread(() =>
            {
                for (int j = 0; j < 10; j++)
                {
                    counter.NextIteration();
                }
            });
        }

        for (int i = 0; i < threads.Length; i++)
        {
            threads[i].Start();
        }

        for (int i = 0; i < threads.Length; i++)
        {
            threads[i].Join();
        }

        Console.WriteLine("Done");
    }
}