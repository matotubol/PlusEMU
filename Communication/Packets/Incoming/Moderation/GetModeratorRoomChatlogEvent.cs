﻿using System;
using System.Collections.Generic;
using System.Data;
using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms.Chat.Logs;

namespace Plus.Communication.Packets.Incoming.Moderation;

internal class GetModeratorRoomChatlogEvent : IPacketEvent
{
    public void Parse(GameClient session, ClientPacket packet)
    {
        if (session == null || session.GetHabbo() == null)
            return;
        if (!session.GetHabbo().GetPermissions().HasRight("mod_tool"))
            return;
        packet.PopInt(); //junk
        var roomId = packet.PopInt();
        if (!PlusEnvironment.GetGame().GetRoomManager().TryGetRoom(roomId, out var room)) return;
        PlusEnvironment.GetGame().GetChatManager().GetLogs().FlushAndSave();
        var chats = new List<ChatlogEntry>();
        using (var dbClient = PlusEnvironment.GetDatabaseManager().GetQueryReactor())
        {
            dbClient.SetQuery("SELECT * FROM `chatlogs` WHERE `room_id` = @id ORDER BY `id` DESC LIMIT 100");
            dbClient.AddParameter("id", roomId);
            var data = dbClient.GetTable();
            if (data != null)
            {
                foreach (DataRow row in data.Rows)
                {
                    var habbo = PlusEnvironment.GetHabboById(Convert.ToInt32(row["user_id"]));
                    if (habbo != null) chats.Add(new ChatlogEntry(Convert.ToInt32(row["user_id"]), roomId, Convert.ToString(row["message"]), Convert.ToDouble(row["timestamp"]), habbo));
                }
            }
        }
        session.SendPacket(new ModeratorRoomChatlogComposer(room, chats));
    }
}