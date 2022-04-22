﻿using Plus.Communication.Packets.Outgoing.Catalog;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Catalog;

public class GetCatalogPageEvent : IPacketEvent
{
    public void Parse(GameClient session, ClientPacket packet)
    {
        var pageId = packet.PopInt();
        packet.PopInt();
        var cataMode = packet.PopString();
        if (!PlusEnvironment.GetGame().GetCatalog().TryGetPage(pageId, out var page))
            return;
        if (!page.Enabled || !page.Visible || page.MinimumRank > session.GetHabbo().Rank || page.MinimumVip > session.GetHabbo().VipRank && session.GetHabbo().Rank == 1)
            return;
        session.SendPacket(new CatalogPageComposer(page, cataMode));
    }
}