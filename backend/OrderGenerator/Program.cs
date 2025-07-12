using OrderGenerator.Clients.Interfaces;
using OrderGenerator.Clients;
using QuickFix;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var settings = new SessionSettings("generator.cfg");
builder.Services.AddSingleton(settings);

var sessionID = settings.GetSessions().First(); 
builder.Services.AddSingleton(sessionID);

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();



builder.Services.AddSingleton<IQuickFixClient, QuickFixClient>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
