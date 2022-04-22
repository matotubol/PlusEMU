﻿using Plus.Communication.Packets.Outgoing.Rooms.Permissions;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Rooms.Action;

internal class RemoveMyRightsEvent : IPacketEvent
{
    public void Parse(GameClient session, ClientPacket packet)
    {
        if (!session.GetHabbo().InRoom)
            return;
        if (!PlusEnvironment.GetGame().GetRoomManager().TryGetRoom(session.GetHabbo().CurrentRoomId, out var room))
            return;
        if (!room.CheckRights(session, false))
            return;
        if (room.UsersWithRights.Contains(session.GetHabbo().Id))
        {
            var user = room.GetRoomUserManager().GetRoomUserByHabbo(session.GetHabbo().Id);
            if (user != null && !user.IsBot)
            {
                user.RemoveStatus("flatctrl 1");
                user.UpdateNeeded = true;
                user.GetClient().SendPacket(new YouAreNotControllerComposer());
            }
            using (var dbClient = PlusEnvironment.GetDatabaseManager().GetQueryReactor())
            {
                dbClient.SetQuery("DELETE FROM `room_rights` WHERE `user_id` = @uid AND `room_id` = @rid LIMIT 1");
                dbClient.AddParameter("uid", session.GetHabbo().Id);
                dbClient.AddParameter("rid", room.Id);
                dbClient.RunQuery();
            }
            if (room.UsersWithRights.Contains(session.GetHabbo().Id))
                room.UsersWithRights.Remove(session.GetHabbo().Id);
        }
    }
}