using OrderGenerator.Clients.Interfaces;
using OrderGenerator.Clients;
using QuickFix;
using QuickFix.Logger;
using QuickFix.Store;
using QuickFix.Transport;
using OrderAccumulator.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Configuração do FIX
var settings = new SessionSettings(@"FIX/generator.cfg");
builder.Services.AddSingleton(settings);

var application = new QuickFixOrderService(); 
var storeFactory = new FileStoreFactory(settings);
var logFactory = new FileLogFactory(settings);
var initiator = new SocketInitiator(application, storeFactory, settings, logFactory);

builder.Services.AddSingleton(initiator);
builder.Services.AddSingleton<IApplication>(application);

var sessionID = settings.GetSessions().First();
builder.Services.AddSingleton(sessionID);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IQuickFixClient, QuickFixClient>();

var app = builder.Build();

var fixInitiator = app.Services.GetRequiredService<SocketInitiator>();
fixInitiator.Start();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
