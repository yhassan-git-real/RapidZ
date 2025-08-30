using System;

namespace RapidZ.Views.Models;

public class ConnectionInfo
{
    public string ServerName { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
    public string ResponseTime { get; set; } = "0"; // Response time in milliseconds
    public DateTime? LastConnectionTime { get; set; }
    public string StatusMessage { get; set; } = string.Empty;
}
