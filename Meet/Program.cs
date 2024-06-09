using Meet.Controllers;
using Meet.services;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<ChatService>();
builder.Services.AddSingleton<WhiteboardController>(); // Register WhiteboardController as a singleton

builder.Services.AddSession();

var app = builder.Build();
// Enable WebSocket support
app.UseWebSockets();

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/whiteboard")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            Console.WriteLine("WebSocket request received");
            WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
            var whiteboardController = context.RequestServices.GetRequiredService<WhiteboardController>();
            await whiteboardController.HandleWebSocketConnection(webSocket);
        }
        else
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("WebSocket requests only");
        }
    }
    else
    {
        await next();
    }
});


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseSession(); // Add this line to enable session support

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
 pattern: "{controller=Chat}/{action=SignIn}/{id?}");
app.Run();
