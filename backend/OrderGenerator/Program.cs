using Microsoft.AspNetCore.Builder;
using OrderGenerator.Clients.Interfaces;
using OrderGenerator.Clients;
using QuickFix.Logger;
using QuickFix.Store;
using QuickFix.Transport;
using QuickFix;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod());
});


// FIX settings
var settings = new SessionSettings(@"FIX/generator.cfg");
builder.Services.AddSingleton(settings);

// Instância única do QuickFixClient
var quickFixClient = new QuickFixClient();
builder.Services.AddSingleton(quickFixClient); // usado pelo initiator
builder.Services.AddSingleton<IQuickFixClient>(sp => sp.GetRequiredService<QuickFixClient>());
builder.Services.AddSingleton<IApplication>(sp => sp.GetRequiredService<QuickFixClient>());

// Store e log
builder.Services.AddSingleton<IMessageStoreFactory>(sp => new FileStoreFactory(settings));
builder.Services.AddSingleton<ILogFactory>(sp => new FileLogFactory(settings));

// Iniciador do FIX
builder.Services.AddSingleton<SocketInitiator>(sp =>
    new SocketInitiator(
        sp.GetRequiredService<IApplication>(),
        sp.GetRequiredService<IMessageStoreFactory>(),
        settings,
        sp.GetRequiredService<ILogFactory>()
    )
);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Start FIX
var initiator = app.Services.GetRequiredService<SocketInitiator>();
initiator.Start();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
