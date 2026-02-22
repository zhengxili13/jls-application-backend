using System;

namespace JLSApplicationBackend.hubs;

public class Message
{
    public int clientuniqueid { get; set; }
    public string type { get; set; }
    public string message { get; set; }
    public DateTime date { get; set; }

    public int? fromUser { get; set; }
    public int? toUser { get; set; }
}