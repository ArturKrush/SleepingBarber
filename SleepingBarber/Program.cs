namespace SleepingBarber
{
    public class Program
    {
        static Random rand = new Random();
        const int maxCustomers = 4;
        const int numberOfChairs = 3;
        static Semaphore customers = new Semaphore(numberOfChairs, numberOfChairs);
        static object barber = new();
        static Mutex mutex = new Mutex();
        static int waiting = 0;

        //static bool isSomebodyWaiting = false;

        public static void Barber()
        {
            while (true)
            {
                //While nobody is in the barbershop, barber will sleep (checks for the first time)
                //if (!isSomebodyWaiting)
                    CheckForCustomers();

                //StartCuttingCustomer();

                Thread.Sleep(20000);
                return; //For exit from endless loop
            }
        }

        public static void CheckForCustomers()
        {
            mutex.WaitOne();
            if (waiting == 0)
            {
                mutex.ReleaseMutex();
                BarberGoToSleep();
            }
            //else
            //    isSomebodyWaiting = true;
        }

        public static void BarberGoToSleep()
        {
            Console.WriteLine("No customer is waiting. Barber is sleeping.");
            //Thread.Sleep(2000);
        }

        public static void StartCuttingCustomer()
        {
            lock (barber)
            {
                mutex.WaitOne();
                --waiting;
                //if(waiting == 0)
                //    Console.WriteLine("{0} the first for today or the first for now.", Thread.CurrentThread.Name);
                mutex.ReleaseMutex();

                customers.Release();

                Console.WriteLine("Barber is cutting {0}.", Thread.CurrentThread.Name);
                Thread.Sleep(rand.Next(1, 4) * 1000);
                Console.WriteLine("Barber has finished cutting {0}.", Thread.CurrentThread.Name);
            }
        }

        public static void Customer()
        {
            Console.WriteLine("{0} comes to the shop.", Thread.CurrentThread.Name);
            mutex.WaitOne();
            Thread.Sleep(1000);

            if (waiting == 0)
            {
                Console.WriteLine("Nobody was waiting. {0} is first. {0} will check if somebody is in the barber room.",
                    Thread.CurrentThread.Name);
            }
            else if (waiting == numberOfChairs)
            {
                Console.WriteLine("No free seats left. {0} is leaving.", Thread.CurrentThread.Name);
                mutex.ReleaseMutex();
                return;
            }

            ++waiting;
            mutex.ReleaseMutex();

            customers.WaitOne(); //In queue, maximum 3 customers will left
            StartCuttingCustomer();
        }

        static void Main(string[] args)
        {
            //Creating 1 thread for barber and starting Barber method in it
            Thread barberThread = new Thread(Barber);
            barberThread.Start();

            //Creating maxCustomers threads for customers and starting Customer method in them
            Thread[] customers = new Thread[maxCustomers];
            Thread customerThread;
            for (int i = 0; i < maxCustomers; i++)
            {
                customerThread = new Thread(Customer);
                customerThread.Name = $"Customer {i}";
                customers[i] = customerThread;
                customers[i].Start();
            }
            for (int i = 0; i < maxCustomers; i++)
            {
                customers[i].Join();
            }

            //2 new customers are comming after a break
            Thread.Sleep(2000);
            Thread[] customers2 = new Thread[2];
            for (int i = 0; i < 2; i++)
            {
                customerThread = new Thread(Customer);
                customerThread.Name = $"Customer {maxCustomers + (i + 1)}";
                customers2[i] = customerThread;
                customers2[i].Start();
            }
            for (int i = 0; i < 2; i++)
            {
                customers2[i].Join();
            }

            barberThread.Join();
            Console.WriteLine("Job ends for today!");
        }
    }
}