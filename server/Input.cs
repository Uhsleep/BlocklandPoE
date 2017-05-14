$mod_lshift = 1 << 0;
$mod_rshift = 1 << 1;
$mod_lctrl  = 1 << 2;
$mod_rctrl  = 1 << 3;
$mod_lalt   = 1 << 4;
$mod_ralt   = 1 << 5;

$currentYellow = 0;

function serverCmdSetRes(%client, %width, %height)
{
	%client.screenWidth = %width;
	%client.screenHeight = %height;
}

function serverCmdSetFov(%client, %fov)
{
	talk(%client.name @ "'s fov is " @ %fov);
	%client.setControlCameraFov(%fov);
}

function serverCmdOnMouseDown(%client, %mouseX, %mouseY, %mod)
{
	if(!isObject(%client.player) || !%client.POEEnabled)
		return;
	
	%w = screenToRaycastVector(%client, %mouseX, %mouseY);
	%raycast = containerRaycast(%client.camera.position, VectorAdd(%client.camera.position, VectorScale(%w ,200)),$Typemasks::FxBrickObjectType | $Typemasks::PlayerObjectType, %client.player);
	%obj = getWord(%raycast,0);
	
	if(%obj == 0)
		return;
	
	%line = %obj.getName();
	
	if(getSubStr(%line, 0, 1) $= "_")
		%line = getSubStr(%line, 1, strLen(%line) - 1);
	
	%line = strReplace(%line, "_", " ");
	
	%name = getWord(%line, 0);
	%line = removeWord(%line, %name);

	for(%i = 0; %i < 5; %i++)
		%arg[%i] = getWord(%line, %i);
	
	if(isFunction(%name, "onClick"))
		%name.onClick(%obj, %client, %mod, %arg[0], %arg[1], %arg[2], %arg[3], %arg[4]);
}

function serverCmdOnRightMouseDown(%client, %mouseX, %mouseY, %mod)
{	
	if(!isObject(%client.player) || !%client.POEEnabled)
		return;
	
	%w = screenToRaycastVector(%client, %mouseX, %mouseY);
	
	%raycast = containerRaycast(%client.camera.position, VectorAdd(%client.camera.position, VectorScale(%w ,200)), $Typemasks::All, %client.player);
	%positionHit = getWords(%raycast, 1, 3);
	%obj = getWord(%raycast,0);
	if(%obj == 0)
		return;
	
	if(%obj.getClassName() $= "Player")
	{
		// bring up trade interface
	}
	else
	{
		if(isFunction(%obj.getClassName(), "setColor"))
		{
			if($currentYellow)
			{
				$currentYellow.setColor(16);
			}

			%obj.setColor(1);
			$currentYellow = %obj;
		}
		// check if we can use skills in area
		if(%client.currentArea.canAttack)
			schedule(40,0, ek, %client);
	}
}

function screenToRaycastVector(%client, %mouseX, %mouseY)
{
	// get ndc mouse coordinates
	%screenWidth = %client.screenWidth;
	%screenHeight = %client.screenHeight;
	%ndcX = 2 * %mouseX / %screenWidth - 1;
	%ndcY = 1 - 2 * %mouseY / %screenHeight;
	
	// get camera position
	%camera = %client.camera;
	%cameraPos = %camera.position;
	
	// get camera vectors
	%forwardVector = %camera.getForwardVector();
	%upVector = %camera.getUpVector();
	%rightVector = vectorCross(%forwardVector, %upVector);
	
	%length = 10;
	%horizontalFov = mDegToRad(%client.getControlCameraFov());
	%verticalFov = 2 * mAtan(mTan(%horizontalFov / 2) * %screenHeight / %screenWidth, 1);
	
	%u = vectorScale(%forwardVector, %length);
	
	%vX = vectorScale(%rightVector, %length * %ndcX * mTan(%horizontalFov / 2));
	%vY = vectorScale(%upVector, %length * %ndcY * mTan(%verticalFov / 2));
	%v = vectorAdd(%vX, %vY);
	
	%w = vectorNormalize(vectorAdd(%u, %v));
	
	return %w;
}