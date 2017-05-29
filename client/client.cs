$POE::ClientVersion = 1;
exec("./PlayGuiOverlay.cs");

package POE_Connect
{
	function GameConnection::setConnectArgs(%this, %lanName, %netName, %prefix, %suffix, %nonce, %rtb, %modules, %a, %b, %c, %d, %e, %f, %g, %h)
	{
        if (%modules $= "")
            %modules = "POE" SPC $POE::ClientVersion;
        else
            %modules = %modules TAB ("POE" SPC $POE::ClientVersion);

        activatePackage(PlayGuiOverlayPackage);

        parent::setConnectArgs(%this, %lanName, %netName, %prefix, %suffix, %nonce, %rtb, %modules, %a, %b, %c, %d, %e, %f, %g, %h);
	}

	function disconnect()
	{
		deactivatePackage(PlayGuiOverlayPackage);

		return parent::disconnect();
	}
};
activatePackage(POE_Connect);