using System.Globalization;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLocalization();
builder.Services.AddRazorPages()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();
var apiBase = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5266";
builder.Services.AddHttpClient("Api", client =>
{
    client.BaseAddress = new Uri(apiBase);
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));
});

var app = builder.Build();
var supportedCultures = new[] { "sr-RS", "sr" };
var cultureInfos = supportedCultures.Select(c => new CultureInfo(c)).ToList();

var locOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("sr-RS"),
    SupportedCultures = cultureInfos,
    SupportedUICultures = cultureInfos
};
app.UseRequestLocalization(locOptions);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();

app.Run();
