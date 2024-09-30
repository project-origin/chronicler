using ProjectOrigin.Chronicler;
using ProjectOrigin.ServiceCommon;

await new ServiceApplication<Startup>()
    .ConfigureMigration("--migrate")
    .ConfigureWebApplication("--serve")
    .RunAsync(args);
