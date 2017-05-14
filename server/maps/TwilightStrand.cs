if(isObject(TwilightStrandData))
	TwilightStrandData.delete();
new SimObject(TwilightStrandData)
{
	mapName = "Twilight Strand";
	buildName = "TwilightStrand.bls";
	
	// basic attributes
	canAttack = true;
	availability = $Poe::MapAvailability::Party; 
	vacancyTimeout = 60000;
	spawnCount = 1;
	
	// normal, cruel, and merciless levels
	level[$Poe::Difficulty::Normal] 	= 1;
	level[$Poe::Difficulty::Cruel] 		= 40;
	level[$Poe::Difficulty::Merciless] 	= 56;
};

function TwilightStrand::onClientJoin(%this, %client)
{
	parent::onClientJoin(%this, %client);

	talk(%client.name @ " joined twilight strand.");
}

function TwilightStrand::onClientLeave(%this, %client)
{
	parent::onClientLeave(%this, %client);
	talk(%client.name @ " left twilight strand.");
}