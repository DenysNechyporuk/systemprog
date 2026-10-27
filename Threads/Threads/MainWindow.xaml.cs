using System.Numerics;
using System.Threading;
using System.Windows;

namespace _03_Threads;

public partial class MainWindow : Window
{
    bool primeWork = false;
    bool fibWork = false;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void StartPrime_Click(object sender, RoutedEventArgs e)
    {
        int low = 2;
        int high = int.MaxValue;

        if (lowerTextBox.Text != "")
        {
            if (!int.TryParse(lowerTextBox.Text, out low))
            {
                MessageBox.Show("Wrong number");
                return;
            }
        }
        if (upperTextBox.Text != "")
        {
            if (!int.TryParse(upperTextBox.Text, out high))
            {
                MessageBox.Show("Wrong number");
                return;
            }
        }
        if (low < 2)
        {
            low = 2;
        }

        primeWork = true;
        startPrimeButton.IsEnabled = false;

        Thread primeThread = new Thread(() =>
        {
            for (int i = low; i <= high && primeWork; i++)
            {
                bool prime = true;
                for (int j = 2; j * j <= i; j++)
                {
                    if (i % j == 0)
                    {
                        prime = false;
                        break;
                    }
                }
                if (prime)
                {
                    Dispatcher.Invoke(() =>
                    {
                        primeListBox.Items.Add(i);
                        primeListBox.ScrollIntoView(i);
                    });
                    Thread.Sleep(100);
                }
            }
            primeWork = false;
            Dispatcher.Invoke(() => startPrimeButton.IsEnabled = true);
        });
        primeThread.IsBackground = true;
        primeThread.Start();
    }

    private void StopPrime_Click(object sender, RoutedEventArgs e)
    {
        primeWork = false;
    }

    private void StartFib_Click(object sender, RoutedEventArgs e)
    {
        fibWork = true;
        startFibButton.IsEnabled = false;

        Thread fibThread = new Thread(() =>
        {
            BigInteger a = 0;
            BigInteger b = 1;
            while (fibWork)
            {
                BigInteger value = a;
                Dispatcher.Invoke(() =>
                {
                    fibListBox.Items.Add(value);
                    fibListBox.ScrollIntoView(value);
                });
                BigInteger c = a + b;
                a = b;
                b = c;
                Thread.Sleep(300);
            }
            Dispatcher.Invoke(() => startFibButton.IsEnabled = true);
        });
        fibThread.IsBackground = true;
        fibThread.Start();
    }

    private void StopFib_Click(object sender, RoutedEventArgs e)
    {
        fibWork = false;
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        primeWork = false;
        fibWork = false;
    }
}