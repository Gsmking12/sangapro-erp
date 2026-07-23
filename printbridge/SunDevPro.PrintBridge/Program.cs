using System.Net;
using System.Text;
using System.Text.Json;
using System.Drawing;
using System.Drawing.Printing;

const int Port = 17823;
const string Prefix = "http://+:17823/";

var listener = new HttpListener();
listener.Prefixes.Add(Prefix);
listener.Start();
Console.WriteLine($"SunDevPro PrintBridge actif sur le port {Port}");

while (true)
{
    var context = await listener.GetContextAsync();
    _ = Task.Run(() => HandleAsync(context));
}

static async Task HandleAsync(HttpListenerContext context)
{
    try
    {
        AddCors(context.Response);
        if (context.Request.HttpMethod == "OPTIONS")
        {
            context.Response.StatusCode = 204;
            context.Response.Close();
            return;
        }

        var path = context.Request.Url?.AbsolutePath.Trim('/').ToLowerInvariant() ?? "";
        if (path == "health")
        {
            await JsonAsync(context.Response, new
            {
                ok = true,
                machine = Environment.MachineName,
                service = "SunDevProPrintBridge",
                version = "1.0.23"
            });
            return;
        }

        if (path == "printers")
        {
            var defaultPrinter = new PrinterSettings().PrinterName;
            var printers = PrinterSettings.InstalledPrinters.Cast<string>()
                .OrderBy(x => x)
                .Select(name => new
                {
                    name,
                    isDefault = string.Equals(name, defaultPrinter, StringComparison.OrdinalIgnoreCase)
                })
                .ToArray();

            await JsonAsync(context.Response, new
            {
                ok = true,
                machine = Environment.MachineName,
                defaultPrinter,
                printers
            });
            return;
        }

        if (path == "print" && context.Request.HttpMethod == "POST")
        {
            using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding ?? Encoding.UTF8);
            var body = await reader.ReadToEndAsync();
            var job = JsonSerializer.Deserialize<PrintJob>(body, JsonOptions()) ?? throw new InvalidOperationException("Travail invalide.");
            if (string.IsNullOrWhiteSpace(job.Content)) throw new InvalidOperationException("Contenu vide.");

            var printer = string.IsNullOrWhiteSpace(job.PrinterName)
                ? new PrinterSettings().PrinterName
                : job.PrinterName.Trim();

            using var document = new PrintDocument();
            document.PrinterSettings.PrinterName = printer;
            document.PrinterSettings.Copies = (short)Math.Clamp(job.Copies, 1, 20);
            if (!document.PrinterSettings.IsValid) throw new InvalidOperationException($"Imprimante invalide : {printer}");

            document.DocumentName = string.IsNullOrWhiteSpace(job.Title) ? "SunDevPro" : job.Title.Trim();
            document.PrintPage += (_, e) =>
            {
                using var font = new Font("Segoe UI", 10f);
                e.Graphics?.DrawString(job.Content, font, Brushes.Black, e.MarginBounds);
                e.HasMorePages = false;
            };
            document.Print();

            await JsonAsync(context.Response, new { ok = true, printer, machine = Environment.MachineName });
            return;
        }

        context.Response.StatusCode = 404;
        await JsonAsync(context.Response, new { ok = false, message = "Route introuvable." });
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = 500;
        await JsonAsync(context.Response, new { ok = false, message = ex.Message });
    }
}

static void AddCors(HttpListenerResponse response)
{
    response.Headers["Access-Control-Allow-Origin"] = "*";
    response.Headers["Access-Control-Allow-Headers"] = "Content-Type";
    response.Headers["Access-Control-Allow-Methods"] = "GET,POST,OPTIONS";
}

static JsonSerializerOptions JsonOptions() => new(JsonSerializerDefaults.Web)
{
    PropertyNameCaseInsensitive = true
};

static async Task JsonAsync(HttpListenerResponse response, object value)
{
    response.ContentType = "application/json; charset=utf-8";
    var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions());
    response.ContentLength64 = bytes.Length;
    await response.OutputStream.WriteAsync(bytes);
    response.Close();
}

internal sealed class PrintJob
{
    public string? PrinterName { get; set; }
    public string? Title { get; set; }
    public string Content { get; set; } = "";
    public int Copies { get; set; } = 1;
}
