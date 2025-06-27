namespace OTPBUILD.Models;

public class ItemEvent
{
    public string Puuid { get; set; }
    public string EventType { get; set; }
    public int ItemId { get; set; }
    public long Timestamp { get; set; }

    public ItemEvent(string puuid, string eventType, int itemId, long timestamp)
    {
        Puuid = puuid;
        EventType = eventType;
        ItemId = itemId;
        Timestamp = timestamp;
    }
}
