namespace UBot.Core.Objects.Item;

public class RentInfo
{
    public uint Type { get; set; }
    public ushort CanDelete { get; set; }
    public ulong PeriodBeginTime { get; set; }
    public ulong PeriodEndTime { get; set; }
    public ushort CanRecharge { get; set; }
    public ulong MeterRateTime { get; set; }
    public ulong PackingTime { get; set; }
}
