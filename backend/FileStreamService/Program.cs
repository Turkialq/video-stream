using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


var allowedOrigins = builder.Configuration.GetSection("AllowedHosts").Get<string[]>() 
                     ?? Array.Empty<string>();


builder.Services.AddCors(options =>
{
    options.AddPolicy("MyCorsPolicy", policy =>
    {
        policy
            .WithOrigins(allowedOrigins) 
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddControllers();

var app = builder.Build();

app.UseCors("MyCorsPolicy");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}



var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapGet("/preview/{id}", async (int id, HttpContext context) =>
{
    // Some logic to locate the correct preview image file
    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "previewImgs", $"preview{id}.jpg");
    
    if (!File.Exists(filePath))
    {
        context.Response.StatusCode = 404;
        return;
    }

    context.Response.ContentType = "image/jpeg";
    await context.Response.SendFileAsync(filePath);
});


app.MapGet("/video", async (HttpContext context) =>
{
  
    // 1) Make sure the Range header is present
    string rangeHeader = context.Request.Headers["Range"];
    if (string.IsNullOrEmpty(rangeHeader))
    {
        
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("Requires Range header");
        return;
    }

    // 2) Get the path and size of the video
    //    Make sure "bigbuck.mp4" exists at this path or provide a full path.
    var videoPath = Path.Combine(Directory.GetCurrentDirectory(), "media/GP2.mp4");
    var fileInfo  = new FileInfo(videoPath);
    long videoSize = fileInfo.Length;

    // 3) Parse the Range header
    //    Typically "bytes=12345-" 
    //    We only take the start for simplicity in this example
    const int chunkSize = 1_000_000; // 1 MB chunk
    var bytesUnit = "bytes=";
    var rangeValue = rangeHeader.Replace(bytesUnit, "").Split('-');
    
    // If the range header is malformed or missing, handle it
    if (!long.TryParse(rangeValue[0], out long start))
    {
        context.Response.StatusCode = StatusCodes.Status416RangeNotSatisfiable;
        return;
    }

    // 4) Calculate the end of the chunk
    long end = Math.Min(start + chunkSize, videoSize - 1);
    long contentLength = end - start + 1;

    // 5) Set the response headers for Partial Content
    context.Response.StatusCode = (int)HttpStatusCode.PartialContent;
    context.Response.Headers["Content-Range"] = $"bytes {start}-{end}/{videoSize}";
    context.Response.Headers["Accept-Ranges"] = "bytes";
    context.Response.Headers["Content-Length"] = contentLength.ToString();
    context.Response.ContentType = "video/mp4";

    // 6) Write out the chunk
    using var stream = new FileStream(videoPath, FileMode.Open, FileAccess.Read, FileShare.Read);
    stream.Seek(start, SeekOrigin.Begin);

    // We'll create a buffer the size of our chunk and read from the file
    byte[] buffer = new byte[contentLength];
    await stream.ReadAsync(buffer, 0, (int)contentLength);
    await context.Response.Body.WriteAsync(buffer, 0, (int)contentLength);

    // And that's it—the partial chunk is returned
});

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
