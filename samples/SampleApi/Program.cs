//using Slck.Envelope.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

//app.UseApiEnvelope();

//// GET example
//app.MapGet("/sample/{id}", (string id) =>
//{
//    var ticket = id == "123" ? new { Id = id, Title = "Sample Ticket" } : null;
//    return ticket is null
//        ? Envelope.NotFound($"Ticket '{id}' not found")
//        : Envelope.Ok(ticket);
//});
