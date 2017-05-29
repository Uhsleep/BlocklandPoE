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
		
		commandtoclient(%this, 'getRes');
		
		return %p;
	}
	
	function GameConnection::spawnPlayer(%this)
	{
		%p = parent::spawnPlayer(%this);

		if(%this.hasClient)
		{
			%this.player.changeDatablock(PlayerPathOfExile);
			commandToClient(%this, 'PoE_OpenOverlay');
			commandToClient(%this, 'PoE_GetRes');
			%this.schedule(0, setupCamera);
		}

		return %p;
	}
	
	function GameConnection::onDeath(%this, %killerPlayer, %killer, %damageType, %damageLoc)
	{		
		%p = parent::onDeath(%this, %killerPlayer, %killer, %damageType, %damageLoc);

		commandToClient(%this, 'PoE_CloseOverlay');

		return %p;
	}
	
	function GameConnection::onDrop(%this)
	{		
		//if(%this.party)
			//%this.leaveParty();
		
		//%this.instances.delete();
		
		// save profile
		//%this.saveProfile();
		
		return parent::onDrop(%this);
	}
};
activatePackage(POE);

function GameConnection::setupCamera(%this)
{
	%cam = %this.camera;
	%cam.setflymode();
	%this.setcontrolobject(%cam);
	
	%cam.setorbitmode(%this.player, %this.player.position SPC eulerToAxis("-70 0 -15"), -16, 0, 0, true);
	//-75 0 30
	%cam.setControlObject(%this.player);
	%this.bottomPrint(" ", 1, true); // To prevent the "1 observer(s) thing"
	
	%fov = %this.getControlCameraFov();
	%this.setControlCameraFov(%fov);
}