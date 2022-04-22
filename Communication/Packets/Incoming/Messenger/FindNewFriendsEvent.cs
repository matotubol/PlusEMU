﻿using Plus.Communication.Packets.Outgoing.Messenger;
using Plus.Communication.Packets.Outgoing.Rooms.Session;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Messenger;

internal class FindNewFriendsEvent : IPacketEvent
{
    public void Parse(GameClient session, ClientPacket packet)
    {
        var instance = PlusEnvironment.GetGame().GetRoomManager().TryGetRandomLoadedRoom();
        if (instance != null)
        {
            session.SendPacket(new FindFriendsProcessResultComposer(true));
            session.SendPacket(new RoomForwardComposer(instance.Id));
        }
        else
            session.SendPacket(new FindFriendsProcessResultComposer(false));
    }
}