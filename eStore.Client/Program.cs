using eStore.Client;
using eStore.Client.Services.OrderService;
using eStore.Client.Services.PaymentService;
using eStore.Client.Services.ProductService;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

//Base address from configuration, Configure HttpClient for server communication
var serverBaseAddress = builder.Configuration["ServerBaseAddress"]
    ?? throw new InvalidOperationException("Missing config: ServerBaseAddress");
builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = new Uri(serverBaseAddress)
});
//------------------------------------------
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

await builder.Build().RunAsync();
