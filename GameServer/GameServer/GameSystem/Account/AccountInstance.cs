using System;
using ProtoBuf;

[ProtoContract]
public class AccountInstance
{
    [ProtoMember(1)]
    public string UID { get; private set; }
    [ProtoMember(2)]
    public string Nickname { get; private set; }
    [ProtoMember(3)]
    public string Password { get; private set; }
    [ProtoMember(4)]
    public long CreateTime { get; private set; }
    [ProtoMember(5)]
    public long LastLoginTime { get; private set; }
    [ProtoMember(6)]
    public string AvatarIcon { get; private set; }
    [ProtoMember(7)]
    public string AvatarFrame { get; private set; }

    public AccountInstance(string nickName, string password, string defaultAvatarIcon, string defaultAvatarFrame)
    {
        this.UID = Guid.NewGuid().ToString();
        this.Nickname = nickName;
        this.Password = password;
        this.CreateTime = TimeManager.singleton.GetCurrentUnixtimestamp();
        this.LastLoginTime = TimeManager.singleton.GetCurrentUnixtimestamp();
        this.AvatarIcon = defaultAvatarIcon;
        this.AvatarFrame = defaultAvatarFrame;
    }

    public void Login()
    {
        this.LastLoginTime = TimeManager.singleton.GetCurrentUnixtimestamp();
    }
}
