# NeoFPS_Mirror-Networking

This is an "In Progress" build <br>
NOTE: The only 1 Working weapon prefab, Activly working on the "Shooter" to support network<br>
Will be building in time dilation for projectile shooters to spawn and them up untill synce across all clients<br>
Looking in to Lag Comp for Raycast shooters, may be intergarting some 3rd party assest to improve Lag Comp/Rollback for weapons
<p>
Currently Working<br>
Client Authority on<br>
Movement, Aim, Enviro Damage, Shooting, weapon change<br>
Server Authority on<br>
Weapon Damage, Health, Ammo<br>
only 1 Network working Weapon currently
</p>


# Install Required
<p>NeoFPS ( https://assetstore.unity.com/packages/templates/systems/neofps-fps-controller-template-toolkit-150179 )</p>
<p>Mirror Networking ( https://mirror-networking.com/ ) ( https://assetstore.unity.com/packages/tools/network/mirror-129321 )</p>

# NeoFPS files that need to be modified 
<p>File: FpsGameMode.cs<br>
( This allows the use of FpsNetCharacter and FpsNetPlayerController )<br>
Line 132: protected abstract void ProcessOldPlayerCharacter(ICharacter oldCharacter);<br>
Line 133: protected abstract IController InstantiatePlayer();</p>
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
<p>I suggest using First Gear Games Mirror "Assets For Mirror Networking" (https://www.patreon.com/firstgeargames/posts)<br>
His FlexNetworkTransform is very handy and i have been using his tutorials to help, and seem to be making quite a few assets to improve the Mirror Networking.
</p>
<p>
I also suggest using https://github.com/SoftwareGuy/Ignorance/ transport lay for Mirror, this implaments ENet in to Mirror, to move away from UNet
</p>
