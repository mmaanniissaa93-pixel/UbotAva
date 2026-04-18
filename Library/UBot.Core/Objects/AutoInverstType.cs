using System;

namespace UBot.Core.Objects;

[Flags]
public enum AutoInverstType : byte
{
    None = 0,
    Beginner = 1,
    Helpful = 2,
}
