using System;

namespace UBot.Protocol.Handlers.Agent.Entity;

[Flags]
public enum EntityUpdateStatusFlag : byte
{
    None = 0,
    HP = 1,
    MP = 2,
    HPMP = HP | MP,
    BadEffect = 4,
    Fellow = 13,
}

