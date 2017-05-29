$mod_lshift = 1 << 0;
$mod_rshift = 1 << 1;
$mod_lctrl  = 1 << 2;
$mod_rctrl  = 1 << 3;
$mod_lalt   = 1 << 4;
$mod_ralt   = 1 << 5;

if (!isFunction("GuiMouseEventCtrl", "onMouseMove"))
	eval("function GuiMouseEventCtrl::onMouseMove(){}");
if (!isFunction("GuiMouseEventCtrl", "onMouseEnter"))
	eval("function GuiMouseEventCtrl::onMouseEnter(){}");
if (!isFunction("GuiMouseEventCtrl", "onMouseLeave"))
	eval("function GuiMouseEventCtrl::onMouseLeave(){}");
if (!isFunction("GuiMouseEventCtrl", "onMouseDown"))
	eval("function GuiMouseEventCtrl::onMouseDown(){}");
if (!isFunction("GuiMouseEventCtrl", "onMouseUp"))
	eval("function GuiMouseEventCtrl::onMouseUp(){}");
if (!isFunction("GuiMouseEventCtrl", "onMouseDragged"))
	eval("function GuiMouseEventCtrl::onMouseDragged(){}");
if (!isFunction("GuiMouseEventCtrl", "onRightMouseDown"))
	eval("function GuiMouseEventCtrl::onRightMouseDown(){}");
if (!isFunction("GuiMouseEventCtrl", "onRightMouseUp"))
	eval("function GuiMouseEventCtrl::onRightMouseUp(){}");	

if(!isObject(PlayGuiOverlay))
{
	new GuiMouseEventCtrl(PlayGuiOverlay) {
		  profile = "GuiDefaultProfile";
		  horizSizing = "right";
		  vertSizing = "bottom";
		  position = "0 0";
		  extent = getWords(getRes(), 0, 1);
		  minExtent = "8 2";
		  enabled = "1";
		  visible = "1";
		  clipToParent = "1";
		  lockMouse = "0";
	};
}

function clientCmdPoE_OpenOverlay()
{
	canvas.pushdialog(PlayGuiOverlay);
}

function clientCmdPoE_CloseOverlay()
{
	canvas.popDialog(PlayGuiOverlay);
}

function clientCmdPoE_GetRes()
{
	commandToServer('setRes', getWord(getRes(), 0), getWord(getRes(), 1));
}

function clientCmdPoE_GetFov()
{
	commandToServer('setFov', serverConnection.getControlCameraFov());
}

function getScreenAngle(%vec)
{
	%camera = serverConnection.getControlObject();
	%w = vectorAdd(%camera.getForwardVector(), "0 1 0");
	
	%upVector = %camera.getUpVector();
	%rightVector = vectorCross(%camera.getForwardVector(), %upVector);
	
	%vX = vectorDot(%w, %rightVector);
	%vY = vectorDot(%w, %upVector);
	
	%screenWidth = getWord(getRes(), 0);
	%screenHeight = getWord(getRes(), 1);	
	%horizontalFov = serverConnection.getControlCameraFov();
	%verticalFov = mRadToDeg(2 * mAtan(mTan(mDegToRad(%horizontalFov / 2)) * %screenHeight / %screenWidth, 1));
	
	%ndcX = %vX / mTan(mDegToRad(%horizontalFov / 2));
	%ndcY = %vY / mTan(mDegToRad(%verticalFov / 2));

	%north = vectorNormalize(%vX SPC %vY);
	//echo("north is (" @ %north @ ")");

	return getAngle(%north, %vec);
}

function getAngle(%vec1, %vec2)
{
	%z = getWord(vectorCross(%vec1, %vec2), 2);
	%rad = mAcos(vectorDot(%vec1, %vec2));
	
	return %z > 0 ? %rad : -%rad;
}

function turnToScreenPosition(%mouseX, %mouseY)
{
	%x = %mouseX - getWord(getRes(), 0) / 2;
	%y = getWord(getRes(), 1) / 2 - %mouseY;
	
	%v = vectorNormalize(%x SPC %y SPC "0");
	//talk("screen vector: " @ %v);
	//%screenAngle = getScreenAngle(%v);
	//echo("screen Angle: " @ %screenAngle * 180 / $pi);
	
	// project camera right / up vectors to x-y plane (set z = 0)
	%camera = serverConnection.getControlObject();
	%upVector = %camera.getUpVector();
	%rightVector = vectorCross(%camera.getForwardVector(), %upVector);
	%u = vectorNormalize(setWord(%upVector, 2, "0"));
	%r = vectorNormalize(setWord(%rightVector, 2, "0"));

	%vecX = vectorScale(%r, getWord(%v, 0));
	%vecY = vectorScale(%u, getWord(%v, 1));
	%vec = vectorNormalize(vectorAdd(%vecX, %vecY));
	// 

	//%vec = -mSin(%screenAngle) SPC mCos(%screenAngle);
	%rad = getAngle(%vec, serverConnection.getControlObject().getControlObject().getForwardVector());
	
	$mvYaw = %rad;
}

package PlayGuiOverlayPackage
{
	function GuiMouseEventCtrl::onMouseMove(%this, %a, %b, %c)
	{	
		parent::onMouseMove(%this, %a, %b, %c);

		%mouseX = getWord(%b, 0);
		%mouseY = getWord(%b, 1);
	}
	
	function GuiMouseEventCtrl::onMouseDragged(%this, %a, %b, %c)
	{
		parent::onMouseDragged(%this, %a, %b, %c);
		
		%mouseX = getWord(%b, 0);
		%mouseY = getWord(%b, 1);
		
		turnToScreenPosition(%mouseX, %mouseY);
	}
	
	function GuiMouseEventCtrl::onMouseDown(%this, %mod, %pos, %click)
	{
		parent::onMouseDown(%this, %mod, %pos, %click);
		
		moveForward(1);
		
		//echo(%mod);
		
		%mouseX = getWord(%pos, 0);
		%mouseY = getWord(%pos, 1);
		
		turnToScreenPosition(%mouseX, %mouseY);
		commandToServer('onMouseDown', %mouseX, %mouseY, %mod);
	}

	function GuiMouseEventCtrl::onRightMouseDown(%this, %mod, %pos, %click)
	{
		parent::onRightMouseDown(%this, %mod, %pos, %click);
		
		%mouseX = getWord(%pos, 0);
		%mouseY = getWord(%pos, 1);
		
		turnToScreenPosition(%mouseX, %mouseY);
		commandToServer('onRightMouseDown', %mouseX, %mouseY, %mod);
	}

	function GuiMouseEventCtrl::onMouseUp(%this, %mod, %pos, %click)
	{
		parent::onMouseUp(%this, %mod, %pos, %click);
		moveForward(0);
	} 	
	
	function toggleCursor(%val)
	{
		if(!canvas.isMember(PlayGuiOverlay))
			return parent::toggleCursor(%val);


		if(!canvas.isCursorOn())
			canvas.cursorOn();

		if(%val)
		{
			if(canvas.getObject(canvas.getCount() - 1).getName() $= "NewChatHud")
			{
				%msg = "Normal mode";
				canvas.pushToBack(PlayGuiOverlay);
			}
			else
			{
				%msg = "Link mode";
				canvas.pushToBack(NewChatHud);
			}

			clientCmdCenterPrint(%msg, 3);
		}
		
		return %val;
	}
	
	function Canvas::pushDialog(%this, %dialog)
	{
		%p = parent::pushDialog(%this, %dialog);
		
		moveForward(0);
		
		return %p;
	}

	function Canvas::setContent(%this, %gui)
	{
		%g = %this.isMember(PlayGuiOverlay);
		%p = parent::setContent(%this, %gui);

		if(%g)
			%this.pushDialog(PlayGuiOverlay);

		return %p;
	}
};
activatePackage(PlayGuiOverlayPackage);