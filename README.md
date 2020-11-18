# NeoFPS_Mirror-Networking

This is an "In Progress" build <br>
NOTE: There are only 2 Working weapon prefab, Basic Hitscan and Ballistic shooter are working<br>
<p>
Currently Working<br>
Client Authority on<br>
Movement, Aim, Enviro Damage, Shooting, weapon change<br>
Server Authority on<br>
Weapon Damage, Health, Ammo<br>
only 1 Network working Weapon currently
</p>
# Lag Compensation
<p>
  I have Intergarted both projectile and hitscan a Lag Compensation<br>
Hitscan is based on https://twoten.dev/lag-compensation-in-unity.html and MLAPI<br>
Projectiles are currently not network based elements and are spawned with a speed increase that diminishes over time
</p>
<p>
  Lag Emulation can be done with https://jagt.github.io/clumsy/download.html<br>
Set Preset to "ipv6 all" and select the lag option.
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
<p>
I also suggest using https://github.com/SoftwareGuy/Ignorance/ transport lay for Mirror, this implaments ENet in to Mirror, to move away from UNet
</p>

# 3rd Party Tools
<p>I suggest using First Gear Games Mirror "Assets For Mirror Networking" (https://www.patreon.com/firstgeargames/posts)<br>
His FlexNetworkTransform is very handy and i have been using his tutorials to help, and seem to be making quite a few assets to improve the Mirror Networking.
</p>
<p>I have intergated FFG's ColliderRollback<br>
Changes needed to First Gear Games are as follow<br>
in "RollbackSteps()" change uint fixedFrame to float frameDiff<br>
Comment out line 81 "uint frameDiff ="<br><br>
Add FIRSTGEARGAMES_COLLIDERROLLBACKS<br>
to Project Setting -> Player -> Other Settings -> Scripting Define Symbols<br>
This will disable free one and enable First Gear Games<br>
</p>
