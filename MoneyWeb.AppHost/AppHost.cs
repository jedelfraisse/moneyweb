var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddConnectionString("moneyweb");

builder.AddProject<Projects.MoneyWeb_Blazor>("moneyweb-blazor")
       .WithReference(sql);

builder.Build().Run();
