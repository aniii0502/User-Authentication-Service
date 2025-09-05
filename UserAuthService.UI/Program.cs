using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using UserAuthService.UI.Services;
using Blazored.LocalStorage;
using UserAuthService.UI;
using Microsoft.AspNetCore.Components.Authorization;



var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");

// Register HTTP client and services
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:5284/") });
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddBlazoredLocalStorage();
// Add this line before builder.Build()
builder.Services.AddOptions();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();


await builder.Build().RunAsync();
