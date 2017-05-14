$poe::startFrom = "base/poe/server/";

function loadBuild(%offset, %build, %brickgroup) {
    if (!isFile("./maps/builds/" @ %build)) {
        echo("Error: Load file not found");
        return;
    }

    %file = new FileObject();
    %tempGroup = new SimGroup();
    %file.openForRead($poe::startFrom @ "maps/builds/" @ %build);
    %file.readLine();
    %cnt = %file.readLine();

    for(%i = 0; %i < %cnt; %i++) {
        %file.readLine();
    }

    //generate color table - edited by Conan
    //save and reuse for later instances of same board game
    if ($BoardGame::ColorTable[%name @ "::" @ 0] $= "") {
        for(%i = 0; %i < 64; %i++) {
            $BoardGame::ColorTable[%name @ "::" @ %i] = getClosestColorID(getColorI(%file.readLine()));
        }
    }

    while (!(getWord(%lastline, 0) $= "Linecount"))    {
        %lastline = %file.readLine();
    }

    while(!%file.isEOF()) {
        %line = %file.readLine();
        if (getSubStr(%line, 0, 2) $= "+-") {
            //save all these lines to the brick to be applied to the planted brick later

            %brick.numProperties += 1;
            %brick.property[%brick.numProperties - 1] = %line;
            continue;
        }

        //thank god for zack0wack0
        //http://forum.blockland.us/index.php?topic=157516.msg3787924#msg3787924
        %line = trim(nextToken(%line, "brickUIname", "\""));
        %brickDatablock = $uiNameTable[%brickUIname];

        if (!isObject(%brickDatablock)) {
            continue;
        }

        %brickPos = getWords(%line, 0, 2);
        %brickAngle = getWord(%line, 3);
        %brickPrintID = getWord(%line, 4);
        %brickColor = $BoardGame::ColorTable[%name @ "::" @ getWord(%line, 5)];
        %brickPrint = getWord(%line, 6);
        if (%brickPrint !$= "") {
            %brickPrint = $printNameTable[%brickPrint];
        }
        %brickColorFX = getWord(%line, 7);
        %brickShapeFX = getWord(%line, 8);

        %brickRaycasting = getWord(%line, 9);
        %brickCollision = getWord(%line, 10);
        %brickRendering = getWord(%line, 11);

        //code from Zeblote
        switch(%brickAngle)    {
            case 0: %brickRot = "1 0 0 0";
            case 1: %brickRot = "0 0 1 90.0002";
            case 2: %brickRot = "0 0 1 180";
            case 3: %brickRot = "0 0 -1 90.0002";
        }

        %brick = new fxDTSBrick() {
            datablock = %brickDatablock;
            position = vectorAdd(%brickPos, %offset);
            rotation = %brickRot;
            angleID = %brickAngle;
            colorID = %brickColor;
            colorFXID = %brickColorFX;
            shapeFXID = %brickShapeFX;
            printID = %brickPrint;

            raycasting = %brickRaycasting;
            collision = %brickCollision;
            rendering = %brickRendering;
        };
        %tempGroup.add(%brick);

        //average position
        %worldbox = %brick.getWorldBox();
        %x1 = getWord(%worldbox, 0);
        %x2 = getWord(%worldbox, 3);
        %y1 = getWord(%worldbox, 1);
        %y2 = getWord(%worldbox, 4);
        %z1 = getWord(%worldbox, 2);
        %z2 = getWord(%worldbox, 5);
        if (%posMaxX $= "" || %x1 > %posMaxX || %x2 > %posMaxX) {
            %posMaxX = (%x1 > %x2 ? %x1 : %x2);
        }
        if (%posMaxY $= "" || %y1 > %posMaxY || %y2 > %posMaxY) {
            %posMaxY = (%y1 > %y2 ? %y1 : %y2);
        }
        if (%posMaxZ $= "" || %z1 > %posMaxZ || %z2 > %posMaxZ) {
            %posMaxZ = (%z1 > %z2 ? %z1 : %z2);
        }

        if (%posMinX $= "" || %x1 < %posMinX || %x2 < %posMinX) {
            %posMinX = (%x1 > %x2 ? %x1 : %x2);
        }
        if (%posMinY $= "" || %y1 < %posMinY || %y2 < %posMinY) {
            %posMinY = (%y1 > %y2 ? %y1 : %y2);
        }
        if (%posMinZ $= "" || %z1 < %posMinZ || %z2 < %posMinZ) {
            %posMaxZ = (%z1 > %z2 ? %z1 : %z2);
        }
    }
    echo("Read file and found " @ %tempgroup.getCount() @ " bricks");

    for (%i = 0; %i < %tempGroup.getCount(); %i++) {
        %brick = %tempGroup.getObject(%i);

        %finalBrick = new fxDTSBrick() {
            datablock = %brick.getDatablock();
            position = %brick.position;
            rotation = %brick.rotation;
            angleID = %brick.angleID;
            colorID = %brick.colorID;
            colorFXID = %brick.colorFXID;
            shapeFXID = %brick.shapeFXID;
            printID = %brick.printID;
            isPlanted = 1;
        };
        %error = %finalBrick.plant();
        if (%error > 0 && %error != 2) {
            %errorCnt++;
            %finalBrick.delete();
        } else {
            %finalBrick.setTrusted(1);
            %finalBrick.setRaycasting(%brick.raycasting);
            %finalBrick.setColliding(%brick.collision);
            %finalBrick.setRendering(%brick.rendering);
            %brickgroup.add(%finalBrick);
            
            for (%j = 0 ; %j < %brick.numProperties; %j++) {
                applyBrickProperties(%finalBrick, %brick.property[%j], %brick.angleID);
            }
        }
    }
    %tempGroup.delete();

    if (%errorCnt > 0) {
        echo(%errorCnt @ " ghost bricks failed to plant");
        echo("Parameters: " @ %offset SPC %build SPC %brickgroup.getName());
    }
    echo("Loaded " @ %build @ " @ " @ %offset);
    %file.close();
    %file.delete();
    if (%errorCnt > 0) {
        return 0;
    } else {
        return 1;
    }
}


//By Zeblote; originally made for New Duplicator
function getClosestColorID(%rgba) {
    %rgb = getWords(%rgba, 0, 2);
    %a = getWord(%rgba, 3);

    //Set initial value
    %color = getColorI(getColorIdTable(0));
    %alpha = getWord(%color, 3);

    %best = 0;
    %bestDiff = vectorLen(vectorSub(%rgb, %color));

    if((%alpha > 254 && %a < 254) || (%alpha < 254 && %a > 254)) {
        %bestDiff += 1000;
    } else {
        %bestDiff += mAbs(%alpha - %a) * 0.5;
    }

    for(%i = 1; %i < 64; %i++) {
        %color = getColorI(getColorIdTable(%i));
        %alpha = getWord(%color, 3);

        %diff = vectorLen(vectorSub(%rgb, %color));

        if((%alpha > 254 && %a < 254) || (%alpha < 254 && %a > 254)) {
            %diff += 1000;
        } else {
            %diff += mAbs(%alpha - %a) * 0.5;
        }

        if(%diff < %bestDiff) {
            %best = %i;
            %bestDiff = %diff;
        }
    }

    return %best;
}

function applyBrickProperties(%brick, %line, %angleID) {
    if (getWord(%line, 0) $= "+-EVENT") {
        //echo("   Applying event property");
        applyBrickEvent(%brick, %line, %angleID);
    } else if (getWord(%line, 0) $= "+-LIGHT") {
        //echo("   Applying light property");
        applyBrickLight(%brick, %line);
    } else if (getWord(%line, 0) $= "+-ITEM") {
        //echo("   Applying item property");
        applyBrickItem(%brick, %line, %angleID);
    } else if (getWord(%line, 0) $= "+-EMITTER") {
        //echo("   Applying emitter property");
        applyBrickEmitter(%brick, %line, %angleID);
    } else if (getWord(%line, 0) $= "+-NTOBJECTNAME") {
        //echo("   Applying name property");
        %brick.setNTObjectName(getSubStr(getWord(%line, 1), 0, strLen(getWord(%line, 1))));
    }
}

function applyBrickItem(%brick, %line, %angleID) {
    //takes in a %brick and a %line from a save file (including the +- prefix)
    //and applies the line's item onto the brick
    %line = restWords(%line);
    %itemNameEnd = stripos(%line, "\"");
    %itemName = getSubStr(%line, 0, %itemNameEnd);
    %line = getSubStr(%line, %itemNameEnd+2, strlen(%line)-%itemNameEnd-1);
    %itemPos = getWord(%line, 0);
    if (%itemPos > 1) {
        %itemPos = 2+(%itemPos-2+%angleID)%4;
    }
    %itemDir = 2+(getWord(%line, 1)-2)%4; //+ %angleID)%4;
    %itemTimeout = getWord(%line, 2);

    %brick.setItem($uiNameTable_Items[%itemName]);
    %brick.setItemDirection(%itemDir);
    %brick.setItemPosition(%itemPos);
    %brick.setItemRespawnTime(%itemTimeout);
}

function applyBrickEmitter(%brick, %line, %angleID) {
    //takes in a %brick and a %line from a save file (including the +- prefix)
    //and applies the line's emitter onto the brick
    %line = restWords(%line);
    %emitterNameEnd = stripos(%line, "\"");
    %emitterName = getSubStr(%line, 0, %emitterNameEnd);
    %line = getSubStr(%line, %emitterNameEnd+2, strlen(%line)-%emitterNameEnd-1);
    %emitterDir = getWord(%line, 0);
    if (%emitterDir > 1) {
        %emitterDir = 2+(%emitterDir-2+%angleID)%4;
    }

    %brick.setemitter($uiNameTable_emitters[%emitterName]);
    %brick.setemitterDirection(%emitterDir);
}

function applyBrickLight(%brick, %line) {
    %line = restWords(%line);
    %lightNameEnd = stripos(%line, "\"");
    %light = getSubStr(%line, 0, %lightNameEnd);
    if ($uiNameTable_Lights[%light] > 0) {
        %brick.setLight($uiNameTable_Lights[%light]);
    }
}

function applyBrickEvent(%brick, %line, %angleID) {
    %idx = getField(%line, "1");
    %enabled = getField(%line, "2");
    %inputName = getField(%line, "3");
    %delay = getField(%line, "4");
    %targetName = getField(%line, "5");
    %NT = getField(%line, "6");
    %outputName = getField(%line, "7");
    %par1 = getField(%line, "8");
    %par2 = getField(%line, "9");
    %par3 = getField(%line, "10");
    %par4 = getField(%line, "11");
    if (isObject(%par1) && !isInteger(%par1) && getWordCount(%par1) == 1) {
        %par1 = nameToID(%par1);
    }
    if (isObject(%par2) && !isInteger(%par2) && getWordCount(%par2) == 1) {
        %par2 = nameToID(%par2);
    }
    if (isObject(%par3) && !isInteger(%par3) && getWordCount(%par3) == 1) {
        %par3 = nameToID(%par3);
    }
    if (isObject(%par4) && !isInteger(%par4) && getWordCount(%par4) == 1) {
        %par4 = nameToID(%par4);
    }

    %inputEventIdx = inputEvent_GetInputEventIdx(%inputName);
    %targetIdx = inputEvent_GetTargetIndex("fxDTSBrick", %inputEventIdx, %targetName);
    
    if(%targetName == (-1)) {
        %targetClass = "fxDTSBrick";
    } else {
        %field = getField($InputEvent_TargetList["fxDTSBrick", %inputEventIdx], %targetIdx);
        %targetClass = getWord(%field, "1");
    }
    %outputEventIdx = outputEvent_GetOutputEventIdx(%targetClass, %outputName);
    %NTNameIdx = (-1);
    
    //check for rotation
    //check for vector parameters
    %paramList = $OutputEvent_ParameterList[%targetClass, %outputEventIdx];
    %paramCount = getFieldCount(%paramList);
    for(%k = 0; %k < %paramCount; %k++)
    {
        if(getWord(getField(%paramList, %k), 0) $= "vector")
        {
            //apply rotation effects
            %vec = %par[%k+1];

            for (%i = 0; %i < 4-%angleID; %i++) {
                %vec = -getWord(%vec, 1) SPC getWord(%vec, 0) SPC getWord(%vec, 2);
            }

            %par[%k+1] = %vec;
        }
    }
    //check for relay events
    switch$ (%outputName) {
        case "fireRelayNorth": %dir = 2;
        case "fireRelayEast":  %dir = 3;
        case "fireRelaySouth": %dir = 4;
        case "fireRelayWest":  %dir = 5;
        default: %dir = -1;
    }

    if (%dir >= 0) {
        %rotated = %dir;
        %rotated = (%rotated + %angleID - 2) % 4 + 2;
        //Apply mirror effects
        %outputEventIdx += %rotated - %dir;

        switch (%rotated) {
            case 0: %outputName = "fireRelayUp";
            case 1: %outputName = "fireRelayDown";
            case 2: %outputName = "fireRelayNorth";
            case 3: %outputName = "fireRelayEast";
            case 4: %outputName = "fireRelaySouth";
            case 5: %outputName = "fireRelayWest";
        }
    }
    //talk(%brick SPC %enabled SPC %inputEventIdx SPC %delay SPC %targetName SPC %targetIDX SPC %NTNameIdx SPC %outputName SPC %outputEventIdx SPC %par1 SPC %par2 SPC %par3 SPC %par4);
    //talk(%targetClass @ %outputName);
    //talk(%NT);

    %brick.eventEnabled[%idx] = %enabled;
    %brick.eventInput[%idx] = %inputName;
    %brick.eventInputIdx[%idx] = %inputEventIdx;
    %brick.eventDelay[%idx] = %delay;
    %brick.eventTarget[%idx] = %targetName;
    %brick.eventTargetIDX[%idx] = %targetIdx;
    if (%NT !$= "") {
        %brick.eventNT[%idx] = %NT;
    }
    %brick.eventOutput[%idx] = %outputName;
    %brick.eventOutputIdx[%idx] = %outputEventIdx;
    if (%par1 !$= "") {
        %brick.eventOutputParameter[%idx,1] = %par1;
    }
    if (%par2 !$= "") {
        %brick.eventOutputParameter[%idx,2] = %par2;
    }
    if (%par3 !$= "") {
        %brick.eventOutputParameter[%idx,3] = %par3;
    }
    if (%par4 !$= "") {
        %brick.eventOutputParameter[%idx,4] = %par4;
    }
    %brick.eventOutputAppendClient[%idx] = $outputEvent_AppendClient[%targetName, %eventOutputIdx]; //fix this asap
    %brick.numEvents = %idx+1;
}

function isInteger(%int) {
    return stripchars(%int, "0123456789") $= "";
}

