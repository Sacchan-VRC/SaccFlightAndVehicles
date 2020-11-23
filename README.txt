Sacchan's Flight and Vehicles Prefab
FOR VRChat SDK3 WITH UDONSHARP
https://github.com/Merlin-san/UdonSharp/releases
https://discord.gg/Z7bUDc8
https://twitter.com/Sacchan_VRC
Feel free to give feedback or ask questions
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Small Update 1.37
•Gun Lead Indicator smoother
•Missiles reverted to 1.35 because they were much more consistant
•Animator events for firing weapons
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Small Update 1.36
•Many small tweaks and bugfixes
•Added Gun Lead Indicator
•Improved missile tracking
•Cable snap sound
•Missiles unable to lock on through walls
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Hotfix Update 1.35
•Added missing ViewScreen material
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Small Update 1.34
•Added commented out elevon and ruddervator code to EffectsController, uncomment it if you need it.
•Added channel number display to ViewScreen
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Hotfix Update 1.33
•Fixed AoA effects appearing on stationary planes when wind is enabled
•Tweaked AAM code to prevent possibility of firing at next target without locking if you fire on the frame target changes
•Fixed Throttle Slider animation
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Small Update 1.32
•Added plane view screen
•Small changes to EffectsController to support view screen
•Small fixes to guide images
•Bomb angle randomization option
•Fair number of small tweaks
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Hotfix Update 1.31
•Emergency fix to seat exit code to support new VRChat patch
•Some tweaks
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

!!!
in 1.37 EngineController:
High Aoa Min Control Pitch
High Aoa Min Control Yaw
have been renamed to:
High Pitch Aoa Min Control
High Yaw Aoa Min Control
You must remember and re-enter the values if updating
!!!

On first run there may be a compile problem that causes the plane to not function, try running it a second time before checking anything else.

If the VRChat world upload screen becomes unresponsive, it means you've messed up your Inputs page on your project settings.

Some of the animations use the 'Normalized Time' feature, which makes the animation be controlled by a float parameter (which is controlled by effectscontroller).
float value 1(or more) = play the last frame of the animation
float value 0(or less) = play the first frame of the animation
and everything in between accordingly.

The MachVapor for example uses the mach10 variable. This variable = 1 at mach10, and 0 when not moving. The animation is 1000 frames long, and the MachVapor is enabled on frame 97 and disabled on frame 102, which corresponds to mach1.
This can be used to do other things like F-14 wings moving back at a certain speed.

The angle of attack variable is 0 at 0 AoA, and 1 at 180.

For control inputs to work in editor play mode you must add the VRChat inputs to your unity project. Just filling in the name entry is fine, It needs them to compile. See inputs.png and inputs.txt

Remember you can test fly the vehicles inside unity by adding a camera to them. Recommend adding camera as child of HudController. Set camera view distance beyond 15,000m~ to see the HUD.
You can test buttons by clicking Interact in the inspecter with the object selected(PilotSeat etc), some may crash in the editor but not ingame.
You can also test the animator variables, but some are set every frame so you won't be able to change them without altering or disabling the script.
To test the gun damage to planes/objects in editor, you must enable the Gun_pilot object, as it's disabled by default (The Interact button on the pilotseat also enables it)
All planes will react to inputs in the editor play mode.
SaccFlight will always crash when testing in editor, this doesnt matter.

It's best to break my prefab in order to make your own planes.
If you've made you own vehicle and it's having trouble taking off, try adjusting the center of mass and pitch moment positions, and also the takeoff assist options

When using a custom model, remove the Avatar reference from the animator to stop some confusing things from happening with references in animations

To see the HUD in-game you must set up a reference camera with a view distance greater than around 12,000 on the VRCworld.

Tips for modifying basic flight characteristics of aircraft:
The Strength and Friction values for pitch, yaw, and roll are important, and both play off each other. You may need to set them to very high values, especially if you make a large plane.
Rot Multi Max Speed will need to be adjusted for planes with different speeds. It's the speed at which (in meters per second) the plane reaches maximum responsiveness.
Vel Straighten Str Pitch/Yaw are Very important to the handling of the plane. They push the nose toward the velocity direction. (they also interact with pitch and yaw strength)
Lift and Max Lift are also very important, and a bit tricky to tweak. If max lift is too high you can end up with a plane that can fly in circles extremely quickly.
If you make a heavier plane with a great rigidbody mass value, you will have to tweak the values a lot.
Don't change the angular drag or drag of the rigidbody, drag is handled by the script, and angular drag is set by the script as a workaround for a sync issue. (it's 0 when you're owner of the plane, 0.3 when your not)

The Gun_pilot is set to not collide with reserved2 layer. The plane is set to reserved2 you enter, so you can't shoot your own plane, and reverted back to Walkthrough when you leave.

Never leave an entry of an array input empty in EffectsController and SoundController, it'll cause them to crash.

The hud can be made to appear smaller by moving all elements inside the 'bigstuff' object forward. 'bigstuff' is a child of HudController.

Visual animations are done by either EffectsController directly, HudController if they're local/only when you're in the plane, or through the animator (via values sent to it by EffectsController).
Doing animations using the animator is most performant, so I've used it where possible. HUD stuff included.
When making a custom plane, you must also customize a number of the animations, I recommend duplicating the SF-1's animation controller, and replacing the animations that need replacing.
Likely to need replacing animations:
AAMs
AGMs
AoA (for stall angle)
Bombs
Brake
CanopyOpen
DropFlares
Explode
FlapsOn
Mach
GearUp
TailHook
TailHookHooked
ThrottleSlider
Remember for animations that are controlled by floats (normalized time), set the curves to linear.

Hierarchy:
PlaneBody--------
Everything in here is visual, except that it also contains colliders, and wheel colliders. Wheel colliders are a bit dodgy, I don't recommend messing with their settings.
EngineController--------
Children of this are transforms used by the enginecontroller, and projectile objects for cloning and launching.
EffectsController--------
The children of this are mostly particle systems, visual effects used by EffectsController and the animator.
SoundController--------
The children are all the sounds the plane uses. The AttachedSounds empty contains all sounds that are disabled when the plane explodes.
HudController--------
The children of this are all things that are disabled when you're not inside the plane. Many different objects are in here including the HUD, visual joystick, leave buttons, MFDs(Multi-Function-Display), the AtG Camera and screen.
To test the plane in editor mode, add a camera to this object with no change to position or rotation, and increase the view distance of that camera beyond 15,000m~ to see the HUD.
HUDController is only enabled when inside the vehicle.
many objects that are only enabled while inside the plane are children of the HudController.
HUDController's bigstuff child object is scaled to 8000 to make the hud appear to be on the sky like a real HUD. You may want to scale it down to 1 temporarily if you plan on editing HUD elements.
PilotSeat--------
A custom station with the seat adjuster for entering the plane
PassengerSeat--------
A custom station with the seat adjuster for entering the passenger seat of the plane
AAMs--------
AGMs--------
Bombs--------
These 3 objects just contain meshes to visually represent how many missiles etc you have left. Controlled by the animator.

Custom Layers:
Plane colliders must be set to Walkthrough layer in order to be targeted by AAMs. (Raycast looks for this layer)
Various functions require custom layers to be set up in order to work(Air-to-air-missiles, Air-to-ground custom targets, resupply zones, Arresting cables, and catapults)
As layers can't be imported in a unitypackage you must set them up yourself.
You must set the trigger objects to their respective layers. Create new layers, and set them in EngineController. By default the layers are as follows
I recommend you set up yours the same to save you having to set them on everything:
23:Hook Cable
24:Catapult
25:AAMTargets
26:AGMTargets
27:Resupply
The trigger objects that you made need to change the layer of are in the prefabs listed below. The plane's AAMTarget is a child of EngineController.
Note that the AAMTarget object on a plane MUST be the first child of the EngineController object, or the code to play the radar lock warning tone will fail.

Main Prefab:
SaccFlightAndVehicles
The main prefab containing the SF-1 plane, the AAGun the WindChanger, SaccFlight, and an instructions board.

This package also contains a few prefabs that can be used with the plane. Explanations follow.

AAMDummyTarget
Target object that can be placed on anything that you want air-to-air missiles to be able to lock on to. MUST be enabled by default or planes will not detect it during initial target enumeration in Start().

AGMLockTarget
object that if detected when attempting to target close to it with the AGM targeting system, will cause the targeted spot to snap to this location.

Arresting Cable
A prefab containing a cable mesh and a trigger designed to be placed on short runways or aircraft carriers to enable short landings.
The trigger must be on the correct layer, which you have to set up manually, and select in the EngineController.

Catapult
A prefab containing a catapult mesh and a trigger to enable the plane to launch quickly with no runway
The trigger must be on the correct layer, which you have to set up manually, and select in the EngineController.

ResupplyZone
A prefab containing a square outline mesh with a trigger beneath it, used to reload repair and refuel planes.
The trigger must be on the correct layer, which you have to set up manually, and select in the EngineController.

Target
A basic example target object with configurable health, that respawns in 10 seconds, replace with your own mesh to create destroyable buildings, etc.
Target's health is LOCAL. If destroyed locally everyone will see it explode via an event, but you cannot work together to take down it's health to destroy it. That will require a change in code, and cause a laggy experience for non-owners.


There are 24 udon scripts in this package, I will now explain what each one does, and what each of the variables of each one does. Ctrl-F as needed.

SF-1
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
AAMController(new in 1.3)--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Controls movement of Air-to-air missiles.
Variables:

Engine Control
Missile's plane's EngineController, needed to know target.

Max Lifetime
Time until the missile will explode without hitting anything.

Explosion Sounds
Array of sounds one of which will play for the explosion.

Collider Active Distance
Missile's collider is inactive when it is spawned. After it's this far away from the plane it launched from, it becomes active. This is to prevent it from hitting the plane it launched from. The faster the plane is moving, the higher this number needs to be due to ????Physics.

Rot Speed
angle per second that the missile can turn while chasing it's target.

Missile Drift Compensation
Aims the missile further infront of the plane the lower the number is. Used to account for the fact that the missile is a rigidbody and doesn't fly perfectly straight.
Hard to tweak, recommend not changing unless your missiles are missing easy targets.


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

Angle Randomization
Add a random angle to the bombs spawn pitch and yaw to randomize their fall pattern, useful for planes that drop many bombs.

Collider Active Distance
Missile's collider is inactive when it is spawned. After it's this far away from the plane it launched from, it becomes active. This is to prevent it from hitting the plane it launched from. The faster the plane is moving, the higher this number needs to be due to ????Physics.

Straighten Factor
strength of the force making the bomb face towards the direction it's moving.

Air Physics Strength
Strength of 'air' acting on the bomb's movement


EffectsController.cs (new in 1.1)--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Controls vehicle's control surface movement and vapor effects, also contains synced variables as there's too many for one script.
If you want an easy way to set up your empties at the the right angles for your plane, try copying and dragging the empties from the SF-1 to yours and adjust them from there.
You can also set up empties in blender if you don't want to break your model's prefab.
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
The pilot has many capabilities and functions and they're all processed here.
AAM Targeting: If a target is within 15 degrees the plane will target it and begin to lock on. After lock on delay duration, you can fire a missile. two targets are within the 15 degrees, it will target the one with the lower angle.
The Radarlock event is sent once every second while the plane has a target in it's sights, the radarlock animation lasts 1.1 seconds, this allows for an alarm to play constantly while targeted.
AGM Targeting: move the camera around with the right hand in VR or head in desktop, to lock a target pull the trigger or click the mouse. the camera will lock onto the position you clicked, or if it detects an AGMTarget nearby it'll lock that.
Once locked, you can fire an AGM by double tapping the trigger or left click. To unlock from target tap the trigger or click once, and wait 0.4 seconds.
Smoke: To change smoke color, pull and hold the trigger when you activate it, and move your hand around, XYZ axis movement changes RGB color correspondingly.
Catapult: The plane will lock onto a catapult if the plane's orientation is within 15 degrees of the catapult's trigger object.
Cruise: Tries to keep the plane moving at a constant speed. To change the target speed, pull and hold the trigger with Cruise selected and move your right hand back and forth.

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

Plane Mesh
Put the parent object of the plane's mesh objects here. Sets it and all it's children to the 'OnboardPlaneLayer' layer when you enter, and back to what it was before when you leave. This is so that the Gun_Pilot doesn't hit your own plane.
Do not put any colliders as child of any other object, or the gun might hit them.

Onboard Plane Layer
Layer to set the Plane Mesh and all it's children to when you enter the plane.

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
When moving backwards through the air, your plane's rotational input strengths can but multiplied. Doesn't effect any axis with thrust vectoring value above 0.

Pitch Down Str Multi
Allows you to set a seperate rotation speed for pulling down.

Pitch Down Lift Multi
Allows you to generate less lift from pulling down. (air hitting the top of your plane)

Rot Multi Max Speed (new in 1.2)
Rotational inputs are multiplied by current speed to make flying at low speeds feel heavier. Above the speed input here, all inputs will be at 100%. Linear. (Meters/second)

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

High Pitch Aoa Min Control (new in 1.2)
High Yaw Aoa Min Control
The angle of attack curve is augmented by being MAX'd(taking the higher value) with a linear curve that is multiplied by this number.
Use this value to decide how much control the plane has when beyond it's 'max' angle of attack. See AoALiftCurve.png
Pitch AoA and Yaw AoA are calculated seperately, and whichever value is lower is used.

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

Distance_from_head
Distance that some moving hud elements are projected forward to match the position of other objects on the hud. Change this if you move the hud forward (to make it appear smaller)


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

Cable Snap
Sound made when you try to land with the hook too fast, catch the cable, but go too far and the cable snaps.

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
Used to create an object that can change the wind of planes in-game. Not required for plane functionality. Contains commented code that can be used to make the changes in wind global (doesn't work for late joiners).

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

Variables:

Seat
The object that will move (usually the Seat object of a VRCChair)

Target Height
Empty with transform position at desired head height

Head Test
You can use this to test the script inside unity. Just put an object as the child of the seat object, position it somewhere bad for piloting, and then when in play mode, enable the SaccSeatAdjuster object, and it should move into position.

ViewScreenButton.cs--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Turns on the view screen or changes channel if it's already on

Variables:

View Screen Control
ViewScreenController object to effect.

ViewScreenController.cs--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
Find all AAMTargets that are planes, makes a list of them and positions a camera behind them. Runs EffectsController's Effects() function for the current plane if it's too far away and has therefore been disabled in EffectsController.

AAM Targets Layer
Layer to look for planes's AAMTarget on.

Disable Distance
Distance at which the Plane Camera and View Screen become disabled.

Plane Camera
Camera object that follows planes.

View Screen
Screen object.

AAM Target
Current view target. Used for testing in the editor, the ViewScreenButton increments this. (Be careful not to set it out of range when testing)






Have fun!