function GameConnection::joinArea(%this, %area, %spawnIndex)
{
	if(!isObject(%player = %this.player) || !isObject(%area))
		return;
	
	if(%spawnIndex $= "")
		%spawnIndex = 0;
	
	%currentArea = %this.currentArea;
	
	if(isObject(%currentArea))
	{
			%currentArea.leave(%this);
			%this.instances.add(%currentArea);
	}
	
	if(%this.instances.isMember(%area))
		%this.instances.remove(%area);
	
	%this.currentArea = %area;
	%this.currentArea.join(%this);
	%player.setTransform(vectorAdd(%area.spawnLocation[%spawnIndex], "0 0 0.2"));
}

function GameConnection::loop(%this)
{
	cancel(%this.loopSchedule);
	
	if(!%this.poeEnabled)
		return;
	
	if(%player = %this.player | 0)
	{
		if(vectorLen(%player.getVelocity()) > 3 && %this.bankOpen)
		{
			commandToClient(%client, 'POE_CloseInventory');
			%this.bankOpen = false;
		}
	}
	
	%this.loopSchedule = %this.schedule(50, loop);
}

function doIt(%client)
{
	%cam = %client.camera;
	%cam.setflymode();
	%client.setcontrolobject(%cam);
	
	%cam.setorbitmode(%client.player, %client.player.position SPC eulerToAxis("-70 0 -15"), -20, 0, 0, true);
	//-75 0 30
	%cam.setControlObject(%client.player);
	%client.bottomPrint(" ", 1, true); // To prevent the "1 observer(s) thing"
	
	commandToClient(%client, 'openOverlay');
	commandToClient(%client, 'getRes');
	
	%fov = %client.getControlCameraFov();
	%client.player.changeDatablock(PlayerPathOfExile);
	%client.setControlCameraFov(%fov);
}

package POE
{
	function GameConnection::onConnectRequest(%this, %ip, %lan, %net, %pre, %suf, %blp, %rtb, %modules, %un2, %rsc, %ex1, %ex2, %ex3, %ex4)
	{
		%this.hasClient = false;
		for(%i = 0; %i < getFieldCount(%modules); %i++)
		{
			%module = getField(%modules, %i);

			if(getWord(%module, 0) $= "POE")
			{
				%this.hasClient = true;
				%this.clientVersion = getWord(%module, 1);
				//talk(%module);
				break;
			}
		}

		return parent::onConnectRequest(%this, %ip, %lan, %net, %pre, %suf, %blp, %rtb, %modules, %un2, %rsc, %ex1, %ex2, %ex3, %ex4);
	}

	function GameConnection::autoAdminCheck(%this, %b, %c, %d, %e)
	{
		%p = parent::autoAdminCheck(%this, %b, %c, %d, %e);
		
		// check if they have updated client
		if(!%this.hasClient)
			messageClient(%this, '', "\c6You do not have the client.");
		else if(%this.clientVersion < $POE::clientVersion)
			messageClient(%this, '', "\c6You have an outdated client");

		%instances = new SimSet(Instances_ @ %this.bl_id);
		%this.instances = %instances;
		
		commandtoclient(%this, 'getRes');

		// load profile
		%this.loadProfile();
		
		return %p;
	}
	
	function GameConnection::spawnPlayer(%this)
	{
		%p = parent::spawnPlayer(%this);
		
		schedule(100, 0, commandToClient, %this, 'getFov');
		
		if(!isObject(%this.instances))
			%this.instances = new SimSet(Instances_ @ %this.bl_id);
		
		if(%this.POEEnabled)
		{
			commandToClient(%this, 'openOverlay');
			schedule(0, 0, doIt, %this);
			%this.loop();
		}
		
		return %p;
	}
	
	function GameConnection::onDeath(%this, %killerPlayer, %killer, %damageType, %damageLoc)
	{		
		// do stuff on death?
		commandToClient(%this, 'closeOverlay');
		return parent::onDeath(%this, %killerPlayer, %killer, %damageType, %damageLoc);
	}
	
	function GameConnection::onDrop(%this)
	{		
		if(%this.party)
			%this.leaveParty();
		
		%this.instances.delete();
		
		// save profile
		%this.saveProfile();
		
		return parent::onDrop(%this);
	}
};
activatePackage(POE);