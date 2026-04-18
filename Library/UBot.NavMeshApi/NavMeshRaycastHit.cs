using System.Numerics;
using UBot.NavMeshApi.Cells;
using UBot.NavMeshApi.Edges;
using UBot.NavMeshApi.Mathematics;

namespace UBot.NavMeshApi;

public class NavMeshRaycastHit
{
    public RID Region { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 World => this.Region.Position + this.Position;
    public NavMeshEdge? Edge { get; set; }
    public NavMeshCell Cell { get; set; }
    public NavMeshInst Instance { get; set; }
    public NavMesh NavMesh => this.Cell?.NavMesh;

    public override string ToString() => $"{this.NavMesh} -> [{this.Cell}; {this.Edge}; {this.Instance}]";
}
