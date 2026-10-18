using Microsoft.Extensions.AI;
using OllamaSharp;
using System.Diagnostics;

var builder = Host.CreateApplicationBuilder();

builder.Services.AddChatClient(
    new OllamaApiClient(new Uri("http://localhost:11434"), "qwen2.5vl:7b"));

var app = builder.Build();

var chatClient = app.Services.GetRequiredService<IChatClient>();

var options = new ChatOptions { Temperature = 0f };

const string prompt = """
    Extract data from this receipt image.
    Respond with ONLY a JSON object, no other text, in this structure:
    {
        "store": string,
        "date": string in YYYY-MM-DD format,
        "items": [
            {
                "name": string,
                "quantity": number,
                "unitPrice": number,
                "totalPrice": number
            }
        ],
        "total": number
    }
    Rules:
    - Copy names and numbers exactly as printed. Do not calculate or correct anything.
    - Numbers use a dot as the decimal separator.
    - If a field is not readable or not visible, use null. Do not guess.
    - Do not invent items that are not on the receipt.
    """;

var files = Directory.GetFiles("./receipts", "*.jpg");

if (files.Length > 0)
{
    Console.WriteLine("rozgrzewka");
    var warmup = new ChatMessage(ChatRole.User, prompt);
    warmup.Contents.Add(new DataContent(File.ReadAllBytes(files[0]), "image/jpeg"));
    _ = await chatClient.GetResponseAsync([warmup], options);
    Console.WriteLine("rozgrzewka zakonczona");
}

foreach (var file in files)
{
    var sw = Stopwatch.StartNew();

    var message = new ChatMessage(ChatRole.User, prompt);
    message.Contents.Add(
        new DataContent(
            File.ReadAllBytes(file),
            "image/jpeg"));

    var response = await chatClient.GetResponseAsync([message], options);
    sw.Stop();

    Console.WriteLine($"{Path.GetFileName(file)} ({sw.Elapsed.TotalSeconds:F1} s)");
    Console.WriteLine(response.Text);
}



