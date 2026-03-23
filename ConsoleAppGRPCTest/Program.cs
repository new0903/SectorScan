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
            using var channel = GrpcChannel.ForAddress("http://localhost:6622");//63608:8081⁠
            var client = new StationServiceGrpc.StationServiceGrpcClient(channel);
            var request = new RequestParamsForArea
            {
                Network=NetworkStandardGRPC.Gsm,
                OperatorCode="250001",
                Step=6.0,
                Coordinates=new Location() 
                {
                    LatS= 38,//38
                    LatE= 81,//81
                    LonS= 20,//20
                    LonE= 180//70
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
        public static async Task ReceiveFullServerStream()
        {
            using var channel = GrpcChannel.ForAddress("http://localhost:6622");//63608:8081⁠
            var client = new StationServiceGrpc.StationServiceGrpcClient(channel);
            var request = new Empty();

            using var call = client.SyncStationsAll(request);

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
