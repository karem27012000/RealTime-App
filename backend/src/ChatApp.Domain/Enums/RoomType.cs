namespace ChatApp.Domain.Enums;

public enum RoomType
{
    /// <summary>A one-to-one chat between exactly two users.</summary>
    Private = 0,

    /// <summary>A named room that any user can join.</summary>
    Group = 1
}
