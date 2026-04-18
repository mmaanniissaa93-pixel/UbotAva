using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UBot.Avalonia.Services;

// RuntimeStatus ────────────────────────────────────

public class RuntimeStatus
{
    [JsonPropertyName("botRunning")]       public bool    BotRunning       { get; set; }
    [JsonPropertyName("profile")]          public string  Profile          { get; set; } = "Default";
    [JsonPropertyName("server")]           public string  Server           { get; set; } = "Unknown";
    [JsonPropertyName("character")]        public string  Character        { get; set; } = "-";
    [JsonPropertyName("statusText")]       public string? StatusText       { get; set; }
    [JsonPropertyName("clientReady")]      public bool?   ClientReady      { get; set; }
    [JsonPropertyName("clientStarted")]    public bool?   ClientStarted    { get; set; }
    [JsonPropertyName("clientConnected")]  public bool?   ClientConnected  { get; set; }
    [JsonPropertyName("gatewayConnected")] public bool?   GatewayConnected { get; set; }
    [JsonPropertyName("agentConnected")]   public bool?   AgentConnected   { get; set; }
    [JsonPropertyName("referenceLoading")] public bool?   ReferenceLoading { get; set; }
    [JsonPropertyName("referenceLoaded")]  public bool?   ReferenceLoaded  { get; set; }
    [JsonPropertyName("selectedBotbase")]  public string? SelectedBotbase  { get; set; }
    [JsonPropertyName("connectionMode")]   public string? ConnectionMode   { get; set; }
    [JsonPropertyName("divisionIndex")]    public int?    DivisionIndex    { get; set; }
    [JsonPropertyName("gatewayIndex")]     public int?    GatewayIndex     { get; set; }
    [JsonPropertyName("player")]           public PlayerStats? Player      { get; set; }
}

public class PlayerStats
{
    [JsonPropertyName("name")]              public string? Name             { get; set; }
    [JsonPropertyName("level")]             public int?    Level            { get; set; }
    [JsonPropertyName("health")]            public long?   Health           { get; set; }
    [JsonPropertyName("maxHealth")]         public long?   MaxHealth        { get; set; }
    [JsonPropertyName("healthPercent")]     public double? HealthPercent    { get; set; }
    [JsonPropertyName("mana")]              public long?   Mana             { get; set; }
    [JsonPropertyName("maxMana")]           public long?   MaxMana          { get; set; }
    [JsonPropertyName("manaPercent")]       public double? ManaPercent      { get; set; }
    [JsonPropertyName("experiencePercent")] public double? ExperiencePercent{ get; set; }
    [JsonPropertyName("gold")]              public long?   Gold             { get; set; }
    [JsonPropertyName("skillPoints")]       public int?    SkillPoints      { get; set; }
    [JsonPropertyName("statPoints")]        public int?    StatPoints       { get; set; }
    [JsonPropertyName("inCombat")]          public bool?   InCombat         { get; set; }
    [JsonPropertyName("xOffset")]           public double? XOffset          { get; set; }
    [JsonPropertyName("yOffset")]           public double? YOffset          { get; set; }
}

// PluginDescriptor ──────────────────────────────────

public class PluginDescriptor
{
    [JsonPropertyName("id")]           public string Id          { get; set; } = "";
    [JsonPropertyName("title")]        public string Title       { get; set; } = "";
    [JsonPropertyName("enabled")]      public bool   Enabled     { get; set; }
    [JsonPropertyName("displayAsTab")] public bool   DisplayAsTab{ get; set; }
    [JsonPropertyName("index")]        public int    Index       { get; set; }
    [JsonPropertyName("iconKey")]      public string? IconKey    { get; set; }
}

// ConnectionOptions ────────────────────────────────────────────────

public class ConnectionOptions
{
    [JsonPropertyName("mode")]            public string Mode           { get; set; } = "clientless";
    [JsonPropertyName("divisionIndex")]   public int    DivisionIndex  { get; set; }
    [JsonPropertyName("gatewayIndex")]    public int    GatewayIndex   { get; set; }
    [JsonPropertyName("divisions")]       public List<ConnectionDivisionDto> Divisions { get; set; } = new();
    [JsonPropertyName("clientType")]      public string? ClientType    { get; set; }
    [JsonPropertyName("clientTypes")]     public List<ConnectionClientTypeDto>? ClientTypes { get; set; }
    [JsonPropertyName("referenceLoaded")] public bool?  ReferenceLoaded{ get; set; }
    [JsonPropertyName("referenceLoading")]public bool?  ReferenceLoading{ get; set; }
}

public class ConnectionDivisionDto
{
    [JsonPropertyName("index")]   public int    Index   { get; set; }
    [JsonPropertyName("name")]    public string Name    { get; set; } = "";
    [JsonPropertyName("servers")] public List<ConnectionServerDto> Servers { get; set; } = new();
}

public class ConnectionServerDto
{
    [JsonPropertyName("index")] public int    Index { get; set; }
    [JsonPropertyName("name")]  public string Name  { get; set; } = "";
}

public class ConnectionClientTypeDto
{
    [JsonPropertyName("id")]    public string Id    { get; set; } = "";
    [JsonPropertyName("name")]  public string Name  { get; set; } = "";
    [JsonPropertyName("value")] public int    Value { get; set; }
}

// ─── Plugin state ─────────────────────────────────────────────────────────────

public class PluginStateDto
{
    [JsonPropertyName("id")]      public string  Id      { get; set; } = "";
    [JsonPropertyName("enabled")] public bool    Enabled { get; set; }
    [JsonPropertyName("state")]   public JsonElement? State { get; set; }
}

// ─── Profiles ────────────────────────────────────────────────────────────────

public class ProfilesSnapshot
{
    [JsonPropertyName("selectedProfile")] public string         SelectedProfile { get; set; } = "Default";
    [JsonPropertyName("profiles")]        public List<string>   Profiles        { get; set; } = new();
    [JsonPropertyName("saveSelection")]   public bool?          SaveSelection   { get; set; }
}

// ─── Network config ───────────────────────────────────────────────────────────

public class NetworkConfig
{
    [JsonPropertyName("bindIp")] public string BindIp { get; set; } = "0.0.0.0";
    [JsonPropertyName("proxy")]  public ProxyConfig Proxy { get; set; } = new();
}

public class ProxyConfig
{
    [JsonPropertyName("active")]   public bool   Active   { get; set; }
    [JsonPropertyName("ip")]       public string Ip       { get; set; } = "";
    [JsonPropertyName("port")]     public int    Port     { get; set; }
    [JsonPropertyName("username")] public string Username { get; set; } = "";
    [JsonPropertyName("password")] public string Password { get; set; } = "";
    [JsonPropertyName("type")]     public string Type     { get; set; } = "SOCKS5";
    [JsonPropertyName("version")]  public int    Version  { get; set; } = 5;
}

public class AutoLoginAccountDto
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string SecondaryPassword { get; set; } = "";
    public byte Channel { get; set; } = 1;
    public string Type { get; set; } = "Joymax";
    public string ServerName { get; set; } = "";
    public string SelectedCharacter { get; set; } = "";
    public List<string> Characters { get; set; } = new();
}
