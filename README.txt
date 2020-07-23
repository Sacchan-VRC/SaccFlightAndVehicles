Sacchan's Flight and Vehicles Prefab
FOR VRChat SDK3 WITH UDONSHARP
https://github.com/Merlin-san/UdonSharp/releases
https://discord.gg/Z7bUDc8
https://twitter.com/Sacchan_VRC
Feel free to give feedback or ask questions
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Major Changes in 1.2
•New Lift model, not floaty any more
•Customizable lift based on Angle of Attack, allowing stalls
•Square input on control stick
•'Exponant' option on control stick for more precise control
•Invert control inputs if plane is moving backwards
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Major Changes in 1.17
•Added fully animated HUD
•Added mannable AAGun
•Improved respawning 
•Changed roll behaviour
•Many small tweaks and fixes
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Hotfix update 1.16
•Fixed Respawn button
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Hotfix update 1.15
•Fixed sync on Gun firing animation and Flaps animation
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Major Changes in 1.14
•Removed other vehicles and attempted to optimized code for SF-1
•Added Flares
•Smoke is now operated by the pilot
•Now makes use of animations for things it makes sense to for, flaps, exploding, gunfire etc.
•Smoke effect when damaged
•Seat Adjuster
•Can no longer destroy your own plane with the machine gun
•Renamed AircraftController to EngineController
•Merged AirplaneGunController into EffectsController
•Atmospheric thinning
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Bugfix update 1.13
2 Small bugfixes.
•Soundcontroller Sonic boom max distance fix
•EffectsController Sea level now always destroys plane
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Bugfix update 1.12
Fixed: Vapor and Wingtrails don't show for people who aren't the owner of the vehicle
Fixed: Bug where respawning vehicles would explode instantly in a loop
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
I Suggest backing up your project before updating.
Major Changes in 1.11
•A lot of variable names have changed so you will have to re-input values on most scripts. This was done because I removed RespawnTrigger.
•Exploding planes. SF-1 now has HP and takes damage from crashing and getting shot.
I have also shuffled around the hierarchy of the SF-1 to support exploding more easily.
•Replaced the respawntrigger with station triggers, and
•Replaced the different LeaveButtons with just one
•Custom Pitching Moment Setting
•Might run a bit faster
•Sea Level option on HUDController, plane explodes if you fly below it.
•Passenger can make display smoke by holding S or clicking the left thumbstick .
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Changes in 1.1
Huge changes, too much to list. Some features:
Added SF-1 plane
Added SoundController
•Doppler effect
•Sonic booms
Added EffectsController
•Control surface movement
•Condensation
•Machspeed vapor
Added HudController
•Speed
•Gs
•Altitude
•Landing Gear and Flaps status
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Changes in 1.02
Default Airplane Pitch Down Str Ratio changed from .6 to .8
Removed redundant object references in AircraftController.cs
Improved how VehicleRespawnButton.cs works
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Changes in 1.01
Roll now decays much faster in airplanes
Added option to disable thrust vectoring
Implemented (unrealistic) increased lift at higher speeds, you can now fall down faster at low speed
the two above combined should allow for more boring plane physics
This package contains the air vehicles and scripts I've been working on. I hope to provide an example of avatar flight and air vehicles people can use as a base to create their own. Feel free to use it however you want.
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

important changes in the SF-1's hierarchy since the last version:
HUDController is now only enabled when inside the vehicle.
The leave buttons are now children of the HUDController object, just to make the explosion animation simpler. Disabling them in the explosion animation and using write defaults when not exploding means they need a parent to be disabled.


HUDController's bigstuff object is scaled to 8000 to make the hud appear to be on the sky like a real HUD. You may want to scale it down to 1 if you plan on editing HUD elements.

To update from the previous version, make sure all the object references are set, and there are no missing references in the animations.

The Gun_pilot now is set to not collide with Playerlocal layer. The plane is set to PlayerLocal for the pilot when he enters, and reverted back to Walkthrough when he leaves.

The prefab uses station triggers, which currently are currently a bit awkward to use. You must set the inspector to debug mode in order to assign them. Enter the name of a public function that is on the script running on this objects UdonBehaviour.
This prefab is only using On Local Player Exit Station, and the function names used for it are PilotLeave, PassengerLeave and GunnerLeave.
I recommend spending as little time as possible with the Inspector in debug mode. I think there may be a bug with it and udon.
See StationTriggersTutorial.jpg or PhaxeNor's tweet here. Thanks PhaxeNor!
https://twitter.com/PhaxeNor/status/1262792675767603201

UdonSharp bug warning: If you disable auto-refresh in unity, make sure to refresh before building for VRChat after code changes. If you do not the build will break, and you will need to re-import udonsharp, and place all of your udon's back into your udonbehaviours.

For control inputs to work you must add the VRChat inputs to your unity project. Just filling in the name entry is fine, It needs them to compile. See inputs.png and inputs.txt

Remember you can test fly the vehicles inside unity by adding a camera to them. You can test buttons by clicking Interact in the inspecter with the object selected(PilotSeat etc)
You can also ofcourse test the animator variables, but some are set every frame so you won't be able to test them without altering or disabling the script.
To test damage to planes/objects, you must enable the Gun_pilot object, as it's disabled by default (The Interact button on the pilotseat also enables it)
All planes always react to inputs in the editor play mode.

It's probably best to break my prefab and remake your own after you've modified the vehicles to your liking and understand how everything works.
If you've made you own vehicle and it's having trouble taking off, try adjusting the center of mass and pitch moment positions

Performance seemed to be suffering a little in testing, I hope to find ways to optimize it in future.


There are 15 udon scripts in this package, I will now explain what each one does, and what each of the variables of each one does. Ctrl-F as needed.


SF-1
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
EngineController.cs--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
This is the main control script for air vehicles. There are values in here that other scripts use to work, for example the bool Passenger is used in the SoundController script. The owner / pilot does almost everything in this script. 
Everyone else just uses it to work out if they've just touched down so soundcontroller can play the touchown sound.

Controls Desktop:
Shift: Thrust Forward
Q: Yaw Left
E: Yaw Right
Arrow Up / W: Pitch Down
Arrow Down / S: Pitch Up
Arrow Left / A: Roll Left
Arrow Right / D: Roll Right
Space: Thrust Up (Helicopter)

Controls VR:
Left Stick Up: Pitch Down
Left Stick Down: Pitch Up
Left Stick Left: Yaw Left
Left Stick Right: Yaw Right
Right Stick Up: Pitch Down
Right Stick Down:  Pitch Up
Right Stick Left: Roll Left
Right Stick RIght: Roll Right
Left Trigger: Thrust Up
Right Trigger: Thrust Forward
A Button: Toggle Flaps
X Button: Toggle Landing Gear

Variables:

Vehicle Main Obj
The script needs access to the Vehicle's main object.

Ground Detector
Empty object who's location is used to detect whether or not the vehicle is touching the ground. If gear is down the code traces from it's position, local down 44cm, and if it hits something it enables taxxiing. Including the vehicle itself, so place it between the wheels or something.

Effects Control
Effects controller goes in here, used to access variables in effects controller

Sound Control
Sound controller goes in here, used to access variables in sound controller

Hud Control
Hud controller goes in here, used to access variables in hud controller

Center of Mass (new in 1.1)
The aircraft's center of mass. Useful to adjust how long it takes to take off.

Pitch Moment (new in 1.04)
The point at which force is added to make the vehicle pitch. If not set, vehicle will rotate around center of mass. When set, Pitch Strength will likely need to be set to a much lower value.

Airplane Thrust Vectoring (new in 1.01)
Tick if you want an airplane to be able to turn quickly even at low speeds.

Airplane Thrust Vec Str (new in 1.04)
minimum rotatation speed value for vehicles that are moving slowly. Only affects if Airplane Thrust Vectoring is enabled.

Throttle Strength Forward
Value put into the Constant Force's value corresponding to forward.

Air Friction
Amount per frame the vehicle's velocity is lerped towards MaxSpeed.

Pitch Strength
Value put into the Constant Force's value corresponding to Pitch.

Yaw Strength
Value put into the Constant Force's value corresponding to Yaw.

Roll Strength
Value put into the Constant Force's value corresponding to Roll.

Acceleration Response
How long it takes for the throttle to reach max after you press and hold it (it's lerped).

Rotation Response
How long it takes for the rotation to reach max after you press and hold it (it's lerped).

Vel Straighten Str Pitch
How much the the vehicle's nose is pulled toward the direction of movement on the pitch axis (uses the constantforce).

Vel Straighten Str Yaw
How much the the vehicle's nose is pulled toward the direction of movement on the yaw axis (uses the constantforce).

Taxi Rotation Speed
Degrees per second the vehicle rotates on the ground. Uses simple object rotation, no physics, should probably be improved.

Airplane Pitch Down Str Ratio
Allows you to set a seperate rotation speed for pulling down.

Airplane Lift
Amount of lift the wings generate. Or amount of local thrust upward generated when your velocity vector is toward the bottom of your vehicle, and vice-versa.

Airplane Vel Lift Coefficient (new in 1.01)
Adjust the curve of how much more lift is generated from moving faster.

Airplane Pull Down Lift Ratio
Allows you to generate less lift from pulling down.

Airplane Sideways Lift
How much angle of attack on yaw affects vehicles velocity vector. Yaw steering strength?

Airplane Vel Pull up
Vehicle will pull up slightly depending on it's speed multiplied by this value. Used to counter the fact that without it, your nose will slowly point down.

Airplane Roll Friction (new in 1.1)
How quickly the plane will stop rolling after you let go of roll controls. Also increases the faster you go.

Has Flaps (new in 1.1)
Whether or not the vehicle has flaps and does physics/animations for them.

Has Landing Gear (new in 1.1)
Whether or not the vehicle has landing gear and does physics/animations for them.

Landing Gear Drag Multi (new in 1.1)
How much extra drag you incur from having landing gear deployed.

Flap Drag Multi (new in 1.1)
How much extra drag you incur from having flaps deployed.

Flaps Lift Multi (new in 1.1)
How much extra lift the flaps give you.

Health
Total health of the plane

Sea Level
Height of the sea in global coordinates, the hud's height is based with this at 0. Plane will instantly explode if below this height.

Atmosphere Thinning Start
Height(above Sea Level) at which the 'atmosphere' starts getting thinner. Plane starts losing maneuverability and thrust at this height.

Atmosphere Thinning End
Height at which the 'atmosphere' finishes getting thinner. Plane cannot maneuver or thrust at all beyond this height


EffectsController.cs (new in 1.1)--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Controls vehicle's control surface movement, vapor effects, and explosions.
If you want an easy way to set up your empties at the the right angles for your plane, try copying and dragging the empties from the SF-1 to yours and adjust them from there.
Optimizations: The DoEffects variable tracks whether or not a player is inside the vehicle, or if they have left. If they have left more than 10 seconds ago the animations will stop,
This script also checks if the plane is further away than 2km and if it is, only does vapor effects, because everything else will be so tiny on your screen. If you have a remote camera set up, you probably want to remove this code.
Variables:

Vehicle Main Obj
The script needs access to the Vehicle's main object.

Pilot Seat Station
Needs the pilot's station to eject the player when exploded.

Passenger Seat Station
Needs the passenger's station to eject the player when exploded.

Aileron L
Aileron R
Canards
Elevator
Engine L
Engine R
Rudder L
Rudder R
All of these are gameobject inputs that contain empties that contain meshes, they are animated based on the SF-1 plane. You will probably have to change the script if you make your own vehicle.
Most of them are children of an empty that sets orientation, they then animate by rotating on just one local axis.

Slats L
Slats R
These rotate to point forward as speed increases.

Enginefire L
Enginefire R
Scaled larger on Z with throttle, disappear if too small.

Mach Vapor
Particle System that appears when you are going approximately mach 1.

G Vapor L
G Vapor R
Particle system that appears when pulling Gs.

Wing Trail L
Wing Trail R
Trail renderers that start emitting when you pull enough Gs. Located on the wing tips.

Trail Gs
How many Gs you have to pull before wing trails starts appearing.

Max Gs
amount of Gs at which you will take damage if you go past. You take damage of 15 per second per G above MaxGs. This is what hurts you when you crash too.

GDamage
Damage taken Per G above maxGs, per second.
Gs - MaxGs = damage/second


HitDetector.cs (new in 1.11)--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Detects if a particle hit the vehicle, and if it does, take off 10HP(if owner) and play the bullet hit sound.
This script also contains functions used by the animator on the vehicle's main object because it can't use functions from other objects.

Variables:

EngineControl
Needs this because the Health value is containted in EngineController.

SoundControl
Needs this to play the sound.


HUDController.cs (new in 1.1)--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Calculates information about the vehicle and sends an output to a Text script.

EngineControl
Required to know information about the vehicle. EngineController goes here.

HUD Text_G
HUD Text_mach
HUD Text_altitude
HUD Text_knots
HUD Text_angleofattack
Output Text scripts, corresponding canvas text objects go here.

Distance from Head
HUD's distance from the head when scale 1, used to move the velocity vector to the right position

Down Indicator
Hud object that points to the ground

Elevation Indicator
Hud object that shows pitch angle

Heading Indicator
Hud object that shows yaw angle

Velocity Indicator
Hud object that shows what direction vehicle moving

LeaveVehicleButton.cs (new in 1.1)--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Used to leave pilot seat of vehicles.

Controls Desktop:
R: Leave vehicle

Controls VR:
Left Menu Button: Leave Vehicle

Variables:

Engine Control
Engine Controller is needed for getting the velocity of the vehicle, so the local player can inherit that speed as they leave.

Seat
Station we're getting out of.


PassengerSeat.cs (new in 1.1)--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Used to enter as a passenger of vehicles, and to set things back after you leave. Also contains the smoke function control for the passenger.
Controls Desktop:
S: Produce Smoke

Controls VR:
Click Left Thumbstick: Produce Smoke

Variables:

Engine Control
Used to tell the vehicle you're a passenger.

Passenger Leave Button
Used to enable and disable the leave button. Passenger Leave Button goes here.

Seat Adjuster
Object containing the Seat Adjuster script. Enabled when enterering, and disabled when leaving(if not already disabled by itself)

Saccflight
Used to disable and enable Saccflight. Saccflight goes in here.


PilotSeat.cs--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Used to enter as pilot of vehicles.
Variables:

Vehicle Main Obj
Needed to set player as the owner of the vehicle's main object.

Engine Control
Player must be the owner of this to operate the vehicle. also sets 'piloting'

Leave Button
Enable the leave button. PilotLeaveButton goes in here.

Saccflight
Disables Saccflight when inside. Saccflight goes in here.

Gun_Pilot
Used to enable the invisible gun that only the pilot fires, (the one that actually does damage)

PlaneMesh
Put the parent object of the plane's mesh objects here. Sets it and all it's children to 'Playerlocal' layer when you enter, and back to 'Walkthrough' when you leave. This is so that the Gun_Pilot doesn't hit your own plane.
Do not put any colliders as child of any other object, or the gun might hit them.

Seat Adjuster
Object containing the Seat Adjuster script. Enabled when enterering, and disabled when leaving(if not already disabled by itself)

Enable Other
Put any other object you want to be enabled locally whilst you are in the plane here.


SoundController.cs (new in 1.1)--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Controls sound. By default in unity editor it will test the inside cockpit piloting sounds. Change various "playerlocal == null" to "playerlocal != null" inside if statements to test others.
See the comments in the script for more details
Optimizations:The script will do nothing if the vehicle is beyond the range of the the Sonic Boom's max distance (This is assumed to be the loudest/longest range sound).

Variables:

Engine Control
The script needs access to information about the vehicle to control sound. EngineController goes here.

Plane Idle
Played while the plane is occupied, increases pitch with throttle.

Plane Inside
Same as Plane Idle but only people inside the plane can hear it.

Plane Distant
Engine sound of the plane when it is far away.

Play Afterburner
Engine sound of the plane when it is close.

Touch Down
Plane touchdown sound. Plays when the wheels touch the ground.

Plane Wind
Air rushing sound, increases volume with speed and pulling Gs.

Sonic Boom
Sound played when the plane flies past you at over mach 1.

Explosion
Boom sound

Bullet Hit
Plays when the plane gets hit by a particle.

Gun Sound
Plays when firing the gun

Testcamera
Used only to test inside unity editor. Calculates doppler from this transform. Doesn't do anything in-game.


VehicleRespawnButton.cs--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Used to respawn vehicles on button press. Only respawns the vehicle if EngineController.Occupied = false. Sets flaps on and gear down.
If your vehicle fails to respawn in-game make sure the 'Synchronize Position' tickbox is checked on the vehicle's main object's udonbehaviour.
Variables:

Vehicle Main Obj
The script needs access to the vehicle's main object in order to move it. Vehicle's main object goes in here.

Engine Control
Needed to tell the plane to set certain settings when respawning.

AAGun
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
AAGunController.cs--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Main Control script for the AAGun
Controls Desktop
W: Pitch up
S: Pitch down
A: Yaw left
D: Yaw right
Shift: Zoom in
Ctrl: Zoom out

Controls VR
Right stick up: Pitch up
Right stick down: Pitch down
Right stick left: Yaw left
Right stick right: Yaw right
Left stick up: Zoom in
Left stick down: Zoom out

Rotator
Object that rotates for aiming

Vehicle Main Obj
Used for various stuff like check owner, and getting the animator

AA Gun Seat Station
Needed to eject player when exploding

AA Cam
Camera used inside the cockpit, needed to control zoom

Turn Speed Multi
Maximum speed of turning

Turning Response
How long it takes to get to max speed

Stop Speed
How much friction is applied when slowing turning

Zoom Fov
FOV when zoomed all the way in

Zoom Out Fov
FOV when zoomed all the way out

Health
Total health, takes 10 damage per hit taken.

AAGunSeat.cs--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Used to enter the AAGun

AA Gun Control
AA Gun Controller is needed to set manning = true, and loads of other stuff

HUD Controller
Needed to activate the HUD and other stuff inside the vehicle when you enter

Saccflight
Disabled SaccFlight when player enters and re-enable when player leave

Seat Adjuster
Needed to enable the seat adjuster when you enter


HitDetectorAAGun.cs--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Used to detect hits on the AAGun, and also has functions that are called by the animator.
AA Gun Control
AAGunController is needed to reduce health on hit

Bullet Hit
Audio source for being hit by a bullet


HUDControllerAAGun.cs--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Calculates information about the vehicle and sends an output to a Text script.

AA Gun Control
Required to know information about the vehicle. AAGunController goes here.

Elevation Indicator
Hud object that shows pitch angle

Heading Indicator
Hud object that shows yaw angle


LeaveAAGun.cs--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Used on the button to leave the AAGun

Controls Desktop:
R: Exit AAGun

Controls VR:
Left menu button: Exit AAGun

AA Gun Control
Used for the localplayer reference

Seat
Used to leave the seat


SwivelScreenToggle.cs--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Used to control the screen position toggler

Screen
The object with the swivel animator on it.


SaccTarget
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
SaccTarget.cs--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
This is used in combination with an animator to create an object that can be destroyed by shooting it.

Variables:

HitPoints
How many HitPoints the target has. It takes 10 damage per bullet that hits it.


Other
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
GunnerSeat.cs--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Used to enter the gunner seat of the gunship. Gunship was removed but the code might be useful.
Variables:

Leave Button
Enable the leave button. GunnerLeaveButton goes in here.

Saccflight
Disables Saccflight when you enter, reenables when you exit. Saccflight goes in here.

Gun Object
Sets the person who enters as the owner of the gun. Gun goes in here.

Gun Controller
Script makes you the owner of the guncontroller and controls the value 'manning'.


GunshipGunController.cs--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Takes head orientation, trigger/mouse0 input and controls the gun's orientation and particle system's emission. Gunship was removed but the code might be useful.
Controls Desktop:
Left Click: Fire Gun

Controls VR:
Right Trigger: Fire Gun

Variables:

EngineController
Needed to use the VRCPlayerAPI contained within.

Gun
Used to control orientation of gun model. Gun goes in here.

Gun Particle
Script emits particles from this particle system. muzzleflash goes in here.


SaccFlight.cs--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
For flight without a vehicle, only does anything if you're not grounded. So jump first.

Controls Desktop:
Space: Fly Straight Up
F: Fly towards Head bone's 'forward' direction (doesn't work right with every avatar)

Controls VR:
Left Trigger: Fly Straight Up
Right Trigger: Fly towards direction right hand is pointing

Variables:

FlyStr
Speed of acceleration.

Back Thrust Strength
Amount of extra thrust applied when trying to slow down (Thrusting away from velocity vector)


SaccSeatAdjuster.cs--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
This script adjusts the height of a seat object until your avatar's head bone is within 1cm of the target height object, then disables itself.
It is enabled when entering the seat, does its thing, then disables itself.
When enabled, it resets the seat to its original position before proceeding.
This script uses broadcasts to sync the position of the seat.

Variables

Seat
The object that will move (usually the Seat object of a VRCChair)

Target Height
Empty with transform position at desired head height

Head Test
You can use this to test the script inside unity. Just put an object as the child of the seat object, position it somewhere bad for piloting, and then when in play mode, enable the SaccSeatAdjuster object, and it should move into position.



Have fun!