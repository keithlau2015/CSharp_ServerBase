using ProtoBuf;

[ProtoContract]
public class RequestLogin
{
    [ProtoMember(1)]
    public string LoginName { get; private set; }
    [ProtoMember(2)]
    public string Password { get; private set; }
}
