﻿namespace DstServerQuery.Models;

public class WorldLevelItem
{
    public string? Address { get; set; }

    public int Port { get; set; }

    public required string Id { get; set; }

    public string? SteamId { get; set; }
}