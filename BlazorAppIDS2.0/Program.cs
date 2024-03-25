using BlazorAppIDS;
using BlazorAppIDS.Services;
using BlazorAppIDS2._0.Components;
using BlazorAppIDS2._0.Components.Account;
using BlazorAppIDS2._0.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Duende.IdentityServer.Models;
using Duende.IdentityServer;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

var apiScopes = builder.Configuration.GetSection("ApiScopes").Get<List<ApiScope>>();
var clients = builder.Configuration.GetSection("Clients").Get<List<Client>>();
var identityResources = builder.Configuration.GetSection("IdentityResources").Get<List<IdentityResource>>();
var apiResources = builder.Configuration.GetSection("ApiResources").Get<List<ApiResource>>();

// Adding IdentityServer service to the app
builder.Services.AddIdentityServer(options =>
    {
        options.Events.RaiseErrorEvents = true;
        options.Events.RaiseInformationEvents = true;
        options.Events.RaiseFailureEvents = true;
        options.Events.RaiseSuccessEvents = true;

        // see https://docs.duendesoftware.com/identityserver/v6/fundamentals/resources/
        options.EmitStaticAudienceClaim = true;
    })
    .AddInMemoryClients(clients)
    .AddInMemoryApiResources(apiResources)
    .AddInMemoryApiScopes(apiScopes)
    .AddInMemoryIdentityResources(identityResources)
    .AddAspNetIdentity<ApplicationUser>()
    .AddProfileService<MyProfileService>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    await SeedData.EnsureSeedData(app);
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// seed the database before starting the app
var scope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();
var context = scope.ServiceProvider.GetService<ApplicationDbContext>();
context.Database.Migrate();

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

// adding IdentityServer middlewarez
app.UseIdentityServer();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();