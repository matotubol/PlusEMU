﻿using Plus.Communication.Packets.Outgoing.Inventory.Furni;
using Plus.Communication.Packets.Outgoing.Inventory.Purse;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items;

namespace Plus.Communication.Packets.Incoming.Rooms.Furni;

internal class CreditFurniRedeemEvent : IPacketEvent
{
    public void Parse(GameClient session, ClientPacket packet)
    {
        if (!session.GetHabbo().InRoom)
            return;
        if (!PlusEnvironment.GetGame().GetRoomManager().TryGetRoom(session.GetHabbo().CurrentRoomId, out var room))
            return;
        if (!room.CheckRights(session, true))
            return;
        if (PlusEnvironment.GetSettingsManager().TryGetValue("room.item.exchangeables.enabled") != "1")
        {
            session.SendNotification("The hotel managers have temporarilly disabled exchanging!");
            return;
        }
        var exchange = room.GetRoomItemHandler().GetItem(packet.PopInt());
        if (exchange == null)
            return;
        if (exchange.Data.InteractionType != InteractionType.Exchange)
            return;
        var value = exchange.Data.BehaviourData;
        if (value > 0)
        {
            session.GetHabbo().Credits += value;
            session.SendPacket(new CreditBalanceComposer(session.GetHabbo().Credits));
        }
        using (var dbClient = PlusEnvironment.GetDatabaseManager().GetQueryReactor())
        {
            dbClient.SetQuery("DELETE FROM `items` WHERE `id` = @exchangeId LIMIT 1");
            dbClient.AddParameter("exchangeId", exchange.Id);
            dbClient.RunQuery();
        }
        session.SendPacket(new FurniListUpdateComposer());
        room.GetRoomItemHandler().RemoveFurniture(null, exchange.Id);
        session.GetHabbo().GetInventoryComponent().RemoveItem(exchange.Id);
    }
}