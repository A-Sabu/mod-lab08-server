using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using static System.Runtime.InteropServices.JavaScript.JSType;
using ScottPlot;
namespace Lab08{
    class Program
    {      
        static void Main()
        {
            double intensivityClientsRequests = 0.048; // lambda (0;1]
            double intensivityServerMaintenance = 0.008; // mu (0;1]
            int countServerThreads = 3; // n
            int countClientRequests = 100;

            SimulationQueuingSystem SQS = new SimulationQueuingSystem(intensivityClientsRequests, intensivityServerMaintenance, countServerThreads, countClientRequests, true);
            Console.WriteLine("Практические показатели:");
            Dictionary<string, double> experimentalCoefficientsDictionary = SQS.server.CalculateExperimentalСoefficients();
            foreach (var (name, value) in experimentalCoefficientsDictionary)
            {
                Console.WriteLine($"{name}: {value}");
            }
            Console.WriteLine("Всего заявок: {0}", SQS.server.requestCount);
            Console.WriteLine("Обработано заявок: {0}", SQS.server.processedCount);
            Console.WriteLine("Отклонено заявок: {0}", SQS.server.rejectedCount);
            Console.WriteLine("Теоретические показатели:");
            Dictionary<string, double> estimatedCoefficientsDictionary = CalculateEstimatedСoefficients(intensivityClientsRequests, intensivityServerMaintenance, countServerThreads);
            foreach (var (name, value) in estimatedCoefficientsDictionary)
            {
                Console.WriteLine($"{name}: {value}");
            }

            GetAnalysis();
        }
        static long Factorial(long x) => (x == 0) ? 1 : x * Factorial(x - 1);
        static Dictionary<string, double> CalculateEstimatedСoefficients(double intensivityClientsRequests, double intensivityServerMaintenance, int countServerThreads)
        {
            Dictionary<string, double> coefficientsDictionary = new Dictionary<string, double>();

            double ReducedFlowRate = intensivityClientsRequests / intensivityServerMaintenance; // ro
            double probabilityDowntimeSystem = 0;
            for (int i = 0; i <= countServerThreads; i++) probabilityDowntimeSystem += Math.Pow(ReducedFlowRate, i) / Factorial(i);
            probabilityDowntimeSystem = 1 / probabilityDowntimeSystem; //P0
            double probabilityFailureSystem = Math.Pow(ReducedFlowRate, countServerThreads) / Factorial(countServerThreads) * probabilityDowntimeSystem; //Pn
            double relativeThroughput = 1 - probabilityFailureSystem; //Q
            double absoluteThroughput = intensivityClientsRequests * relativeThroughput; //A
            double averageCountBusyThreads = absoluteThroughput / intensivityServerMaintenance; //k

            coefficientsDictionary.Add("Вероятность простоя системы", probabilityDowntimeSystem);
            coefficientsDictionary.Add("Вероятность отказа системы", probabilityFailureSystem);
            coefficientsDictionary.Add("Относительная пропускная способность системы", relativeThroughput);
            coefficientsDictionary.Add("Абсолютная пропускная способность системы", absoluteThroughput);
            coefficientsDictionary.Add("Среднее число занятых каналов системы", averageCountBusyThreads);

            return coefficientsDictionary;
        }
        static void GetAnalysis()
        {
            Console.WriteLine("Начало анализа");
            Dictionary<double, List<Dictionary<string, double>>> analysisData = new Dictionary<double, List<Dictionary<string, double>>>();
            List<double> intensivityClientsRequestsList = new List<double>();
            for (double intensivity = 0.001; intensivity < 0.1; intensivity += 0.003) intensivityClientsRequestsList.Add(intensivity);
            int iter = 0;
            foreach (double intensivityClientsRequests in intensivityClientsRequestsList)
            {
                iter++;
                double intensivityServerMaintenance = 0.008; // mu (0;1]
                int countServerThreads = 3; // n
                int countClientRequests = 100;

                SimulationQueuingSystem SQS = new SimulationQueuingSystem(intensivityClientsRequests, intensivityServerMaintenance, countServerThreads, countClientRequests, false);

                analysisData.Add(intensivityClientsRequests, new List<Dictionary<string, double>>());
                analysisData[intensivityClientsRequests].Add(SQS.server.CalculateExperimentalСoefficients());
                analysisData[intensivityClientsRequests].Add(CalculateEstimatedСoefficients(intensivityClientsRequests, intensivityServerMaintenance, countServerThreads));

                Console.WriteLine($"Процесс {iter}/{intensivityClientsRequestsList.Count}");
            }
            string filePath = "results.txt";
            File.WriteAllText(filePath, string.Empty, Encoding.UTF8);
            using (StreamWriter writer = new StreamWriter(filePath, true, Encoding.UTF8))
            {
                writer.WriteLine("lambda mu estP0 estPn estQ estA estK expP0 expPn expQ expA expK");
                string strData;
                foreach (var (lambda, val) in analysisData)
                {
                    strData = lambda.ToString() + " ";
                    foreach (var estkoeff in val[1])
                    {
                        strData += estkoeff.Value.ToString() + " ";
                    }
                    foreach (var expkoeff in val[0])
                    {
                        strData += expkoeff.Value.ToString() + " ";
                    }
                    writer.WriteLine(strData);
                }
            }

            PlotCoefficientComparison(analysisData,"Вероятность простоя системы","p-1.png","P0 (вероятность простоя)");
            PlotCoefficientComparison(analysisData,"Вероятность отказа системы","p-2.png","Pn (вероятность отказа)");
            PlotCoefficientComparison(analysisData,"Относительная пропускная способность системы","p-3.png","Q (относительная пропускная способность)");
            PlotCoefficientComparison(analysisData,"Абсолютная пропускная способность системы","p-4.png","A (абсолютная пропускная способность)");
            PlotCoefficientComparison(analysisData,"Среднее число занятых каналов системы","p-5.png","K (среднее число занятых каналов)");
        }
        static void PlotCoefficientComparison(Dictionary<double, List<Dictionary<string, double>>> analysisData,string coefficientName,string fileName,string yAxisLabel)
        {
            List<double> lambdas = new List<double>();
            List<double> estValues = new List<double>();
            List<double> expValues = new List<double>();

            foreach (var (lambda, valList) in analysisData)
            {
                lambdas.Add(lambda);

                if (valList[1].TryGetValue(coefficientName, out double est))
                    estValues.Add(est);
                else
                    estValues.Add(0);

                if (valList[0].TryGetValue(coefficientName, out double exp))
                    expValues.Add(exp);
                else
                    expValues.Add(0);
            }

            var plt = new Plot();
            plt.Add.Scatter(lambdas.ToArray(), estValues.ToArray());
            plt.Add.Scatter(lambdas.ToArray(), expValues.ToArray());

            plt.Title($"Зависимость {yAxisLabel} от λ");
            plt.XLabel("λ (интенсивность заявок)");
            plt.YLabel(yAxisLabel);
            plt.ShowLegend();

            string resultDir = "result";
            if (!Directory.Exists(resultDir)) Directory.CreateDirectory(resultDir);
            plt.SavePng(Path.Combine(resultDir, fileName), 1400, 900);
            Console.WriteLine($"График сохранён: result/{fileName}");
        }
    }
    struct PoolRecord
    {
        public Thread thread;
        public bool in_use;
    }
    class Server
    {
        private PoolRecord[] pool;
        private readonly int timeIntervalProcessingRequest;
        private object threadLock = new object();
        public int requestCount = 0;
        public int processedCount = 0;
        public int rejectedCount = 0;
        private Stopwatch totalTime = new Stopwatch();
        private double totalDowntime = 0;
        private double totalBusyThreadsTime = 0;
        private double lastChangeStateTime = 0;
        private int countBusyThreads = 0;
        private bool writeToConsole;
        public Server(int timeIntervalProcessingRequest, int countServerThreads, bool writeToConsole)
        {
            this.timeIntervalProcessingRequest = timeIntervalProcessingRequest;
            pool = new PoolRecord[countServerThreads];
            totalTime.Start();
            this.writeToConsole = writeToConsole;
        }
        public void proc(object? sender, procEventArgs e)
        {
            lock (threadLock)
            {
                if (sender == null) throw new ArgumentNullException(nameof(sender));
                UpdateState();
                if (writeToConsole) Console.WriteLine("Поступила заявка #{0}", e.id);
                requestCount++;
                for (int i = 0; i < pool.Length; i++)
                {
                    if (!pool[i].in_use)
                    {
                        pool[i].in_use = true;
                        pool[i].thread = new Thread(new ParameterizedThreadStart(Answer));
                        pool[i].thread.Name = i.ToString();
                        pool[i].thread.Start(e.id);
                        processedCount++;
                        countBusyThreads++;
                        return;
                    }
                }
                rejectedCount++;
                if (writeToConsole) Console.WriteLine("Заявка #{0} отклонена", e.id);
            }
        }
        public void Answer(object? arg)
        {
            if (arg == null) throw new ArgumentNullException(nameof(arg));
            int id = (int)arg;
            if (writeToConsole) Console.WriteLine($"Заявка #{id} взята в обработку на потоке #{Thread.CurrentThread.Name}");
            Thread.Sleep(timeIntervalProcessingRequest);
            lock (threadLock)
            {
                for (int i = 0; i < pool.Length; i++)
                {
                    if (pool[i].thread == Thread.CurrentThread)
                    {
                        pool[i].in_use = false;
                        break;
                    }               
                }
                UpdateState();
                countBusyThreads--;
            }         
        }
        public void UpdateState()
        {
            double now = totalTime.ElapsedMilliseconds;
            double dt = now - lastChangeStateTime;
            totalBusyThreadsTime += countBusyThreads * dt;
            if (countBusyThreads == 0) totalDowntime += dt;
            lastChangeStateTime = now;
        }
        public Dictionary<string, double> CalculateExperimentalСoefficients()
        {           
            Dictionary<string, double> coefficientsDictionary = new Dictionary<string, double>();
            UpdateState();

            double probabilityDowntimeSystem = totalDowntime/totalTime.ElapsedMilliseconds; //P0
            double probabilityFailureSystem = (double)rejectedCount / requestCount; //Pn
            double relativeThroughput = (double)processedCount / requestCount; //Q
            double absoluteThroughput = (double)processedCount / totalTime.ElapsedMilliseconds; //A
            double averageCountBusyThreads = (double)totalBusyThreadsTime / totalTime.ElapsedMilliseconds; //k

            coefficientsDictionary.Add("Вероятность простоя системы", probabilityDowntimeSystem);
            coefficientsDictionary.Add("Вероятность отказа системы", probabilityFailureSystem);
            coefficientsDictionary.Add("Относительная пропускная способность системы", relativeThroughput);
            coefficientsDictionary.Add("Абсолютная пропускная способность системы", absoluteThroughput);
            coefficientsDictionary.Add("Среднее число занятых каналов системы", averageCountBusyThreads);

            return coefficientsDictionary;
        }
        public void WaitAllThreadsComplete()
        {
            for (int i = 0; i < pool.Length; i++)
            {
                if (pool[i].thread != null && pool[i].thread.IsAlive)
                {
                    pool[i].thread.Join();
                }
            }
            totalTime.Stop();
        }
    }
    class Client
    {
        private Server server;
        public Client(Server server)
        {
            this.server = server;
            this.request += server.proc;
        }
        public void send(int id)
        {
            procEventArgs args = new procEventArgs();
            args.id = id;
            OnProc(args);
        }
        protected virtual void OnProc(procEventArgs e)
        {
            EventHandler<procEventArgs> handler = request;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        public event EventHandler<procEventArgs> request;
    }
    public class procEventArgs : EventArgs
    {
        public int id { get; set; }
    }
    class SimulationQueuingSystem
    {
        public Server server;
        private Client client;
        public SimulationQueuingSystem(double intensivityClientsRequests, double intensivityServerMaintenance, int countServerThreads, int countClientRequests, bool writeToConsole)
        {
            int timeIntervaleSendingRequest = (int)(1 / intensivityClientsRequests);
            int timeIntervalProcessingRequest = (int)(1 / intensivityServerMaintenance);
            server = new Server(timeIntervalProcessingRequest, countServerThreads, writeToConsole);
            client = new Client(server);
            for (int requestID = 1; requestID <= countClientRequests; requestID++)
            {
                client.send(requestID);
                Thread.Sleep(timeIntervaleSendingRequest);
            }
            server.WaitAllThreadsComplete();
        }
    }
}