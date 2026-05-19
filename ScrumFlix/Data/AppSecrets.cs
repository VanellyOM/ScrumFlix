using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;

public static class AppSecrets
{
    private static readonly IConfigurationRoot Config =
        new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("secrets.json", optional: false)
            .Build();

    public static string DbConnection =>
        Config["ConnectionStrings:DefaultDb"]!;

    public static string GmailPassword =>
        Config["Email:GmailPassword"]!;

    public static string DbPassword =>
        Config["DBPassword:DefaultDbPassword"]!;
}