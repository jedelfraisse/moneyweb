var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql")
                 .AddDatabase("moneyweb");

builder.AddProject<Projects.MoneyWeb_Blazor>("moneyweb-blazor")
       .WithReference(sql)
       .WaitFor(sql);

builder.Build().Run();
