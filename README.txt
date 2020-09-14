Sacchan's Flight and Vehicles Prefab
FOR VRChat SDK3 WITH UDONSHARP
https://github.com/Merlin-san/UdonSharp/releases
https://discord.gg/Z7bUDc8
https://twitter.com/Sacchan_VRC
Feel free to give feedback or ask questions
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Major Update 1.3
•VR Motion Controls for Joystick and Throttle
•Function Dials with 16 functions for VR control
•Air-to-air missiles
•Air-to-ground missiles
•Bombs
•Fuel system
•Ammo/Fuel/resupply system and animations
•Arresting hook for aircraft carrier landings
•Arresting cable prefab
•Catapult launch functionality for carrier takeoffs
•Catapult prefab
•Afterburner
•Air/ground brake
•Customizable smoke color
•Flight limits safety mode
•Cruise mode
•Altitude hold autopilot
•Animated canopy with sound changes
•Customizable wind functionality
•Takeoff assist options
•Sound barrier friction options
•Many new sounds
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Hotfix 1.21
Fixed plane not exploding in editor test
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Major Changes in 1.2
Huge code changes in this version, back up your projects.
•New Lift model, not floaty any more
•Customizable lift based on angle of attack, allowing stalls
•Inverts control inputs if plane is moving backwards
•Seperate thrust vectoring axis' strength
•Square input on control stick, so that controller users don't have disadvantage against keyboard users
•'Power' option for control stick for more precise input
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
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Some of the animations use the 'Normalized Time' feature, which makes the animation be controlled by a float parameter (which is controlled by effectscontroller).
float value 1(or more) = play the last frame of the animation
float value 0(or less) = play the first frame of the animation
and everything in between accordingly.

The MachVapor for example uses the mach10 variable. This variable = 1 at mach10, and 0 when not moving. The animation is 1000 frames long, and the MachVapor is enabled on frame 97 and disabled on frame 102, which corresponds to mach1.
This can be used to do other things like F-14 wings moving back at a certain speed.

The angle of attack variable is 0 at 0 AoA, and 1 at 180.

The prefab uses station triggers, which currently are currently a bit awkward to use. You must set the inspector to debug mode in order to assign them. Enter the name of a public function that is on the script running on this objects UdonBehaviour.
This prefab is only using On Local Player Exit Station, and the function names used for it are PilotLeave, PassengerLeave and GunnerLeave.
I recommend spending as little time as possible with the Inspector in debug mode. I think there may be a bug with it and udon.
See StationTriggersTutorial.jpg or PhaxeNor's tweet here. Thanks PhaxeNor!
https://twitter.com/PhaxeNor/status/1262792675767603201

For control inputs to work you must add the VRChat inputs to your unity project. Just filling in the name entry is fine, It needs them to compile. See inputs.png and inputs.txt

Remember you can test fly the vehicles inside unity by adding a camera to them. You can test buttons by clicking Interact in the inspecter with the object selected(PilotSeat etc)
You can also ofcourse test the animator variables, but some are set every frame so you won't be able to change them without altering or disabling the script.
To test the gun damage to planes/objects, you must enable the Gun_pilot object, as it's disabled by default (The Interact button on the pilotseat also enables it)
All planes will react to inputs in the editor play mode.

It's best to break my prefab in order to make your own planes.
If you want more than one plane
If you've made you own vehicle and it's having trouble taking off, try adjusting the center of mass and pitch moment positions, and also the takeoff assist options

HUDController is only enabled when inside the vehicle.
Objects that are only enabled while inside the plane are children of the HudController.

HUDController's bigstuff object is scaled to 8000 to make the hud appear to be on the sky like a real HUD. You may want to scale it down to 1 if you plan on editing HUD elements.

The Gun_pilot is set to not collide with reserved2 layer. The plane is set to reserved2 you enter, so you can't shoot himself, and reverted back to Walkthrough when he leaves.

There are 22 udon scripts in this package, I will now explain what each one does, and what each of the variables of each one does. Ctrl-F as needed.

SF-1
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
AAMController(new in 1.3)--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Controls movement of Air-to-air missiles.
Variables:

Engine Control
Missile's plane's EngineController, needed to know target.

Explosion Sounds
Array of sounds one of which will play for the explosion.

Collider Active Distance
Missile's collider is inactive when it is spawned. After it's this far away from the plane it launched from, it becomes active. This is to prevent it from hitting the plane it launched from. The faster the plane is moving, the higher this number needs to be due to ????Physics.

Rot Speed
angle per second that the missile can turn while chasing it's target.


AGMController(new in 1.3)--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Controls movement of Air-to-ground missiles.
Variables:

Engine Control
Missile's plane's EngineController, needed to know target.

Explosion Sounds
Array of sounds one of which will play for the explosion.

Collider Active Distance
Missile's collider is inactive when it is spawned. After it's this far away from the plane it launched from, it becomes active. This is to prevent it from hitting the plane it launched from. The faster the plane is moving, the higher this number needs to be due to ????Physics.

Lock Angle
If the missile's target is within this angle, it will follow it.

Rot Speed
angle per second that the missile can turn while chasing it's target.


BombController(new in 1.3)--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Controls movement of Bombs.
Variables:

Engine Control
Missile's plane's EngineController, needed to know target.

Explosion Sounds
Array of sounds one of which will play for the explosion.

Collider Active Distance
Missile's collider is inactive when it is spawned. After it's this far away from the plane it launched from, it becomes active. This is to prevent it from hitting the plane it launched from. The faster the plane is moving, the higher this number needs to be due to ????Physics.

Straighten Factor
strength of the force making the bomb face towards the direction it's moving.

Air Physics Strength
Strength of 'air' acting on the bomb's movement


EffectsController.cs (new in 1.1)--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Controls vehicle's control surface movement and vapor effects, also contains synced variables as there's too many for one script.
If you want an easy way to set up your empties at the the right angles for your plane, try copying and dragging the empties from the SF-1 to yours and adjust them from there.
Optimizations: The DoEffects variable tracks whether or not a player is inside the vehicle, or if they have left. If they have left more than 10 seconds ago the animations will stop,
This script also checks if the plane is further away than 2km and if it is, only does vapor effects, because everything else will be so tiny on your screen. If you have a remote camera set up, you probably want to remove this code(line 102).
Variables:

Vehicle Main Obj
The script needs access to the Vehicle's main object.

Engine Control
Engine Controller is needed to know vehicle's control inputs.

Joy Stick
Joystick mesh object, animates according to rotational inputs.

Ailerons
Elevators
Rudders
Canards
Engines
Front Wheel
Display Smoke
All of these are transform inputs, most of them are children of an empty that sets orientation, they are animated by rotating on one local axis.
To adjust rotation axis, rotate the parent empty

Enginefire
Scaled along once axis according to if Afterburner is on, else it's scale 0, disabled if throttle is 0.


EngineController.cs--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
This is the main control script for air vehicles. There are values in here that other scripts use to work, for example the bool Passenger is used in the SoundController script. The owner / pilot does almost everything in this script. 
Everyone else just uses it to work out if they've just touched down so soundcontroller can play the touchown sound.

Controls Desktop:
Shift: Increase Forward
Ctrl: Decrease Forward
Q: Yaw Left
E: Yaw Right
Arrow Up / W: Pitch Down
Arrow Down / S: Pitch Up
Arrow Left / A: Roll Left
Arrow Right / D: Roll Right
GUN - 1
AAM - 2
AGM - 3
BOMB - 4
    Weapons: Space to fire
GEAR - G
FLAPS - F
HOOK - H
SMOKE - 5
AFTERBURNER - T
FLT. LIMITS - F1
FLARE - X
CATAPULT - C
BRAKE - B
ALT. HOLD - F3
CANOPY - Z
CRUISE - F2
Exit - Return
num7/4 - Smoke R up/down
num8/5 - Smoke G up/down
num9/6 - Smoke B up/down
= - adjust cruise speed

Controls VR:
While Gripping Left Grip: Throttle Control
While Gripping Right Grip: Joystick Control
A Button: Toggle Flaps
X Button: Toggle Landing Gear

Variables:

Vehicle Main Obj
The script needs access to the Vehicle's main object.

Leave Buttons (new in 1.3)
An array of objects with the LeaveButton script on them, used for exiting each seat of the plane. needed to drop you out of the plane when it explodes

Effects Control
Effects controller goes in here, used to access variables in effects controller

Sound Control
Sound controller goes in here, used to access variables in sound controller

Hud Control
Hud controller goes in here, used to access variables in hud controller

Center of Mass (new in 1.1)
The aircraft's center of mass. Useful to adjust how long it takes to take off.

Pitch Moment (new in 1.04)
The point at which force is added to make the vehicle pitch. If not set, script will fail.

Yaw Moment (new in 1.2)
The point at which force is added to make the vehicle yaw. If not set, script will fail.

Ground Detector
Empty object who's location is used to detect whether or not the vehicle is touching the ground. If gear is down the code raycasts from it's position, local down 44cm, and if it hits something it enables taxxiing.
The resupply check raycast is also checked from this transform.

Hook Detector (new in 1.3)
Position where the raycast to detect if you've caught a cable with the hook starts.

Resupply Layer (new in 1.3)
Layer on which raycast looks for triggers to detect if you're in a resupply zone.

Hook Cable Layer (new in 1.3)
Layer on which raycast looks for triggers to detect if you've caught a cable with your hook.

Catapult Detector (new in 1.3)
Position where the raycast to detect if you're on a catapult starts (usually at the front wheel).

Catapult Layer (new in 1.3)
Layer on which the raycast looks for triggers to detect if you're on a catapult.

AAM (new in 1.3)
Air to air missile object, one of these is spawned when you fire an AAM.

Num AAM (new in 1.3)
The number of AAMs on the plane.

AAM Max Target Distance (new in 1.3)
Distance that if a target is within which it is targetable.

AAM Lock Angle (new in 1.3)
If the missile's target is within this angle, it will follow it.

AAM Lock Time (new in 1.3)
Time before you can fire an air-to-air missile after acquiring a target.

AAM Launch Point (new in 1.3)
Point at which air-to-air missiles spawn, alternates left-right each time fired.

AAM Targets Layer (new in 1.3)
Layer on which AAM target objects are, script searches for triggers on this layer as targets.

AGM (new in 1.3)
Air to ground missile object, one of these is spawned when you fire an AGM.

Num AGM (new in 1.3)
The number of AGMs on the plane.

AGM Launch Point (new in 1.3)
Point at which air-to-ground missiles spawn, alternates left-right each time fired.

AGM Targets Layer (new in 1.3)
Layer on which AGM target objects are, script searches for triggers on this layer as targets.

At G Cam (new in 1.3)
Camera object used to view targets with the AGM and Bomb

Bomb (new in 1.3)
Bomb object, one of these is spawned when you drop a bomb.

Num Bomb (new in 1.3)
The number of Bombs on the plane.

Bomb Hold Delay (new in 1.3)
How long between bomb drops if you hold the button.

Bomb Launch Points (new in 1.3)
Points at which bombs spawn, they spawn at each point in succession.

Gun Ammo In Seconds (new in 1.3)
How long the gun can be fired before it runs out of ammo.

Has Afterburner
Has Limits
Has Catapult
Has Hook
Has Flare
Has Catapult
Has Brake
Has Alt Hold
Has Canopy
Has Cruise
Has Gun
Has AAM
Has AGM
Has Bomb
Has Gear
Has Flaps
Has Hook
Has Smoke
(new in 1.3)
These options effectively disable each function of the plane, making them unselectable. Recomend disabling menu objects from the display screens correspondingly.

Throttle Strength
Value put into the Constant Force's value corresponding to forward.

Afterburner Thrust Multi (new in 1.3)
Thrust is multiplied by this amount when afterburner is enabled.

Acceleration Response
How long it takes for the throttle to reach max after you press and hold it (it's lerped).

Engine Spool Down Speed Multi
Multiplies the number above but for when you're slowing down the engine.

Air Friction
Amount per frame the vehicle's velocity is lerped towards MaxSpeed.

Pitch Strength
Yaw Strength
Roll Strength
Strength of rotation on the respective axis.

Pitch Thrust Vec Str (new in 1.2)
Yaw Thrust Vec Str
Roll Thrust Vec Str
Minimum rotational strength for each axis. If non-zero then the plane will not invert it's rotation controls on that axis when moving backwards.

Pitch Friction (new in 1.2)
Yaw Friction
Roll Friction
How much friction is applied to stop you from moving on each axis.

Pitch Response (new in 1.2)
Yaw Response
Roll Response
How long it takes for the rotational inputs to reach max after you press and hold it (it's lerped).

Reversing Pitch Strength Multi (new in 1.2)
Reversing Yaw Strength Multi
Reversing Roll Strength Multi
When moving backwards through the air, your plane's rotational inputs can but multiplied. Does'nt effect axis with thrust vectoring enabled.

Pitch Down Str Multi
Allows you to set a seperate rotation speed for pulling down.

Pitch Down Lift Multi
Allows you to generate less lift from pulling down. (air hitting the top of your plane)

Rot Multi Max Speed (new in 1.2)
Rotational inputs are multiplied by current speed to make flying at low speeds feel heavier. Above the speed input here, all inputs will be at 100%. Linear.

Vel Straighten Str Pitch
How much the the vehicle's nose is pulled toward the direction of movement on the pitch axis.

Vel Straighten Str Yaw
How much the the vehicle's nose is pulled toward the direction of movement on the yaw axis.

Max Angle Of Attack Pitch (new in 1.2)
Max Angle Of Attack Yaw
Angle of attack above which the plane will lose control.

Aoa Curve Strength (new in 1.2)
Shape of the angle of attack lift curve.
See this to understand (the 2 in the input represents this value): https://www.wolframalpha.com/input/?i=-%28%281-x%29%5E2%29%2B1

High Aoa Mine Control Pitch (new in 1.2)
High Aoa Mine Control Yaw
The angle of attack curve is augmented by being MAX'd(taking the higher value) with a linear curve that is multiplied by this number.
Use this value to decide how much control the plane has when beyond it's 'max' angle of attack.

High Pitch Aoa Min Lift (new in 1.2)
High Yaw Aoa Min Lift
When the plane is is at a high angle of attack you can give it a minimum amount of lift/drag, so that it doesn't just lose all air resistance.

Taxi Rotation Speed
Degrees per second the vehicle rotates on the ground. Uses simple object rotation with a lerp, no real physics to it.

Taxi Rotation Response
How smoothed the taxi movement rotation is

Lift
Adjust how steep the lift curve is. Higher = more lift

Sideways Lift
How much angle of attack on yaw turns vehicles velocity vector. Yaw steering strength?

Max Lift (new in 1.2)
Maximum value for lift, as it's exponential it's wise to stop it at some point.

Vel Lift
Push the vehicle up based on speed. Used to counter the fact that without it, your nose will slowly point down.

Max Gs
amount of Gs at which you will take damage if you go past. You take damage of 15 per second per G above MaxGs. This is what hurts you when you crash too.

GDamage
Damage taken Per G above maxGs, per second.
Gs - MaxGs * GDamage = damage/second

Landing Gear Drag Multi (new in 1.1)
How much extra drag you incur from having landing gear deployed.

Flaps Drag Multi (new in 1.1)
How much extra drag you incur from having flaps deployed.

Flaps Lift Multi (new in 1.1)
How much extra lift the flaps give you.

Airbrake Strength (new in 1.3)
Strength of the airbrake.

Ground Brake Strength (new in 1.3)
Strength of the breaking in meters per second when on the ground and below the Ground Brake Speed.

Ground Brake Speed (new in 1.3)
Speed below which the the ground brake takes effect.

Hooked Brake Strength (new in 1.3)
Strength of the breaking in meters per second when the hook catches an arresting cable.

Catapult Launch Strength (new in 1.3)
Strength of the force that pushes you forward when launching from the catapult. Same units as Thrust Strength.

Catapult Launch Time (new in 1.3)
How long the plane takes to launch (reach the end of the catapult).

Catapult Launch Strength and Time must be adjusted together so that the plane finishes launching as it reaches the end of the catapult.

Takeoff Assist (new in 1.3)
Maximum extra pitch strength given to the plane when it's on the ground, and moving. Strength increases until it reaches Takeoff Assist Speed.

Takeoff Assist Speed (new in 1.3)
Speed at which Takeoff assist reaches maximum strength

G Limiter (new in 1.3)
Controls the Flight Limits function. It tries to keep the plane below this number of Gs by reducing input strength linearly as your Gs increase, until this number. Usually needs to be set it a bit above the value you want to limit to.

Ao A Limit (new in 1.3)
(Angle of Attack Limit) Controls the Flight Limits function. It tries to keep the plane below this angle of attack reducing input strength linearly as your angle of attacked increases, until this number.

Canopy Close Time (new in 1.3)
Time it takes for the canopy animation to play. Used to switch the sound effects from outside to inside at the right moment.

Health
Max health of the plane.

Sea Level
Height of the sea in global coordinates, the hud's height is based with this at 0. Plane will instantly explode if below this height.

Wind (new in 1.3)
Strength of wind on each axis

Wind Gust Strength (new in 1.3)
Strength of wind gusts, which are added to wind direction, and change direction according to Wind Gustiness.

Wind Gustiness (new in 1.3)
How often wind gusts change direction.

Wind Turbulance Scale (new in 1.3)
Scale of the wind gusts, direction of gusts is different according to where you are.

Sound Barrier Strength (new in 1.3)
Extra drag applied when moving around the speed of sound.

Sound Barrier Width (new in 1.3)
As you approach the speed of sound, more friction is applied according to 'Sound Barrier Strength'. It increases linearly from 'Sound Barrier Width' distance away from the speed of sound. Also decreases linearly after the speed of sound. (Meters per second)

Atmosphere Thinning Start
Height(above Sea Level) at which the 'atmosphere' starts getting thinner. Plane starts losing maneuverability and thrust at this height.

Atmosphere Thinning End
Height at which the 'atmosphere' finishes getting thinner. Plane cannot maneuver or thrust at all beyond this height

Fuel (new in 1.3)
Amount of fuel the plane has.

Fuel Consumption (new in 1.3)
Amount of fueld consumed per second by the plane.

Fuel Consumption AB Multi (new in 1.3)
Multiplier for how much fuel is consumed when the afterburner is enabled.


HitDetector.cs (new in 1.11)--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Detects if a particle hit the vehicle, and if it does, take off 10HP(if owner) and play the bullet hit sound.
This script also contains functions used by the animator on the vehicle's main object because it can't use functions from other objects.

Variables:

EngineControl
Needs this because the Health value is containted in EngineController.


HUDController.cs (new in 1.1)--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Calculates information about the vehicle and applies output to the HUD. Also controls the displays inside the cockpit.

EngineControl
Required to know information about the vehicle. EngineController goes here.

Smoke Color Indicator
Material that is used by the display to highlight when smoke is on. It shows the smoke color.

HUD Text_G
HUD Text_mach
HUD Text_altitude
HUD Text_knotstargets
HUD Text_knots
HUD Text_angleofattack
HUD Text_AAM_ammo
HUD Text_AGM_ammo
HUD Text_Bomb_ammo
Output Text scripts, corresponding canvas text objects go here.

Hud Crosshair Gun
Hud Crosshair 
Hud Hold
Hud Limit
Hud AB
Hud elements that are enabled/disabled by the scrip

Down Indicator
Hud object that points to the ground

Elevation Indicator
Hud object that shows pitch angle

Heading Indicator
Hud object that shows yaw angle

Velocity Indicator
Hud object that shows what direction vehicle moving

AAM Target Indicator
Hud object that appears over targets AAMs can shoot at

Pitch Roll
Hud object that display inputs for pitch and roll

Yaw
Hud object that display inputs for yaw

L Stick Display Highlighter
R Stick Display Highlighter

At G Screen
The screen object that displays assists targeting for AGMs and bombs

L Stick_funcon1-8
R Stick_funcon3-8
Function highlight objects that are enabled when the function is on.



LeaveVehicleButton.cs (new in 1.1)--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Used to leave pilot seat of vehicles.

Controls Desktop:
Return: Leave vehicle

Controls VR:
Left Menu Button: Leave Vehicle

Variables:

Engine Control
Engine Controller is needed for getting the velocity of the vehicle, so the local player can inherit that speed as they leave.

Seat
Station we're getting out of.


PassengerSeat.cs (new in 1.1)--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Used to enter as a passenger of vehicles, and to set things back after you leave.
Variables:

Engine Control
Used to tell the vehicle you're a passenger.

Leave Button
Used to enable and disable the leave button. Passenger Leave Button goes here.

Plane Mesh
Put the parent object of the plane's mesh objects here. Sets it and all it's children to 'Playerlocal' layer when you enter, and back to 'Walkthrough' when you leave. This is so that the Gun_Pilot doesn't hit your own plane.
Do not put any colliders as child of any other object, or the gun might hit them.

Seat Adjuster
Object containing the Seat Adjuster script. Enabled when enterering, and disabled when leaving(if not already disabled by itself)


PilotSeat.cs--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Used to enter as pilot of vehicles.
Variables:

Engine Control
Player must be the owner of this to operate the vehicle. also sets 'piloting'

Leave Button
Enable the leave button when you get in.

Gun_Pilot
Used to enable the invisible gun that only the pilot fires, (the one that actually does damage)

Plane Mesh
Put the parent object of the plane's mesh objects here. Sets it and all it's children to 'Playerlocal' layer when you enter, and back to 'Walkthrough' when you leave. This is so that the Gun_Pilot doesn't hit your own plane.
Do not put any colliders as child of any other object, or the gun might hit them.

Seat Adjuster
Object containing the Seat Adjuster script. Enabled when enterering, and disabled when leaving(if not already disabled by itself)


SoundController.cs (new in 1.1)--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Controls sound. By default in unity editor it will test the inside cockpit piloting sounds. To test outside sounds, comment out '&& !EngineControl.Piloting' on line 239 and change -100000 to 100000 on line 277
See the comments in the script for more details
Optimizations:The script will do nothing if the vehicle is beyond the range of the the sound (see MaxAudibleDistance in code).

Variables:

Engine Control
The script needs access to information about the vehicle to control sound. EngineController goes here.

Plane Idle
Played while the plane is occupied, increases pitch with throttle.

Plane Inside
Same as Plane Idle but only people inside the plane can hear it.

Plane Distant
Engine sound of the plane when it is far away.

Thrust
Engine sound of the plane when it is close.

AB On Inside

AB On Outside

Touch Down
Plane touchdown sound. Plays when the wheels touch the ground.

Plane Wind
Air rushing sound, increases volume with speed and pulling Gs.

Sonic Boom
Sound played when the plane flies past you at over mach 1.

Explosion
Boom sound.

Gun Sound
Plays when firing the gun.

Bullet Hit
Plays when the plane gets hit by a particle.

Rolling
Sound made by the wheels on rolling on the runway.

Reloading
Sound played every second when reloading/refueling.

Radar Locked
Sound played when enemy plane is targeting you with AAM.

Missile Incoming
Sound played when a missile is tracking you.

AAM Targeting
Sound played when you're targeting something with AAM.

AAM Target Lock
Sound played when target is locked with AAM.

AGM Lock
Sound played when you lock a target with AGM.

AGM Unlock
Sound played when you unlock AGM target.

Airbrake
Sound made by the airbrake.

Catapult Lock
Sound made when the plane is locked into place on a catapult.

Catapult Launch
Sound made when the plane is launched on a catapult.

Menu Select
Sound made every time menu selection is changed.

Testcamera
Used only to test inside unity editor. Calculates doppler from this transform. Doesn't do anything in-game.


VehicleRespawnButton.cs--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Used to respawn vehicles on button press. Only respawns the vehicle if EngineController.Occupied = false.
If your vehicle fails to respawn in-game make sure the 'Synchronize Position' tickbox is checked on the vehicle's main object's udonbehaviour.
Variables:

Engine Control
Needed to tell the plane to set certain settings when respawning.


WindChanger.cs--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Used to create an object that can change the wind of planes in-game.

Controls:
Use while holding object to apply wind.

Wind Strength Slider
Slider object used to set the strength of the wind.

Wind Str_text
Text object that displays current value of Wind Strength Slider.

Wind Gust Strength Slider
Slider object used to set the strength of wind gusts.

Wind Gust Strength_text
Text object that displays current value of Wind Gust Strength Slider.

Wind Gustiness Slider
Slider object used to set the value of Wind Gustiness.

Wind Gustiness_text
Text object that displays current value of Wind Gustiness Slider.

Wind Turbulance Scale Slider
Slider object used to set the value of Turbulance Scale.

Wind Turbulance Scale_text
Text object that displays current value of Turbulance Scale Slider.

Wind Apply Sound
AudioSource object containing the sound played when wind is applied.

Vehicle Engines
EngineController objects the wind will effect.


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

Seat Adjuster
Needed to enable the seat adjuster when you enter


HitDetectorAAGun.cs--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Used to detect hits on the AAGun, and also has functions that are called by the animator.
AA Gun Control
AAGunController is needed to reduce health on hit

Bullet Hit
Audio source for being hit by a bullet


HUDControllerAAGun.cs--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Calculates information about the AAGun and sends output to the HUD.

AA Gun Control
Required to know information about the AAGun. AAGunController goes here.

Elevation Indicator
Hud object that shows pitch angle

Heading Indicator
Hud object that shows yaw angle


LeaveAAGun.cs--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Used on the button to leave the AAGun

Controls Desktop:
Return: Exit AAGun

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

Gun Object
Sets the person who enters as the owner of the gun. Gun goes in here.

Gun Controller
Script makes you the owner of the guncontroller and controls the value 'manning'.


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