using Server;
using ServerCore;
using System;
using System.Collections.Generic;
using System.Text;

class PacketHandler
{
	public static void C_ChatHandler(PacketSession session, IPacket packet)
	{
		C_Chat chatPacket = packet as C_Chat;
		ClientSession clientSession = session as ClientSession;

		if (clientSession.Room == null)
			return;
		
		//Action 주문서를 작성해서 그대로 전달해줌
		clientSession.Room.Push(
			() => clientSession.Room.Broadcast(clientSession, chatPacket.chat));
	}
}
