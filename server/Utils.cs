function isNumber(%n)
{
	return stripChars(%n, "0123456789") $= "";
}