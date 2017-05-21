if(!isObject(Bestel))
{
	new ScriptObject(Bestel)
	{
		superClass = "NPC";

		// appearance


		// characteristics
		friendly = true;
		talkable = true;
		canDamage = false;

		// conversation tree
	};
}

function Bestel::onSpawn(%this, %aiplayer)
{
	parent::onSpawn(%this, %aiplayer);

	talk("spawn from bestel method");
}