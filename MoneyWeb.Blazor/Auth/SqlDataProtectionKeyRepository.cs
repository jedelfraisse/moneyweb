using System.Xml.Linq;
using Dapper;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Data.SqlClient;

namespace MoneyWeb.Blazor.Auth;

/// <summary>
/// Persists Data Protection keys to the DataProtectionKeys table so auth cookies survive app restarts
/// (IIS app-pool recycles, deploys). The framework handles key rotation and expiry itself; this just
/// stores and returns the raw key XML.
/// </summary>
public class SqlDataProtectionKeyRepository(string connectionString) : IXmlRepository
{
    public IReadOnlyCollection<XElement> GetAllElements()
    {
        using var conn = new SqlConnection(connectionString);
        return conn.Query<string>("SELECT Xml FROM DataProtectionKeys ORDER BY Id")
            .Select(XElement.Parse)
            .ToList();
    }

    public void StoreElement(XElement element, string friendlyName)
    {
        using var conn = new SqlConnection(connectionString);
        conn.Execute("INSERT INTO DataProtectionKeys (FriendlyName, Xml) VALUES (@FriendlyName, @Xml)",
            new { FriendlyName = friendlyName, Xml = element.ToString(SaveOptions.DisableFormatting) });
    }
}
