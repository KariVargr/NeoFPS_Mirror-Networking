# NeoFPS_Mirror-Networking

This is an "In Progress" build

# Install Required
<p>NeoFPS ( https://assetstore.unity.com/packages/templates/systems/neofps-fps-controller-template-toolkit-150179 )</p>
<p>Mirror Networking ( https://mirror-networking.com/ ) ( https://assetstore.unity.com/packages/tools/network/mirror-129321 )</p>

# Files that need to be adjusted 
<p>File: FpsFameMode.cs<br>
( This allows the use of FpsNetCharacter and FpsNetPlayerController )<br>
Line 132: protected abstract void ProcessOldPlayerCharacter(ICharacter oldCharacter);<br>
Line 134: protected abstract ICharacter GetPlayerCharacterPrototype(IController player);</p>
<br>
<p>File: FpsSoloGameMinimal.cs<br>
( Correstion to keep the Solo working with Above Changes )<br>
Line 120: protected override IController InstantiatePlayer ()<br>
Line 175: protected override void ProcessOldPlayerCharacter(ICharacter oldCharacter)</p>
<br>
<p>File: BaseController.cs<br>
( Making isLocalPlayer Viritaul allows FpsNetPlayerController to check its networkInstance to see if the the player is local)<br>
Line 24: public virtual bool isLocalPlayer</p>

# Recommendation 
I suggest using First Gear Games Mirror "Assets For Mirror Networking" (https://www.patreon.com/firstgeargames/posts)<br>
His FlexNetworkTransform is very handy and i have been using his tutorials to help, and seem to be making quite a few assets to improve the Mirror Networking.
