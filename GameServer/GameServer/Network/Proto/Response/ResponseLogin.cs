using ProtoBuf;
[ProtoContract]
public class ResponseLogin
{
    [ProtoMember(1)]
    public AccountInstance accountIns;
}