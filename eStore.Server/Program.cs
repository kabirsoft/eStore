using eStore.Server.Data;
using eStore.Server.Services;
using eStore.Server.Services.OrderService;
using eStore.Server.Services.PaymentOrchestrator;
using eStore.Server.Services.ProductService;
using Microsoft.EntityFrameworkCore;
using eStore.Server.Payments.Vipps;
using eStore.Server.Payments.PayPal;

var builder = WebApplication.CreateBuilder(args);

//--------- Database --------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DbConnection")));
//--------- End Database --------

//--------- Vipps Configuration --------
builder.Services.AddMemoryCache();

// Bind options
var vippsOpt = new VippsOptions();
builder.Configuration.GetSection("Vipps").Bind(vippsOpt);
builder.Services.AddSingleton(vippsOpt);

// Vipps HttpClients
builder.Services.AddHttpClient<VippsAccessTokenService>(c =>
{
    c.BaseAddress = new Uri(vippsOpt.BaseUrl);
});

builder.Services.AddHttpClient<VippsEpaymentClient>(c =>
{
    c.BaseAddress = new Uri(vippsOpt.BaseUrl);
});
//----------------

//---------- PayPal Configuration --------
var payPalOpt = new PayPalOptions();
builder.Configuration.GetSection("PayPal").Bind(payPalOpt);
builder.Services.AddSingleton(payPalOpt);

// PayPal HttpClients
builder.Services.AddHttpClient<PayPalAccessTokenService>(c =>
{
    c.BaseAddress = new Uri(payPalOpt.BaseUrl);
});
builder.Services.AddHttpClient<PayPalCheckoutClient>(c =>
{
    c.BaseAddress = new Uri(payPalOpt.BaseUrl);
});

//---------------------------------------

//------ Add services to the container. --------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer(); // Swagger (OpenAPI) + Swagger UI
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPaymentProvider, StripePaymentProvider>(); // Stripe payment provider

if (vippsOpt.IsConfigured)
{
    builder.Services.AddScoped<IPaymentProvider, VippsPaymentProvider>(); // Vipps payment provider
}
if (payPalOpt.IsConfigured)
{
    builder.Services.AddScoped<IPaymentProvider, PayPalPaymentProvider>();
}

builder.Services.AddScoped<IPaymentOrchestrator, PaymentOrchestrator>();

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
