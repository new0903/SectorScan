using Grpc.Core;
using Grpc.Net.Client;

namespace ConsoleAppGRPCTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            Console.ReadLine();

            Task.Run(ReceiveServerStream);
            Console.ReadLine();
        }

        public static  async Task ReceiveServerStream()
        {
            using var channel = GrpcChannel.ForAddress("https://localhost:62385");//63608:8081⁠
            var client = new StationServiceGrpc.StationServiceGrpcClient(channel);
            var request = new RequestParamsForArea
            {
                Network=NetworkStandardGRPC.Gsm,
                OperatorCode="250001",
                Step=1.8,
                Coordinates=new Location() 
                {
                    LatS= 53,//38
                    LatE=54,//81
                    LonS=50,//20
                    LonE=52//70
                }
            };

            using var call = client.ScanAreaStream(request);

            await foreach (var response in call.ResponseStream.ReadAllAsync())
            {
                Console.WriteLine($"Получено:  Network={response.Network}, OperatorCode={response.OperatorCode}, CountAdded={response.CountAdded}, CountSectors={response.CountSectors}," +
                    $" ScannedSector={response.ScannedSector}," +
                    $"Message={response.Message}, Time={response.Timestamp}");
                if (response.IsDone)
                {
                    Console.WriteLine($"Получено: все");
                }
            }
        }
    }
}
