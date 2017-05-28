function InstanceManager::init(%this)
{
	%this.pathToBuilds = "base/BlocklandPoE/server/maps/builds/";
	%this.loading = false;
	%this.queue = new SimSet(InstanceQueue);
	%this.instances = new SimSet(InstaceSet);

	%this.maxInstanceCount = 100;
	%this.lastOpenLocationIndex = 0;
	%this.instanceCount = 0;
	%this.offset = "-18000 -18000 0";
	%this.brickGroupId = 8008135;
	%this.instancesCreated = 0;

	// create the town instances.
    //%this.town[1], $Poe::Difficulty::Normal] = %this.newInstance("LioneyesWatch", $Poe::Difficulty::Normal);
    // %this.town[2][$Poe::Difficulty::Cruel] = %this.newInstance("LioneyesWatch", $Poe::Difficulty::Cruel);
    // %this.town[3][$Poe::Difficulty::Merciless] = %this.newInstance("LioneyesWatch", $Poe::Difficulty::Merciless);

    // %this.town[1][$Poe::Difficulty::Normal] = %this.newInstance("LioneyesWatch", $Poe::Difficulty::Normal);
    // %this.town[2][$Poe::Difficulty::Cruel] = %this.newInstance("LioneyesWatch", $Poe::Difficulty::Cruel);
    // %this.town[3][$Poe::Difficulty::Merciless] = %this.newInstance("LioneyesWatch", $Poe::Difficulty::Merciless);

    // %this.town[1][$Poe::Difficulty::Normal] = %this.newInstance("LioneyesWatch", $Poe::Difficulty::Normal);
    // %this.town[2][$Poe::Difficulty::Cruel] = %this.newInstance("LioneyesWatch", $Poe::Difficulty::Cruel);
    // %this.town[3][$Poe::Difficulty::Merciless] = %this.newInstance("LioneyesWatch", $Poe::Difficulty::Merciless);
}

if(!isObject(InstanceManager))
{
	new ScriptObject(InstanceManager);
	InstanceManager.init();
}


function InstanceManager::getNewInstanceLocation(%this)
{
	for (%i = %this.lastOpenLocationIndex; %i < %this.lastOpenLocationIndex + %this.maxInstanceCount; %i++)
    {
        %index = %i % %this.maxInstanceCount;

        if (!%this.positionOccupied[%index])
        {
            %row = mFloor(%index / mFloor(mSqrt(%this.maxInstanceCount)));
            %column = %index % mFloor(mSqrt(%this.maxInstanceCount));
            %position = (%column * 4000) SPC (%row * 4000) SPC "0";
            
            %this.lastOpenLocationIndex = %index;
            return vectorAdd(%position, %this.offset);
        }
    }

    return "";
}

function InstanceManager::newInstance(%this, %mapName, %difficulty)
{
	// do we have room for another instance?
	if (%this.instances.getCount() >= %this.maxInstanceCount)
	{
		echo("Unable to load instance. Max instances already reached.");
		return;
	}

	// check if mapName is legit
	if (!isObject(%mapData = %mapName @ "Data"))
	{
		echo("Unable to find map '" @ %mapName @ "'.");
		return;
	}

	// clamp difficulty
	%difficulty = mClamp(%difficulty, $Poe::Difficulty::Normal, $Poe::Difficulty::Merciless);

	// get instance location
	%loadLocation = %this.getNewInstanceLocation();

	// create instance
	%instance = new ScriptObject(%mapName) 
	{ 
		superClass = "Instance";

		name = %mapData.mapName; 
		difficulty = %difficulty; 
		level = %mapData.level[%difficulty];
		vacancyTimeout = %mapData.vacancyTimeout;
		availability = %mapData.availability;
		canAttack = %mapData.canAttack; 
	};

	%instance.clients = new SimSet("InstanceClients_" @ %this.brickGroupId);
	%instance.brickGroup = new SimGroup("BrickGroup_" @ %this.brickGroupId);
	%instance.brickGroup.bl_id = %this.brickGroupId;
	%instance.brickGroup.ispublicdomain = false;
	%instance.brickGroup.name = %mapData.mapName;
	%instance.brickGroup.instance = %instance;
	mainBrickGroup.add(%instance.brickGroup);

	// if the queue is currently empty, begin the loading process directly, otherwise add to queue
	if (%this.queue.getCount() == 0 && !%this.loading)
	{
		%this.currentLoadingInstance = %instance;
		$LoadOffset = %loadLocation;
		serverDirectSaveFileLoad(%this.pathToBuilds @ %mapName @ ".bls", 3, "", 0, true);
	}
	else 
	{
		%job = new SimObject(InstanceJob)
		{
			buildFile = %this.pathToBuilds @ %mapName @ ".bls";
			location = %loadLocation;
			instance = %instance;
		};
		%this.queue.add(%job);
		%this.queue.pushToBack(%job);
	}

	%this.instance.positionIndex = %this.lastOpenLocationIndex;
	%this.positionOccupied[%this.lastOpenLocationIndex] = true;
	%this.instances.add(%instance);
	return %instance;
}

function InstanceManager::destroy(%this)
{
	%count = %this.instances.getCount();
	for (%i = 0; %i < %count; %i++)
		%this.instances.getObject(0).destroy();

	if (%this.queue.getCount() > 0)
		%this.queue.deleteAll();

	%this.instances.delete();
	%this.queue.delete();
	%this.delete();
}

//////////////////////////////////////////////////////////////////////////////////////////////////////
package POE_InstanceLoader
{
	function serverDirectSaveFileLoad(%dir, %colorSet, %dirName, %ownership, %silent)
	{
		// a regular request to load in a file (from the load bricks menu in gui)
		if (InstanceManager.loading)
		{
			%loadJob = new SimObject(InstanceJob)
			{
				buildFile = %dir;
				location = "0 0 0";
				instance = 0;
			};
			InstanceManager.queue.add(%loadJob);
		}
		else
		{
			if (!isObject(InstanceManager.currentLoadingInstance))
				$LoadOffset = "0 0 0";
			
			InstanceManager.loading = true;
			parent::serverDirectSaveFileLoad(%dir, %colorSet, %dirName, %ownership, %silent);
		}
	}
	
	function fxDtsBrick::onLoadPlant(%this)
	{
		%instance = InstanceManager.currentLoadingInstance;

		if (isObject(%instance))
		{
			%instance.brickGroup.add(%this);
			%this.instance = %instance;
		}
		
		return parent::onLoadPlant(%this);
	}
	
	function ServerLoadSaveFile_End()
	{
		Parent::ServerLoadSaveFile_End();
	
		if ((%callback = InstanceManager.currentLoadingInstance.onLoadedCallback) !$= "")
			eval(%callback);
	
		InstanceManager.currentLoadingInstance = 0;
		InstanceManager.loading = false;
		
		if (InstanceManager.queue.getCount() > 0)
		{
			%job = InstanceManager.queue.getObject(0);
			InstanceManager.queue.remove(%job);
			InstanceManager.queue.pushToBack(InstanceManager.queue.getObject(0));

			%buildFile = %job.buildFile;
			$loadOffset = %job.location;
			InstanceManager.currentLoadingInstance = %job.instance;
			
			serverDirectSaveFileLoad(%buildFile, 3, "", 0, true);
			%job.delete();
		}
	}

	function SimObject::setNTObjectName(%this, %name)
	{
		%p = parent::setNTObjectName(%this, %name);

		%i = getSubStr(%name, 0, 1) $= "_" ? 1 : 0;
		%nameWords = strReplace(getSubStr(%name, %i, strLen(%name)), "_", " ");

		if(getWord(%nameWords, 0) $= "npcspawn" && !isObject(%this.npc))
		{
			%npcName = getWord(%nameWords, 1);
			//talk("npc name: " @ %npcName);
			%aiPlayer = new AIPlayer("NPC_" @ %npcName)
			{
				position = vectorAdd(%this.position, "0 0 0.5");
				dataBlock = playerStandardArmor;
				npcName = %npcName;
			};

			%aiPlayer.setHatType(%npcName.hatType);
			%aiPlayer.setHatAccentType(%npcName.accentType);
			%aiPlayer.setHeadSmiley(%npcName.smiley);
			%aiPlayer.setBodyType(%npcName.bodyType);
			%aiPlayer.setBodyDecal(%npcName.decal);
			%aiPlayer.setPackType(%npcName.packType);
			%aiPlayer.setLeftHandType(%npcName.lHandtype);
			%aiPlayer.setRightHandtype(%npcName.rHandType);
			%aiPlayer.setPantsType(%npcName.pantsType);
			%aiPlayer.setLeftShoeType(%npcName.lShoeType);
			%aiPlayer.setRightShoetype(%npcName.rShoeType);

			%aiPlayer.setHatColor(%npcName.hatColor);
			%aiPlayer.setHatAccentColor(%npcName.accentType, %npcName.accentColor);
			%aiPlayer.setHeadColor(%npcName.headColor);
			%aiPlayer.setBodyColor(%npcName.bodyColor);
			%aiPlayer.setRightArmColor(%npcName.rArmColor);
			%aiPlayer.setLeftArmColor(%npcName.lArmColor);
			%aiPlayer.setRightHandColor(%npcName.rHandColor);
			%aiPlayer.setLeftHandColor(%npcName.lHandColor);
			%aiPlayer.setPantsColor(%npcName.pantsColor);
			%aiPlayer.setRightShoeColor(%npcName.rShoeColor);
			%aiPlayer.setLeftShoeColor(%npcName.lShoeColor);

			%aiPlayer.instance = %this.instance;
			%this.npc = %aiPlayer;

			if(isFunction(%aiPlayer.npcName, "onSpawn"))
				%aiPlayer.npcName.onSpawn(%aiPlayer);
		}

		return %p;
	}

	function fxDTSBrick::onDeath(%this)
	{
		%p = parent::onDeath(%this);
		
		if(isObject(%this.npc))
			%this.npc.delete();

		return %p;
	}
};
activatePackage(POE_InstanceLoader);

//////////////////////////////////////////////////////////////////////////////////////////////////////
function Instance::onClientJoin(%this, %client)
{
	if (isEventPending(%this.destroySchedule))
		cancel(%this.destroySchedule);

}

function Instance::onClientLeave(%this, %client)
{
	%count = %this.clients.getCount();

	if (%count == 0)
		%this.destroySchedule = %this.schedule(%this.vacancyTimeout, destroy);
}

function Instance::getSpawnLocation(%this, %spawnIndex)
{
	return %this.brickGroup.ntObject["", "spawn_" @ %spawnIndex, 0].position;
}

function Instance::destroy(%this)
{
	%count = %this.clients.getCount();
	if (%count > 0)
	{
		%act = %this.clients.getObject(0).act;
		%townInstance = InstanceManager.town[%act, %this.difficulty];

		for (%i = 0; %i < %count; %i++)
		{
			%client = %this.clients.getObject(%i);
			%client.joinInstance(%townInstance);
		}
	}

	%this.clients.delete();
	%this.brickGroup.delete();
	%this.delete();
	
	InstanceManager.positionOccupied[%this.positionIndex] = false;
}

//////////////////////////////////////////////////////////////////////////////////////////////////////
function GameConnection::joinInstance(%this, %instance, %spawnIndex)
{
	if (!isObject(%player = %this.player))
		return;

	if (%spawnIndex $= "")
		%spawnIndex = 0;

	%currentInstance = %this.instance;
	if (isObject(%currentInstance))
	{
		%currentInstance.clients.remove(%this);
		%currentInstance.onClientLeave(%this);
	}

	if (isObject(%instance))
	{
		%instance.clients.add(%this);
		%instance.onClientJoin(%this);

		%position = %instance.getSpawnLocation(%spawnIndex);
		%player.setTransform(%position);
		%this.instance = %instance;
	}
}
