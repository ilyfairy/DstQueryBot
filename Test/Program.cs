using Ilyfairy.DstQueryBot.ServerQuery;

namespace Test
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {

            ServerQueryManager manager = new();


            while (true)
            {
                Console.WriteLine(">>> ");
                string? input = Console.ReadLine();
                if (input == null) break;
                var r = await manager.Input("console", input+"\nDay 0");
                Console.WriteLine(r);
            }





        }
    }
}