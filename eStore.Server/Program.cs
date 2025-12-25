using eStore.Server.Data;
using eStore.Server.Services.OrderService;
using eStore.Server.Services.ProductService;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

//--------- Database --------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DbConnection")));
//--------- End Database --------

//------ Add services to the container. --------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer(); // Swagger (OpenAPI) + Swagger UI
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
// ---------------------------------------
//--------- CORS --------
var clientBaseUrl = builder.Configuration["ClientBaseUrl"];

if (string.IsNullOrWhiteSpace(clientBaseUrl))
    throw new InvalidOperationException("Missing config: ClientBaseUrl");

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient", policy =>
    {
        policy.WithOrigins(clientBaseUrl!)
              .AllowAnyHeader()
              .AllowAnyMethod();              
    });
});
//--------- End CORS --------

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{    
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowBlazorClient");

app.UseAuthorization();

app.MapControllers();

app.Run();
