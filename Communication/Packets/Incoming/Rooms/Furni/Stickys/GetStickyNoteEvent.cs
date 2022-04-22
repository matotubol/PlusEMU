﻿using Plus.Communication.Packets.Outgoing.Rooms.Furni.Stickys;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;

namespace Plus.Communication.Packets.Incoming.Rooms.Furni.Stickys;

internal class GetStickyNoteEvent : IPacketEvent
{
    public void Parse(GameClient session, ClientPacket packet)
    {
        if (!session.GetHabbo().InRoom)
            return;
        if (!PlusEnvironment.GetGame().GetRoomManager().TryGetRoom(session.GetHabbo().CurrentRoomId, out var room))
            return;
        var item = room.GetRoomItemHandler().GetItem(packet.PopInt());
        if (item == null || item.GetBaseItem().InteractionType != InteractionType.Postit)
            return;
        session.SendPacket(new StickyNoteComposer(item.Id.ToString(), item.ExtraData));
    }
}