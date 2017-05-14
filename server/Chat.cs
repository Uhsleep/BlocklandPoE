if(isObject(GlobalChatGroup))
	GlobalChatGroup.delete();
new Simset(GlobalChatGroup) { color = "<color:FFA500>"; };

if(isObject(TradeChatGroup))
	TradeChatGroup.delete();
new Simset(TradeChatGroup) { color = "<color:FF0000>"; };

function serverCmdLeaveChat(%client, %chat)
{
	switch(%chat)
	{
		case 0:
			if(GlobalChatGroup.isMember(%client))
			{
				GlobalChatGroup.remove(%client);
				messageCllient(%client, '', "<color:FFFFFF>You have been removed from the global chat");
			}
		case 1:
			if(TradeChatGroup.isMember(%client))
			{
				TradeChatGroup.remove(%client);
				messageClient(%client, '', "<color:FFFFFF>You have been removed from the trade chat");
			}
	}
}

package POE_Chat
{
	function serverCmdMessageSent(%client, %message)
	{
		if(!%client.poeEnabled)
			return parent::serverCmdMessageSent(%client, %message);
		
		if(getSimTime() - %client.lastMessageTime < $POE::Chat::Timeout)
			return;
		
		%client.lastMessageTime = getSimTime();
		
		%message = stripMLControlChars(%message);
		
		%firstChar = getSubStr(%message, 0, 1);
		%white = "<color:FFFFFF>";
		
		if(%firstChar $= "$") // trade
		{
			%message = trim(getSubStr(%message, 1, strLen(%message)));
			if(!strLen(%message))
				return;
			
			if(!TradeChatGroup.isMember(%client))
			{
				messageClient(%client, '', TradeChatGroup.color @ "You have joined the trade chat.");
				TradeChatGroup.add(%client);
			}
			
			for(%i = 0; %i < TradeChatGroup.getCount(); %i++)
			{
				%cl = TradeChatGroup.getObject(%i);
				messageClient(%cl, '', TradeChatGroup.color @ %client.name @ ": " @ %white @ %message);
			}
		}
		else if(%firstChar $= "%") // party
		{
			%message = trim(getSubStr(%message, 1, strLen(%message)));
			if(!strLen(%message))
				return;
			
			if(!(%party = %client.party))
			{
				messageClient(%client, '', "<color:FFFFFF>You are not in a party.");
				return;
			}
			
			for(%i = 0; %i < %party.getCount(); %i++)
			{
				%cl = %party.getObject(%i);
				echo(%message);
				messageClient(%cl, '', "<color:104E8B>" @ %client.name @ ": " @ %white @ %message);
			}
		}
		else if(%firstChar $= "@") // whisper
		{
			%name = trim(strReplace(getWord(%message, 0), "@", ""));
			if(!strLen(%name))
				return;
			
			if(%cl = findClientByName(%name))
			{
				%message = trim(getWords(%message, 1, getWordCount(%message) - 1));
				if(!strLen(%message))
					return;
				
				messageClient(%client, '', "<color:8A2BE2>To " @ %cl.name @ ": " @ %white @ %message);
				messageClient(%cl, '', "<color:8A2BE2>From " @ %client.name @ ": " @ %white @ %message);
			}
		}
		else if(%firstChar $= "&") // guild
		{
			
		}
		else if(%firstChar $= "#") // global
		{
			%message = trim(getSubStr(%message, 1, strLen(%message)));
			if(!strLen(%message))
				return;
			
			if(!GlobalChatGroup.isMember(%client))
			{
				messageClient(%client, '', GlobalChatGroup.color @ "You have joined the global chat.");
				GlobalChatGroup.add(%client);
			}
			
			for(%i = 0; %i < GlobalChatGroup.getCount(); %i++)
			{
				%cl = GlobalChatGroup.getObject(%i);
				messageClient(%cl, '', GlobalChatGroup.color @ %client.name @ ": " @ %white @ %message);
			}
		}
		else if(%firstChar $= "\\") // temporary
			return parent::serverCmdMessageSent(%client, %message);
		else // instance chat
		{		
			%color = "<color:66DC00>";
			for(%i = 0; %i < ClientGroup.getCount(); %i++)
			{
				%cl = ClientGroup.getObject(%i);
				
				if(!%cl.poeEnabled || %cl.currentArea == %client.currentArea)
					messageClient(%cl, '', %color @ %client.name @ ": " @ %white @ %message);
			}
		}
	}
};
activatePackage(POE_Chat);