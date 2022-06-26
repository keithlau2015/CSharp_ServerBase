using ProtoBuf;

[ProtoContract]
public class ServerStatus
{
    public enum Status : int 
    {
        offline = -1,
        standard = 0,
        crowd = 1,
        recommanded = 2,
        maintenance = 3,
    }

    [ProtoMember(1)]
    public int ID { get; private set; }
    [ProtoMember(2)]
    public string Name { get; private set; }
    [ProtoMember(3)]
    public int CurStatus { get; private set; }
    [ProtoMember(4)]
    public long Unixtimestamp { get; private set; }

    public ServerStatus(int id, string name, int status, long unixtimestamp)
    {
        this.ID = id;
        this.Name = name;
        this.CurStatus = status;
        this.Unixtimestamp = unixtimestamp;
    }

    public void UpdateStatus(Status status)
    {
        this.CurStatus = (int)status;
    }
}
