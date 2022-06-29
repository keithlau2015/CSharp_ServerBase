using ProtoBuf;

[ProtoContract]
public class ServerStatus
{
    public enum Status : int 
    {
        //Standard Status
        standard = 0,
        crowd = 1,
        //Override Status
        recommanded = 2,
        offline = 3,
        maintenance = 4,
    }

    [ProtoMember(1)]
    public int ID { get; private set; }
    [ProtoMember(2)]
    public string Name { get; private set; }
    [ProtoMember(3)]
    public int CurStatus { get; private set; }

    public ServerStatus(int id, string name, int status)
    {
        this.ID = id;
        this.Name = name;
        this.CurStatus = status;
    }

    public void UpdateStatus(Status status)
    {
        this.CurStatus = (int)status;
    }
}
