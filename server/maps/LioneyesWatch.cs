if(isObject(LioneyesWatchData))
	LioneyesWatchData.delete();
new SimObject(LioneyesWatchData)
{
	// build bls file
	buildName = "LioneyesWatch.bls";
	
	// basic attributes
	canAttack = false;
	availability = 0; 
	vacancyTimeout = -1;
	spawnCount = 2;
	
};

function LioneyesWatch::onCreate(%this)
{
	
}

function LioneyesWatch::onClientEntered(%this, %client)
{
	talk(%client.name @ " joined lioneyes watch.");
}

function LioneyesWatch::onClientLeft(%this, %client)
{
	talk(%client.name @ " left lioneyes watch.");
}