namespace Messaging.Settings;

public class AsbConnectionSettings
{
    public Topic Topic { get; set; }
}

public class Topic
{
    public required string Name { get; set; }
    public required Connection Connection { get; set; }
    public required string Subscription { get; set; }
}

public class Connection
{

    public required string Listen { get; set; }
    public required string Send { get; set; }
}