using DstQueryBot.Models;
using DstQueryBot.Services;

DstQueryService dst = new(new DstConfig(), null);

while (true)
{
    Console.Write(">>> ");
    string? input = Console.ReadLine();
    if (input == null) break;
    var r = await dst.HandleAsync("console", input);
    if (r?.Result is not null)
    {
        Console.WriteLine(r.Result);
    }
}

