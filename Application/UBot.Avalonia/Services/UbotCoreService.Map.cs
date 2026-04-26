using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UBot.FileSystem;
using UBot.NavMeshApi;
using UBot.NavMeshApi.Dungeon;
using UBot.NavMeshApi.Edges;
using UBot.NavMeshApi.Extensions;
using UBot.NavMeshApi.Terrain;
using UBot.Core.Client.ReferenceObjects;
using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Event;
using UBot.Core.Extensions;
using UBot.Core.Network;
using UBot.Core.Network.Protocol;
using UBot.Core.Objects;
using UBot.Core.Objects.Party;
using UBot.Core.Objects.Spawn;
using UBot.Core.Objects.Skill;
using UBot.Core.Plugins;
using Forms = System.Windows.Forms;
using CoreRegion = UBot.Core.Objects.Region;

namespace UBot.Avalonia.Services;

internal sealed class UbotMapService : UbotServiceBase
{
    private static readonly object MapRenderSync = new();
    private static readonly Dictionary<string, (string minimap, string navmesh, string source, float width, float height)> MapRenderCache = new();
    private static string _mapLastRenderContextKey = string.Empty;
    private static int _mapLastImagePushTick = -1;
    private static int _mapAutoSelectUniqueLastTick = -1;

    internal Dictionary<string, object?> BuildMapPluginStateSnapshot()
    {
        Game.ReferenceManager?.EnsureMapInfoLoaded();
        return BuildMapPluginState();
    }

    internal bool TryResolveWalkDestination(Position source, double mapX, double mapY, out Position destination)
    {
        Game.ReferenceManager?.EnsureMapInfoLoaded();
        if (TryBuildMapRenderContext(source, out var renderContext)
            && TryProjectMapToPosition((float)mapX, (float)mapY, renderContext, source, out var projected))
        {
            destination = projected;
            return true;
        }

        if (!source.Region.IsDungeon && mapX >= 0 && mapX <= 1920 && mapY >= 0 && mapY <= 1920)
        {
            destination = new Position(
                source.Region,
                (float)Math.Clamp(mapX, 0, 1920),
                (float)Math.Clamp(mapY, 0, 1920),
                source.ZOffset);
            return true;
        }

        destination = new Position((float)mapX, (float)mapY, source.Region) { ZOffset = source.ZOffset };
        return true;
    }

    private readonly struct MapRenderContext
    {
        public MapRenderContext(CoreRegion playerRegion, byte centerX, byte centerY, string dungeonName, string floorName)
        {
            PlayerRegion = playerRegion;
            CenterX = centerX;
            CenterY = centerY;
            DungeonName = dungeonName ?? string.Empty;
            FloorName = floorName ?? string.Empty;
        }

        public CoreRegion PlayerRegion { get; }
        public byte CenterX { get; }
        public byte CenterY { get; }
        public string DungeonName { get; }
        public string FloorName { get; }
        public bool IsDungeon => PlayerRegion.IsDungeon;
        public string CacheKey => $"{PlayerRegion.Id}:{CenterX}:{CenterY}:{DungeonName}:{FloorName}";
    }

    private sealed class MapEntityState
    {
        public string Name { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
        public bool IsUnique { get; init; }
        public bool IsParty { get; init; }
        public int Level { get; init; }
        public double Distance { get; init; }
        public string Position { get; init; } = string.Empty;
        public double XOffset { get; init; }
        public double YOffset { get; init; }
    }

    private static Dictionary<string, object?> BuildMapPluginState()
    {
        var showFilter = NormalizeMapShowFilter(
            PlayerConfig.Get("UBot.Desktop.Map.ShowFilter",
                PlayerConfig.Get("UBot.Desktop.Map.EntityFilter", "All")));

        SpawnManager.TryGetEntities<SpawnedEntity>(out var spawnedEntities);
        var all = spawnedEntities?.Where(entity => entity != null).ToArray() ?? Array.Empty<SpawnedEntity>();
        var player = Game.Player;
        var playerPosition = player?.Position ?? default;
        var playerRegion = playerPosition.Region;

        var partyMemberNames = new HashSet<string>(
            (Game.Party?.Members ?? new List<PartyMember>())
                .Select(member => member?.Name ?? string.Empty)
                .Where(name => !string.IsNullOrWhiteSpace(name)),
            StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(player?.Name))
            partyMemberNames.Add(player.Name);

        var renderContext = default(MapRenderContext);
        var hasRenderContext = player != null && TryBuildMapRenderContext(playerPosition, out renderContext);

        var minimapImage = string.Empty;
        var navmeshImage = string.Empty;
        var imageSource = "none";
        var mapWidth = 1920f;
        var mapHeight = 1920f;
        var mapImageVersion = string.Empty;
        var playerMapX = playerPosition.XSectorOffset;
        var playerMapY = playerPosition.YSectorOffset;

        if (hasRenderContext)
        {
            var renderCache = GetOrCreateMapRenderCache(renderContext);
            mapImageVersion = renderContext.CacheKey;
            var hasRenderableImage = !string.IsNullOrWhiteSpace(renderCache.minimap) || !string.IsNullOrWhiteSpace(renderCache.navmesh);
            var includeImages = false;

            lock (MapRenderSync)
            {
                var contextChanged = !string.Equals(_mapLastRenderContextKey, renderContext.CacheKey, StringComparison.Ordinal);
                var elapsed = _mapLastImagePushTick < 0 ? int.MaxValue : Kernel.TickCount - _mapLastImagePushTick;
                if (elapsed < 0)
                    elapsed = int.MaxValue;

                includeImages = !hasRenderableImage || contextChanged || elapsed >= 1000;
                if (contextChanged)
                    _mapLastRenderContextKey = renderContext.CacheKey;
                if (includeImages && hasRenderableImage)
                    _mapLastImagePushTick = Kernel.TickCount;
            }

            if (includeImages)
            {
                minimapImage = renderCache.minimap;
                navmeshImage = renderCache.navmesh;
            }

            imageSource = renderCache.source;
            mapWidth = renderCache.width;
            mapHeight = renderCache.height;

            if (TryProjectPositionToMap(playerPosition, renderContext, out var projectedPlayerX, out var projectedPlayerY))
            {
                playerMapX = projectedPlayerX;
                playerMapY = projectedPlayerY;
            }
        }

        var rows = new List<MapEntityState>(all.Length);
        var players = 0;
        var party = 0;
        var monsters = 0;
        var npcs = 0;
        var cos = 0;
        var items = 0;
        var portals = 0;
        var uniques = 0;

        foreach (var entity in all)
        {
            if (entity == null)
                continue;

            switch (entity)
            {
                case SpawnedPlayer spawnedPlayer:
                    players++;
                    if (!string.IsNullOrWhiteSpace(spawnedPlayer.Name) && partyMemberNames.Contains(spawnedPlayer.Name))
                        party++;
                    break;
                case SpawnedMonster spawnedMonster:
                    monsters++;
                    if (IsUniqueMonster(spawnedMonster))
                        uniques++;
                    break;
                case SpawnedCos:
                    cos++;
                    break;
                case SpawnedNpc:
                    npcs++;
                    break;
                case SpawnedItem:
                    items++;
                    break;
                case SpawnedPortal:
                    portals++;
                    break;
            }

            var projectedX = entity.Position.XSectorOffset;
            var projectedY = entity.Position.YSectorOffset;
            if (hasRenderContext && TryProjectPositionToMap(entity.Position, renderContext, out var mapX, out var mapY))
            {
                projectedX = mapX;
                projectedY = mapY;
            }

            var distance = player != null ? player.Position.DistanceTo(entity.Position) : 0;
            if (!TryBuildMapEntityState(entity, projectedX, projectedY, partyMemberNames, distance, out var entry))
                continue;

            if (!MatchesMapFilter(showFilter, entry.Category, entry.IsUnique, entry.IsParty))
                continue;

            rows.Add(entry);
        }

        TryAutoSelectUniqueMonster(player, all);

        rows.Sort((left, right) => left.Distance.CompareTo(right.Distance));
        var payload = new List<object?>(rows.Count);
        foreach (var item in rows)
        {
            payload.Add(new Dictionary<string, object?>
            {
                ["name"] = item.Name,
                ["type"] = item.Type,
                ["category"] = item.Category,
                ["level"] = item.Level,
                ["position"] = item.Position,
                ["xOffset"] = item.XOffset,
                ["yOffset"] = item.YOffset
            });
        }

        return new Dictionary<string, object?>
        {
            ["showFilter"] = showFilter,
            ["total"] = all.Length,
            ["players"] = players,
            ["party"] = party,
            ["monsters"] = monsters,
            ["npcs"] = npcs,
            ["cos"] = cos,
            ["items"] = items,
            ["portals"] = portals,
            ["uniques"] = uniques,
            ["collisionDetection"] = Kernel.EnableCollisionDetection,
            ["autoSelectUniques"] = PlayerConfig.Get("UBot.Map.AutoSelectUnique", false),
            ["resetToPlayerAt"] = PlayerConfig.Get("UBot.Desktop.Map.ResetToPlayerAt", 0L),
            ["mapRegion"] = (int)playerRegion.Id,
            ["mapWidth"] = mapWidth,
            ["mapHeight"] = mapHeight,
            ["mapImageSource"] = imageSource,
            ["mapImageVersion"] = mapImageVersion,
            ["playerXOffset"] = Math.Round(playerMapX, 2),
            ["playerYOffset"] = Math.Round(playerMapY, 2),
            ["entities"] = payload,
            ["minimapImage"] = minimapImage,
            ["navmeshImage"] = navmeshImage
        };
    }

    private static bool TryBuildMapEntityState(
        SpawnedEntity entity,
        float mapX,
        float mapY,
        HashSet<string> partyMemberNames,
        double distance,
        out MapEntityState entry)
    {
        entry = new MapEntityState();
        var name = ResolveMapEntityName(entity);
        var category = ResolveMapEntityCategory(entity);
        var isUnique = entity is SpawnedMonster spawnedMonster && IsUniqueMonster(spawnedMonster);
        var isParty = entity is SpawnedPlayer spawnedPlayer
            && !string.IsNullOrWhiteSpace(spawnedPlayer.Name)
            && partyMemberNames.Contains(spawnedPlayer.Name);

        string type;
        var level = ResolveMapEntityLevel(entity);
        switch (entity)
        {
            case SpawnedMonster:
                type = isUnique ? "Unique" : "Monster";
                break;
            case SpawnedPlayer:
                type = isParty ? "Party" : "Player";
                break;
            case SpawnedCos:
                type = "COS";
                break;
            case SpawnedNpc:
                type = "NPC";
                break;
            case SpawnedItem:
                type = "Item";
                break;
            case SpawnedPortal:
                type = "Portal";
                break;
            default:
                return false;
        }

        entry = new MapEntityState
        {
            Name = name,
            Type = type,
            Category = category,
            IsUnique = isUnique,
            IsParty = isParty,
            Level = level,
            Distance = distance,
            Position = $"X:{entity.Position.X:0.0} Y:{entity.Position.Y:0.0}",
            XOffset = Math.Round(mapX, 2),
            YOffset = Math.Round(mapY, 2)
        };

        return true;
    }

    private static byte ResolveMapEntityLevel(SpawnedEntity entity)
    {
        return entity switch
        {
            SpawnedItem spawnedItem => spawnedItem.Record?.ReqLevel1 ?? 0,
            _ => entity.Record?.Level ?? 0
        };
    }

    private static bool IsUniqueMonster(SpawnedMonster monster)
    {
        return monster.Rarity is MonsterRarity.Unique
            or MonsterRarity.Unique2
            or MonsterRarity.UniqueParty
            or MonsterRarity.Unique2Party;
    }

    private static string ResolveMapEntityName(SpawnedEntity entity)
    {
        var rawName = entity switch
        {
            SpawnedPlayer spawnedPlayer => spawnedPlayer.Name,
            SpawnedMonster spawnedMonster => spawnedMonster.Record?.GetRealName(),
            SpawnedCos spawnedCos => spawnedCos.Name ?? spawnedCos.Record?.GetRealName(),
            SpawnedNpc spawnedNpc => spawnedNpc.Record?.GetRealName(),
            SpawnedItem spawnedItem => spawnedItem.Record?.GetRealName(true),
            SpawnedPortal spawnedPortal => spawnedPortal.Record?.GetRealName(),
            _ => entity.Record?.GetRealName()
        };

        return string.IsNullOrWhiteSpace(rawName) ? "<No name>" : rawName;
    }

    private static string ResolveMapEntityCategory(SpawnedEntity entity)
    {
        return entity switch
        {
            SpawnedPlayer => "Players",
            SpawnedMonster => "Monsters",
            SpawnedCos => "COS",
            SpawnedNpc => "NPC",
            SpawnedItem => "Items",
            SpawnedPortal => "Portals",
            _ => "All"
        };
    }

    private static bool MatchesMapFilter(string showFilter, string category, bool isUnique, bool isParty)
    {
        return showFilter switch
        {
            "Monsters" => string.Equals(category, "Monsters", StringComparison.OrdinalIgnoreCase),
            "Players" => string.Equals(category, "Players", StringComparison.OrdinalIgnoreCase),
            "Party" => isParty,
            "NPC" => string.Equals(category, "NPC", StringComparison.OrdinalIgnoreCase),
            "COS" => string.Equals(category, "COS", StringComparison.OrdinalIgnoreCase),
            "Items" => string.Equals(category, "Items", StringComparison.OrdinalIgnoreCase),
            "Portals" => string.Equals(category, "Portals", StringComparison.OrdinalIgnoreCase),
            "Uniques" => isUnique,
            _ => true
        };
    }

    private static string NormalizeMapShowFilter(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "All";

        return value.Trim().ToLowerInvariant() switch
        {
            "all" or "*" => "All",
            "monster" or "monsters" => "Monsters",
            "player" or "players" => "Players",
            "party" => "Party",
            "npc" or "npcs" => "NPC",
            "cos" => "COS",
            "item" or "items" => "Items",
            "portal" or "portals" => "Portals",
            "unique" or "uniques" => "Uniques",
            _ => "All"
        };
    }

    private static void TryAutoSelectUniqueMonster(Player? player, IReadOnlyCollection<SpawnedEntity> entities)
    {
        if (player == null || Kernel.Bot?.Running == true)
            return;

        if (!PlayerConfig.Get("UBot.Map.AutoSelectUnique", false))
            return;

        if (Kernel.TickCount - _mapAutoSelectUniqueLastTick < 1250)
            return;

        _mapAutoSelectUniqueLastTick = Kernel.TickCount;

        if (Game.SelectedEntity is SpawnedMonster selectedMonster && IsUniqueMonster(selectedMonster))
            return;

        var nearestUnique = entities
            .OfType<SpawnedMonster>()
            .Where(monster => monster.State.LifeState != LifeState.Dead && IsUniqueMonster(monster))
            .OrderBy(monster => monster.DistanceToPlayer)
            .FirstOrDefault();

        nearestUnique?.TrySelect();
    }

    private static bool TryBuildMapRenderContext(Position playerPosition, out MapRenderContext context)
    {
        context = default;
        if (playerPosition.Region.Id == 0)
            return false;

        var centerX = playerPosition.Region.IsDungeon
            ? playerPosition.GetSectorFromOffset(playerPosition.XOffset)
            : playerPosition.Region.X;
        var centerY = playerPosition.Region.IsDungeon
            ? playerPosition.GetSectorFromOffset(playerPosition.YOffset)
            : playerPosition.Region.Y;

        var dungeonName = string.Empty;
        var floorName = string.Empty;
        if (playerPosition.Region.IsDungeon)
        {
            dungeonName = UBot.Core.Client.RegionInfoManager.GetDungeonName(playerPosition.Region) ?? string.Empty;

            if (playerPosition.TryGetNavMeshTransform(out var playerTransform)
                && playerTransform.Instance is NavMeshInstBlock dungeonBlock
                && dungeonBlock.Parent is NavMeshDungeon dungeon
                && dungeon.FloorStringIDs.TryGetValue(dungeonBlock.FloorIndex, out var floor))
            {
                floorName = floor ?? string.Empty;
            }
        }

        context = new MapRenderContext(playerPosition.Region, centerX, centerY, dungeonName, floorName);
        return true;
    }

    private static (string minimap, string navmesh, string source, float width, float height) GetOrCreateMapRenderCache(MapRenderContext context)
    {
        lock (MapRenderSync)
        {
            if (MapRenderCache.TryGetValue(context.CacheKey, out var cached))
            {
                if (!string.IsNullOrWhiteSpace(cached.minimap) || !string.IsNullOrWhiteSpace(cached.navmesh))
                    return cached;

                MapRenderCache.Remove(context.CacheKey);
            }
        }

        var minimap = BuildMinimapImage(context, out var minimapSource);
        var navmesh = BuildNavMeshImage(context);
        var source = minimapSource;

        if (string.IsNullOrWhiteSpace(minimap) && !string.IsNullOrWhiteSpace(navmesh))
        {
            minimap = navmesh;
            source = "navmesh-fallback";
        }

        if (string.IsNullOrWhiteSpace(navmesh))
            navmesh = minimap;

        var created = (
            minimap,
            navmesh,
            source,
            MapSectorSpan * MapSectorGridSize,
            MapSectorSpan * MapSectorGridSize);

        if (string.IsNullOrWhiteSpace(created.minimap) && string.IsNullOrWhiteSpace(created.navmesh))
            return created;

        lock (MapRenderSync)
        {
            if (MapRenderCache.Count > 64)
                MapRenderCache.Clear();

            MapRenderCache[context.CacheKey] = created;
        }

        return created;
    }

    private static string BuildMinimapImage(MapRenderContext context, out string source)
    {
        if (Game.MediaPk2 == null)
        {
            source = "none";
            return string.Empty;
        }

        var side = MinimapSectorPixels * MapSectorGridSize;
        using var bitmap = new Bitmap(side, side, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Black);
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.SmoothingMode = SmoothingMode.HighQuality;

        var loadedSectorCount = 0;
        for (var x = 0; x < MapSectorGridSize; x++)
        {
            for (var z = 0; z < MapSectorGridSize; z++)
            {
                var sectorX = context.CenterX + x - 1;
                var sectorY = context.CenterY + z - 1;
                if (sectorX is < byte.MinValue or > byte.MaxValue || sectorY is < byte.MinValue or > byte.MaxValue)
                    continue;

                var sectorPath = GetMinimapFileName(new CoreRegion((byte)sectorX, (byte)sectorY), context.DungeonName, context.FloorName);
                if (!TryLoadImage(Game.MediaPk2, sectorPath, out var sectorImage))
                    continue;

                using (sectorImage)
                {
                    loadedSectorCount++;
                    var drawX = x * MinimapSectorPixels;
                    var drawY = (MapSectorGridSize - 1 - z) * MinimapSectorPixels;
                    graphics.DrawImage(sectorImage, drawX, drawY, MinimapSectorPixels, MinimapSectorPixels);
                }
            }
        }

        if (loadedSectorCount > 0)
        {
            source = "pk2-sectors";
            return ToPngDataUri(bitmap);
        }

        source = "navmesh-fallback";
        return BuildFallbackMinimapImage(context.PlayerRegion);
    }

    private static string GetMinimapFileName(CoreRegion region, string dungeonName, string floorName)
    {
        if (!string.IsNullOrWhiteSpace(dungeonName) && !string.IsNullOrWhiteSpace(floorName))
            return $"minimap_d\\{dungeonName}\\{floorName}_{region.X}x{region.Y}.ddj";

        return $"minimap\\{region.X}x{region.Y}.ddj";
    }

    private static string BuildFallbackMinimapImage(CoreRegion region)
    {
        if (!NavMeshManager.TryGetNavMeshTerrain(region.Id, out var terrain) || terrain == null)
            return string.Empty;

        var side = MinimapSectorPixels * MapSectorGridSize;
        using var bitmap = new Bitmap(side, side, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.FromArgb(8, 14, 22));

        var tileWidth = bitmap.Width / (float)NavMeshTerrain.TILES_X;
        var tileHeight = bitmap.Height / (float)NavMeshTerrain.TILES_Z;

        for (var z = 0; z < NavMeshTerrain.TILES_Z; z++)
        {
            for (var x = 0; x < NavMeshTerrain.TILES_X; x++)
            {
                var tile = terrain.GetTile(x, z);
                var color = ResolveFallbackMinimapTileColor(tile.TextureID, tile.IsBlocked);
                using var brush = new SolidBrush(color);
                graphics.FillRectangle(
                    brush,
                    x * tileWidth,
                    z * tileHeight,
                    Math.Max(1f, tileWidth + 0.4f),
                    Math.Max(1f, tileHeight + 0.4f));
            }
        }

        using var gridPen = new Pen(Color.FromArgb(30, 180, 210, 255), 1f);
        for (var step = 0; step <= 6; step++)
        {
            var px = step * (bitmap.Width / 6f);
            graphics.DrawLine(gridPen, px, 0, px, bitmap.Height);
            graphics.DrawLine(gridPen, 0, px, bitmap.Width, px);
        }

        return ToPngDataUri(bitmap);
    }

    private static string BuildNavMeshImage(MapRenderContext context)
    {
        var side = NavMeshSectorPixels * MapSectorGridSize;
        using var bitmap = new Bitmap(side, side, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.FromArgb(4, 8, 12));
        graphics.SmoothingMode = SmoothingMode.HighSpeed;
        graphics.PixelOffsetMode = PixelOffsetMode.HighSpeed;

        var mapSpan = MapSectorSpan * MapSectorGridSize;
        var scaleX = bitmap.Width / mapSpan;
        var scaleY = bitmap.Height / mapSpan;
        DrawNavMeshGrid(graphics, scaleX, scaleY);

        if (context.IsDungeon)
        {
            if (!NavMeshManager.TryGetNavMesh(context.PlayerRegion.Id, out var navMesh) || navMesh == null)
                return string.Empty;

            if (navMesh is NavMeshDungeon dungeon)
                DrawDungeonEdges(graphics, dungeon, context, scaleX, scaleY);
            else if (navMesh is NavMeshTerrain terrain)
            {
                DrawTerrainEdges(graphics, terrain, context, scaleX, scaleY);
                DrawTerrainObjectEdges(graphics, terrain, context, scaleX, scaleY);
            }
        }
        else
        {
            for (var rz = context.CenterY - 1; rz <= context.CenterY + 1; rz++)
            {
                for (var rx = context.CenterX - 1; rx <= context.CenterX + 1; rx++)
                {
                    if (rx is < byte.MinValue or > byte.MaxValue || rz is < byte.MinValue or > byte.MaxValue)
                        continue;

                    var rid = new UBot.NavMeshApi.Mathematics.RID((byte)rx, (byte)rz);
                    if (!NavMeshManager.TryGetNavMeshTerrain(rid, out var terrain) || terrain == null)
                        continue;

                    foreach (var edge in terrain.GlobalEdges)
                    {
                        try
                        {
                            edge.Link();
                        }
                        catch
                        {
                            // ignored
                        }
                    }

                    DrawTerrainEdges(graphics, terrain, context, scaleX, scaleY);
                    DrawTerrainObjectEdges(graphics, terrain, context, scaleX, scaleY);
                }
            }
        }

        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return $"data:image/png;base64,{Convert.ToBase64String(stream.ToArray())}";
    }

    private static void DrawNavMeshGrid(Graphics graphics, float scaleX, float scaleY)
    {
        var mapSpan = MapSectorSpan * MapSectorGridSize;
        using var majorPen = new Pen(Color.FromArgb(125, 0, 255, 64), 1.1f);
        using var minorPen = new Pen(Color.FromArgb(65, 0, 120, 70), 1f);

        var minorStep = MapSectorSpan / 4f;
        for (var offset = 0f; offset <= mapSpan; offset += minorStep)
        {
            var x = offset * scaleX;
            var y = offset * scaleY;
            graphics.DrawLine(minorPen, x, 0, x, mapSpan * scaleY);
            graphics.DrawLine(minorPen, 0, y, mapSpan * scaleX, y);
        }

        for (var offset = 0f; offset <= mapSpan; offset += MapSectorSpan)
        {
            var x = offset * scaleX;
            var y = offset * scaleY;
            graphics.DrawLine(majorPen, x, 0, x, mapSpan * scaleY);
            graphics.DrawLine(majorPen, 0, y, mapSpan * scaleX, y);
        }
    }

    private static void DrawTerrainEdges(
        Graphics graphics,
        NavMeshTerrain terrain,
        MapRenderContext context,
        float scaleX,
        float scaleY)
    {
        using var blockedPen = new Pen(Color.FromArgb(220, 255, 70, 70), 1.15f);
        using var railingPen = new Pen(Color.FromArgb(220, 70, 160, 255), 1.1f);
        using var openPen = new Pen(Color.FromArgb(210, 90, 255, 120), 1.05f);
        using var globalPen = new Pen(Color.FromArgb(210, 255, 180, 64), 1.1f);

        foreach (var edge in terrain.InternalEdges)
            DrawProjectedNavMeshEdge(
                graphics,
                terrain.Region.X,
                terrain.Region.Z,
                edge.Line.Min.X,
                edge.Line.Min.Z,
                edge.Line.Max.X,
                edge.Line.Max.Z,
                ResolveEdgePen(edge.IsBlocked, edge.IsRailing, blockedPen, railingPen, openPen),
                context,
                scaleX,
                scaleY);

        foreach (var edge in terrain.GlobalEdges)
            DrawProjectedNavMeshEdge(
                graphics,
                terrain.Region.X,
                terrain.Region.Z,
                edge.Line.Min.X,
                edge.Line.Min.Z,
                edge.Line.Max.X,
                edge.Line.Max.Z,
                edge.IsBlocked ? blockedPen : globalPen,
                context,
                scaleX,
                scaleY);
    }

    private static void DrawTerrainObjectEdges(
        Graphics graphics,
        NavMeshTerrain terrain,
        MapRenderContext context,
        float scaleX,
        float scaleY)
    {
        using var blockedPen = new Pen(Color.FromArgb(220, 255, 90, 90), 1f);
        using var railingPen = new Pen(Color.FromArgb(220, 85, 170, 255), 1f);
        using var openPen = new Pen(Color.FromArgb(170, 120, 255, 130), 1f);

        foreach (var instance in terrain.Instances)
        {
            if (instance?.NavMeshObj == null)
                continue;

            foreach (var edge in instance.NavMeshObj.InternalEdges)
            {
                var worldLine = instance.LocalToWorld.MultiplyLine(edge.Line);
                DrawProjectedNavMeshEdge(
                    graphics,
                    terrain.Region.X,
                    terrain.Region.Z,
                    worldLine.Min.X,
                    worldLine.Min.Z,
                    worldLine.Max.X,
                    worldLine.Max.Z,
                    ResolveEdgePen(edge.IsBlocked, edge.IsRailing, blockedPen, railingPen, openPen),
                    context,
                    scaleX,
                    scaleY);
            }

            foreach (var edge in instance.NavMeshObj.GlobalEdges)
            {
                var worldLine = instance.LocalToWorld.MultiplyLine(edge.Line);
                DrawProjectedNavMeshEdge(
                    graphics,
                    terrain.Region.X,
                    terrain.Region.Z,
                    worldLine.Min.X,
                    worldLine.Min.Z,
                    worldLine.Max.X,
                    worldLine.Max.Z,
                    ResolveEdgePen(edge.IsBlocked, edge.IsRailing, blockedPen, railingPen, openPen),
                    context,
                    scaleX,
                    scaleY);
            }
        }
    }

    private static void DrawDungeonEdges(
        Graphics graphics,
        NavMeshDungeon dungeon,
        MapRenderContext context,
        float scaleX,
        float scaleY)
    {
        using var blockedPen = new Pen(Color.FromArgb(220, 255, 90, 90), 1f);
        using var railingPen = new Pen(Color.FromArgb(220, 85, 170, 255), 1f);
        using var openPen = new Pen(Color.FromArgb(170, 120, 255, 130), 1f);

        foreach (var block in dungeon.Blocks)
        {
            if (block?.NavMeshObj == null)
                continue;

            foreach (var edge in block.NavMeshObj.InternalEdges)
            {
                var worldLine = block.LocalToWorld.MultiplyLine(edge.Line);
                DrawProjectedNavMeshEdge(
                    graphics,
                    context.CenterX,
                    context.CenterY,
                    worldLine.Min.X,
                    worldLine.Min.Z,
                    worldLine.Max.X,
                    worldLine.Max.Z,
                    ResolveEdgePen(edge.IsBlocked, edge.IsRailing, blockedPen, railingPen, openPen),
                    context,
                    scaleX,
                    scaleY);
            }

            foreach (var edge in block.NavMeshObj.GlobalEdges)
            {
                var worldLine = block.LocalToWorld.MultiplyLine(edge.Line);
                DrawProjectedNavMeshEdge(
                    graphics,
                    context.CenterX,
                    context.CenterY,
                    worldLine.Min.X,
                    worldLine.Min.Z,
                    worldLine.Max.X,
                    worldLine.Max.Z,
                    ResolveEdgePen(edge.IsBlocked, edge.IsRailing, blockedPen, railingPen, openPen),
                    context,
                    scaleX,
                    scaleY);
            }
        }
    }

    private static bool TryProjectPositionToMap(Position position, MapRenderContext context, out float mapX, out float mapY)
    {
        var sectorX = position.Region.IsDungeon
            ? position.GetSectorFromOffset(position.XOffset)
            : position.Region.X;
        var sectorY = position.Region.IsDungeon
            ? position.GetSectorFromOffset(position.YOffset)
            : position.Region.Y;

        return TryProjectCoordinatesToMap(sectorX, sectorY, position.XSectorOffset, position.YSectorOffset, context, out mapX, out mapY);
    }

    private static bool TryProjectCoordinatesToMap(
        byte sectorX,
        byte sectorY,
        float localX,
        float localY,
        MapRenderContext context,
        out float mapX,
        out float mapY)
    {
        var startX = context.CenterX - 1;
        var startY = context.CenterY - 1;
        var sectorSpan = MapSectorSpan * MapSectorGridSize;

        mapX = (sectorX - startX) * MapSectorSpan + localX;
        mapY = sectorSpan - ((sectorY - startY) * MapSectorSpan + localY);
        return true;
    }

    private static bool TryProjectMapToPosition(
        float mapX,
        float mapY,
        MapRenderContext context,
        Position playerPosition,
        out Position destination)
    {
        destination = default;

        if (!float.IsFinite(mapX) || !float.IsFinite(mapY))
            return false;

        var sectorSpan = MapSectorSpan * MapSectorGridSize;
        var clampedMapX = Math.Clamp(mapX, 0f, sectorSpan);
        var clampedMapY = Math.Clamp(mapY, 0f, sectorSpan);

        var startX = context.CenterX - 1;
        var startY = context.CenterY - 1;
        var projectedY = sectorSpan - clampedMapY;

        var sectorOffsetX = (int)Math.Floor(clampedMapX / MapSectorSpan);
        var sectorOffsetY = (int)Math.Floor(projectedY / MapSectorSpan);
        sectorOffsetX = Math.Clamp(sectorOffsetX, 0, MapSectorGridSize - 1);
        sectorOffsetY = Math.Clamp(sectorOffsetY, 0, MapSectorGridSize - 1);

        var localX = clampedMapX - sectorOffsetX * MapSectorSpan;
        var localY = projectedY - sectorOffsetY * MapSectorSpan;
        var targetSectorX = startX + sectorOffsetX;
        var targetSectorY = startY + sectorOffsetY;

        if (context.IsDungeon)
        {
            var dungeonXOffset = (targetSectorX - 128) * MapSectorSpan + localX;
            var dungeonYOffset = (targetSectorY - 128) * MapSectorSpan + localY;
            destination = new Position(playerPosition.Region, dungeonXOffset, dungeonYOffset, playerPosition.ZOffset);
            return true;
        }

        if (targetSectorX is < byte.MinValue or > byte.MaxValue || targetSectorY is < byte.MinValue or > byte.MaxValue)
            return false;

        destination = new Position((byte)targetSectorX, (byte)targetSectorY, localX, localY, playerPosition.ZOffset);
        return true;
    }

    private static void DrawProjectedNavMeshEdge(
        Graphics graphics,
        byte sectorX,
        byte sectorY,
        float x1,
        float y1,
        float x2,
        float y2,
        Pen pen,
        MapRenderContext context,
        float scaleX,
        float scaleY)
    {
        if (!TryProjectCoordinatesToMap(sectorX, sectorY, x1, y1, context, out var mapX1, out var mapY1))
            return;
        if (!TryProjectCoordinatesToMap(sectorX, sectorY, x2, y2, context, out var mapX2, out var mapY2))
            return;

        graphics.DrawLine(pen, mapX1 * scaleX, mapY1 * scaleY, mapX2 * scaleX, mapY2 * scaleY);
    }

    private static Pen ResolveEdgePen(bool isBlocked, bool isRailing, Pen blockedPen, Pen railingPen, Pen openPen)
    {
        if (isBlocked)
            return blockedPen;
        if (isRailing)
            return railingPen;
        return openPen;
    }

    private static bool TryLoadImage(IFileSystem fileSystem, string path, out Image image)
    {
        image = null;
        if (!fileSystem.TryGetFile(path, out var file) || file == null)
            return false;

        try
        {
            if (path.EndsWith(".ddj", StringComparison.OrdinalIgnoreCase))
            {
                image = file.ToImage();
                return image != null;
            }

            using var stream = file.OpenRead().GetStream();
            using var raw = Image.FromStream(stream);
            image = new Bitmap(raw);
            return true;
        }
        catch
        {
            image = null;
            return false;
        }
    }

    private static Color ResolveFallbackMinimapTileColor(short textureId, bool blocked)
    {
        var seed = textureId;
        var red = 28 + Math.Abs((seed * 73) % 96);
        var green = 42 + Math.Abs((seed * 37) % 120);
        var blue = 18 + Math.Abs((seed * 29) % 80);

        if (blocked)
        {
            red = (int)(red * 0.42f);
            green = (int)(green * 0.38f);
            blue = (int)(blue * 0.35f);
        }

        return Color.FromArgb(255, red, green, blue);
    }

    private static string ToPngDataUri(Image image)
    {
        using var memory = new MemoryStream();
        image.Save(memory, ImageFormat.Png);
        return $"data:image/png;base64,{Convert.ToBase64String(memory.ToArray())}";
    }


    public Task<IReadOnlyList<MapLocationDto>> GetMapLocationsAsync()
    {
        Game.ReferenceManager?.EnsureMapInfoLoaded();
        var list = new List<MapLocationDto>();
        var teleports = Game.ReferenceManager?.OptionalTeleports;
        if (teleports != null)
        {
            foreach (var entry in teleports.Values)
            {
                if (entry == null || entry.Service == 0 || entry.ID <= 0) continue;
                
                var regionText = entry.Region.ToString();
                var name = Game.ReferenceManager?.GetTranslation(regionText) ?? regionText;

                list.Add(new MapLocationDto
                {
                    Id = entry.ID,
                    Name = name
                });
            }
        }

        return Task.FromResult((IReadOnlyList<MapLocationDto>)list.OrderBy(x => x.Name).ToList());
    }
}

