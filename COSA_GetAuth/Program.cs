// See https://aka.ms/new-console-template for more information


using COSA_GetAuth.Coastal.Helpers;
using COSA_GetAuth.Services;
using Microsoft.Extensions.Configuration;

//----------------------------------------------------------
//config
var builder = new ConfigurationBuilder();

//IConfiguration Configuration = new ConfigurationBuilder()
//                                            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
//                                            .AddEnvironmentVariables()
//                                            .AddCommandLine(args)     
//                                            .Build();

builder.SetBasePath(Directory.GetCurrentDirectory());
builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.AddCommandLine(args);
IConfiguration config = builder.Build();

ConfigHelper.Initialise(config);

//----------------------------------------------------------
// services

XeroAuthService xeroAuthService = new XeroAuthService();

if (args.Length > 0)
{
    switch (args[0].ToLower())
    {
        case "refresh":
            xeroAuthService.RefreshTokens();
            break;

        case "disconnect":
            xeroAuthService.DisconnectTenant();
            break;

    }
}
else
{
    xeroAuthService.GetCode();
    xeroAuthService.GetTokens();
}
