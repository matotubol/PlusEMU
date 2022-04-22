﻿using Plus.Communication.Packets.Outgoing.Navigator.New;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Navigator;

internal class InitializeNewNavigatorEvent : IPacketEvent
{
    public void Parse(GameClient session, ClientPacket packet)
    {
        var topLevelItems = PlusEnvironment.GetGame().GetNavigator().GetTopLevelItems();
        session.SendPacket(new NavigatorMetaDataParserComposer(topLevelItems));
        session.SendPacket(new NavigatorLiftedRoomsComposer());
        session.SendPacket(new NavigatorCollapsedCategoriesComposer());
        session.SendPacket(new NavigatorPreferencesComposer());
    }
}