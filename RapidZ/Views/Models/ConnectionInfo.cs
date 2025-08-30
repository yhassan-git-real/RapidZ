namespace RapidZ.Views.Models;

public class ConnectionInfo
{
    public string ServerName { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
}
