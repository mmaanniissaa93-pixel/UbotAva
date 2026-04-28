using System;

namespace UBot.Core.Objects;

public struct Movement
{
    /// <summary>
    ///     Gets or sets the type.
    /// </summary>
    public MovementType Type;

    /// <summary>
    ///     Gets or sets the type.
    /// </summary>
    public bool Moving;

    /// <summary>
    ///     Gets or sets the has destination.
    /// </summary>
    public bool HasDestination;

    /// <summary>
    ///     Gets or sets the destination.
    /// </summary>
    public Position Destination;

    /// <summary>
    ///     Gets or sets the has source.
    /// </summary>
    public bool HasSource;

    /// <summary>
    ///     Gets or sets the source.
    /// </summary>
    public Position Source;

    /// <summary>
    ///     Gets or sets the has angle.
    /// </summary>
    public bool HasAngle;

    /// <summary>
    ///     Gets or sets the angle.
    /// </summary>
    public float Angle;

    internal double MovingX,
        MovingY;
    internal TimeSpan RemainingTime;

}
