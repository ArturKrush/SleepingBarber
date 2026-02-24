using System;
using System.Threading;

namespace SleepingBarber
{
    public class Program
    {
        static Random rand = new Random();
        const int numberOfChairs = 3;
        const int maxCustomers = 6;
        static int waiting = 0; // кількість клієнтів, що чекають

        // Локер, який захищає змінну waiting
        static object queueLocker = new();

        // Семафор для сигналізації перукареві: 0 спочатку (немає клієнтів)
        static Semaphore customersWaiting = new Semaphore(0, numberOfChairs);

        // Семафор для сигналізації клієнту: 0 спочатку (перукар спить/зайнятий)
        static Semaphore barberReady = new Semaphore(0, 1);

        public static void Barber()
        {
            while (true)
            {
                // Повідомлення, що барбер спить, виводиться лише тоді, коли нікого немає в черзі
                lock (queueLocker)
                {
                    if (waiting == 0)
                        Console.WriteLine("Barber is sleeping/waiting for customers...");
                }

                // Перукар чекає, поки з'явиться хоча б 1 клієнт. Якщо 0 - спить (потік блокується).
                customersWaiting.WaitOne();

                /* Клієнт розбудив перукаря. Клієнт приходить з черги.
                   Виключно потік барбера може змінювати/читати змінну waiting у цей момент часу */
                lock (queueLocker)
                {
                    waiting--;
                }

                // Перший з клієнтів отримує сигнал, що барбер його чекає
                barberReady.Release();

                // Перукар (тепер саме цей потік) стриже
                //Console.WriteLine("Barber is cutting hair...");
                Thread.Sleep(rand.Next(1, 3) * 1000); // Затримка рандомної довжини
                Console.WriteLine("Barber finished cutting.");
            }
        }

        public static void Customer(object id)
        {
            string name = $"Customer {id}";
            Console.WriteLine($"{name} comes to the barbershop.");

            /* Тільки один потік може працювати з виділеним за допомогою lock кодом
               в певний момент часу завдяки блокуванню queueLocker */
            lock (queueLocker)
            {
                // Якщо є вільні місця, то клієнт займає місце у черзі
                if (waiting < numberOfChairs)
                {
                    waiting++; // Займає місце в черзі
                    Console.WriteLine($"{name} took a waiting chair. Waiting: {waiting}");

                    // Сигналізує потоку перукаря, що він очікує (як мінімум 1 клієнт вже є)
                    customersWaiting.Release();
                }
                else
                {
                    Console.WriteLine($"No free seats left. {name} is leaving.");
                    return; // Місць немає, клієнт уходить (потік завершується)
                }
            }

            // Клієнт чекає, поки перукар покличе його в робоче крісло
            barberReady.WaitOne();

            // Тут клієнта стрижуть
            Console.WriteLine($"{name} is getting a haircut.");
        }

        static void Main(string[] args)
        {
            Thread barberThread = new Thread(Barber);
            barberThread.IsBackground = true; // Щоб програма могла завершитися при нескінченному циклі перукаря
            barberThread.Start();

            for (int i = 1; i <= maxCustomers; i++)
            {
                Thread customerThread = new Thread(Customer);
                customerThread.Start(i);
                Thread.Sleep(500); // Клієнти приходять з невеликим інтервалом
            }

            Console.ReadLine(); // Очікування натискання Enter для завершення
        }
    }
}