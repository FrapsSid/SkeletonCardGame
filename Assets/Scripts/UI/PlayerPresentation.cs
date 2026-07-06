using UnityEngine;

public sealed class PlayerPresentation
{
    public PlayerPresentation(
        Skeleton player,
        string displayName,
        Sprite avatarSprite,
        Color teamColor,
        int playerIndex,
        bool isLocalPlayer,
        bool hasNetworkClientId,
        ulong networkClientId)
    {
        Player = player;
        DisplayName = displayName;
        AvatarSprite = avatarSprite;
        TeamColor = teamColor;
        PlayerIndex = playerIndex;
        IsLocalPlayer = isLocalPlayer;
        HasNetworkClientId = hasNetworkClientId;
        NetworkClientId = networkClientId;
    }

    public Skeleton Player { get; }
    public string DisplayName { get; }
    public Sprite AvatarSprite { get; }
    public Color TeamColor { get; }
    public int PlayerIndex { get; }
    public bool IsLocalPlayer { get; }
    public bool HasNetworkClientId { get; }
    public ulong NetworkClientId { get; }
}
