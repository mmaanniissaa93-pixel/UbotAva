namespace UBot.Core.Objects;

public interface IPositionRuntimeContext
{
    double DistanceToPlayer(Position position);
    bool HasCollisionBetween(Position source, Position destination);
}
