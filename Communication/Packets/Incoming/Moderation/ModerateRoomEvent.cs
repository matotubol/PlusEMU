using Plus.Communication.Packets.Outgoing.Navigator;
using Plus.Communication.Packets.Outgoing.Rooms.Settings;
using Plus.Database;
using Dapper;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Moderation;

internal class ModerateRoomEvent : IPacketEvent
{
    private readonly IRoomManager _roomManager;
    private readonly IDatabase _database;

    public ModerateRoomEvent(IRoomManager roomManager, IDatabase database)
    {
        _roomManager = roomManager;
        _database = database;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        if (!session.GetHabbo().GetPermissions().HasRight("mod_tool"))
            return Task.CompletedTask;
        if (!_roomManager.TryGetRoom(packet.ReadInt(), out var room))
            return Task.CompletedTask;
        var setLock = packet.ReadInt() == 1;
        var setName = packet.ReadInt() == 1;
        var kickAll = packet.ReadInt() == 1;
        if (setName)
        {
            room.Name = "Inappropriate to Hotel Management";
            room.Description = "Inappropriate to Hotel Management";
        }
        if (setLock)
            room.Access = RoomAccess.Doorbell;
        if (room.Tags.Count > 0)
            room.ClearTags();
        if (room.HasActivePromotion)
            room.EndPromotion();
        using (var connection = _database.Connection())
        {
            if (setName && setLock)
            {
                connection.Execute("UPDATE rooms SET caption = @caption, description = @description, tags = @tags, state = @state WHERE roomId = @roomId LIMIT 1", new {
                    roomId = room.RoomId,
                    caption = room.Name,
                    state = 1,
                    tags = "",
                    description = room.Description
                });
            }
            else if (setName)
            {
                connection.Execute("UPDATE rooms SET caption = @caption, description = @description, tags = @tags WHERE id = roomId LIMIT 1", new
                {
                    roomId = room.RoomId,
                    caption = room.Name,
                    description = room.Description,
                    tags = room.Tags
                });
            }
            else if (setLock)
                connection.Execute("UPDATE rooms SET state = 1, tags = '' WHERE id = @roomId LIMIT 1", new
                {
                    RoomId = room.RoomId,
                });
        }
        room.SendPacket(new RoomSettingsSavedComposer(room.RoomId));
        room.SendPacket(new RoomInfoUpdatedComposer(room.RoomId));
        if (kickAll)
        {
            foreach (var roomUser in room.GetRoomUserManager().GetUserList().ToList())
            {
                if (roomUser == null || roomUser.IsBot)
                    continue;
                if (roomUser.GetClient() == null || roomUser.GetClient().GetHabbo() == null)
                    continue;
                if (roomUser.GetClient().GetHabbo().Rank >= session.GetHabbo().Rank || roomUser.GetClient().GetHabbo().Id == session.GetHabbo().Id)
                    continue;
                room.GetRoomUserManager().RemoveUserFromRoom(roomUser.GetClient(), true);
            }
        }
        return Task.CompletedTask;
    }
}