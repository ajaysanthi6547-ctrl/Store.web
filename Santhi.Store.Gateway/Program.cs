
var builder = WebApplication.CreateBuilder(args);

// Register YARP Reverse Proxy
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(
        builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Forward incoming requests to backend servers
app.MapReverseProxy();

app.Run();
