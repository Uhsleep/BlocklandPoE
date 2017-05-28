if(!isObject(Bestel))
{
	new ScriptObject(Bestel)
	{
		superClass = "NPC";

		// appearance
		hatType = "Bicorn";
		accentType = "";
		smiley = "brownSmiley";
		bodyType = "male";
		decal = "";
		packType = "";
		rHandtype = "Hand";
		lHandType = "Hand";
		pantsType = "male";
		rShoeType = "Shoe";
		lShoeType = "Shoe";

		hatColor = "0.435 0.271 0.125 1";
		accentColor = "";
		headColor = "0.918 0.753 0.525 1";
		bodyColor = "0.918 0.753 0.525 1";
		rArmColor = "0.918 0.753 0.525 1";
		lArmColor = "0.918 0.753 0.525 1";
		rHandColor = "0.918 0.753 0.525 1";
		lHandColor = "0.918 0.753 0.525 1";
		pantsColor = "0.435 0.271 0.125 1";
		rShoeColor = "0.918 0.753 0.525 1";
		lShoeColor = "0.918 0.753 0.525 1";


		// characteristics
		friendly = true;
		talkable = true;
		canDamage = false;

		// conversation tree
	};
}

function Bestel::onSpawn(%this, %aiplayer)
{
	talk("spawn from bestel method");
}