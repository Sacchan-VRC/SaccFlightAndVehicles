
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SAV_EventTestScript : UdonSharpBehaviour
    {
        public Text DebugText;
        private int NumEvents = 77;
        private string[] Event;
        private int[] Ints;

        public void SFEXT_L_EntityStart()
        {
            Event = new string[NumEvents];
            Ints = new int[NumEvents];
            int x = 0;

            Event[x++] = "SFEXT_L_EntityStart";
            Event[x++] = "SFEXT_G_Dead";
            Event[x++] = "SFEXT_G_NotDead";
            Event[x++] = "SFEXT_L_BulletHit";
            Event[x++] = "SFEXT_O_TakeOwnership";
            Event[x++] = "SFEXT_O_LoseOwnership";
            Event[x++] = "SFEXT_L_OwnershipTransfer";
            Event[x++] = "SFEXT_O_PilotEnter";
            Event[x++] = "SFEXT_G_PilotEnter";
            Event[x++] = "SFEXT_G_PilotExit";
            Event[x++] = "SFEXT_O_PilotExit";
            Event[x++] = "SFEXT_P_PassengerEnter";
            Event[x++] = "SFEXT_P_PassengerExit";
            Event[x++] = "SFEXT_G_PassengerEnter";
            Event[x++] = "SFEXT_G_PassengerExit";
            Event[x++] = "SFEXT_O_OnPlayerJoined";
            Event[x++] = "SFEXT_O_OnPickup";
            Event[x++] = "SFEXT_O_OnDrop";
            Event[x++] = "SFEXT_O_OnPickupUseDown";
            Event[x++] = "SFEXT_L_AAMTargeted";
            Event[x++] = "SFEXT_O_AAMLaunch";
            Event[x++] = "SFEXT_O_AGMLaunch";
            Event[x++] = "SFEXT_O_AltHoldOn";
            Event[x++] = "SFEXT_O_AltHoldOff";
            Event[x++] = "SFEXT_O_BombLaunch";
            Event[x++] = "SFEXT_O_CanopyClosed";
            Event[x++] = "SFEXT_O_CanopyOpen";
            Event[x++] = "SFEXT_O_CanopyBreak";
            Event[x++] = "SFEXT_O_CanopyRepair";
            Event[x++] = "SFEXT_O_LaunchFromCatapult";
            Event[x++] = "SFEXT_O_CruiseEnabled";
            Event[x++] = "SFEXT_O_CruiseDisabled";
            Event[x++] = "SFEXT_O_FlapsOff";
            Event[x++] = "SFEXT_O_FlapsOn";
            Event[x++] = "SFEXT_G_LaunchFlare";
            Event[x++] = "SFEXT_O_GearUp";
            Event[x++] = "SFEXT_O_GearDown";
            Event[x++] = "SFEXT_O_GunStartFiring";
            Event[x++] = "SFEXT_O_GunStopFiring";
            Event[x++] = "SFEXT_O_HookDown";
            Event[x++] = "SFEXT_O_HookUp";
            Event[x++] = "SFEXT_O_LimitsOn";
            Event[x++] = "SFEXT_O_LimitsOff";
            Event[x++] = "SFEXT_G_SmokeOn";
            Event[x++] = "SFEXT_G_SmokeOff";
            Event[x++] = "SFEXT_O_ReSupply";
            Event[x++] = "SFEXT_O_JoystickGrabbed";
            Event[x++] = "SFEXT_O_JoystickDropped";
            Event[x++] = "SFEXT_O_ThrottleGrabbed";
            Event[x++] = "SFEXT_O_ThrottleDropped";
            Event[x++] = "SFEXT_O_LowFuel";
            Event[x++] = "SFEXT_O_NoFuel";
            Event[x++] = "SFEXT_O_EnterVTOL";
            Event[x++] = "SFEXT_O_ExitVTOL";
            Event[x++] = "SFEXT_O_Explode";
            Event[x++] = "SFEXT_G_ReAppear";
            Event[x++] = "SFEXT_O_MoveToSpawn";
            Event[x++] = "SFEXT_G_TouchDown";
            Event[x++] = "SFEXT_G_TouchDownWater";
            Event[x++] = "SFEXT_G_TakeOff";
            Event[x++] = "SFEXT_G_AfterburnerOn";
            Event[x++] = "SFEXT_G_AfterburnerOff";
            Event[x++] = "SFEXT_G_ReSupply";
            Event[x++] = "SFEXT_O_NotLowFuel";
            Event[x++] = "SFEXT_O_NotNoFuel";
            Event[x++] = "SFEXT_G_RespawnButton";
            Event[x++] = "SFEXT_G_BulletHit";
            Event[x++] = "SFEXT_G_MissileHit25";
            Event[x++] = "SFEXT_G_MissileHit50";
            Event[x++] = "SFEXT_G_MissileHit75";
            Event[x++] = "SFEXT_G_MissileHit100";
            Event[x++] = "SFEXT_O_GotKilled";
            Event[x++] = "SFEXT_O_GotAKill";
            Event[x++] = "SFEXT_O_DoorsClosed";
            Event[x++] = "SFEXT_O_DoorsOpened";
            Event[x++] = "SFEXT_G_EnterWater";
            Event[x++] = "SFEXT_G_ExitWater";

            x = 0;
            while (x < NumEvents)
            {
                Ints[x] = 0;
                x++;
            }
            Ints[0]++;//entitystart is an event
            CompileString();
        }
        private void CompileString()
        {
            DebugText.text = string.Empty;
            int x = 0;
            foreach (string txt in Event)
            {
                DebugText.text = string.Concat(DebugText.text, Ints[x], ": ", Event[x], "\n");
                x++;
            }
        }
        public void SFEXT_G_Dead()
        {
            Ints[01]++;
            CompileString();
        }
        public void SFEXT_G_NotDead()
        {
            Ints[02]++;
            CompileString();
        }
        public void SFEXT_L_BulletHit()
        {
            Ints[03]++;
            CompileString();
        }
        public void SFEXT_O_TakeOwnership()
        {
            Ints[04]++;
            CompileString();
        }
        public void SFEXT_O_LoseOwnership()
        {
            Ints[05]++;
            CompileString();
        }
        public void SFEXT_L_OwnershipTransfer()
        {
            Ints[06]++;
            CompileString();
        }
        public void SFEXT_O_PilotEnter()
        {
            Ints[07]++;
            CompileString();
        }
        public void SFEXT_G_PilotEnter()
        {
            Ints[08]++;
            CompileString();
        }
        public void SFEXT_G_PilotExit()
        {
            Ints[09]++;
            CompileString();
        }
        public void SFEXT_O_PilotExit()
        {
            Ints[10]++;
            CompileString();
        }
        public void SFEXT_P_PassengerEnter()
        {
            Ints[11]++;
            CompileString();
        }
        public void SFEXT_P_PassengerExit()
        {
            Ints[12]++;
            CompileString();
        }
        public void SFEXT_G_PassengerEnter()
        {
            Ints[13]++;
            CompileString();
        }
        public void SFEXT_G_PassengerExit()
        {
            Ints[14]++;
            CompileString();
        }
        public void SFEXT_O_OnPlayerJoined()
        {
            Ints[15]++;
            CompileString();
        }
        public void SFEXT_O_OnPickup()
        {
            Ints[16]++;
            CompileString();
        }
        public void SFEXT_O_OnDrop()
        {
            Ints[17]++;
            CompileString();
        }
        public void SFEXT_O_OnPickupUseDown()
        {
            Ints[18]++;
            CompileString();
        }
        public void SFEXT_L_AAMTargeted()
        {
            Ints[19]++;
            CompileString();
        }
        public void SFEXT_O_AAMLaunch()
        {
            Ints[20]++;
            CompileString();
        }
        public void SFEXT_O_AGMLaunch()
        {
            Ints[21]++;
            CompileString();
        }
        public void SFEXT_O_AltHoldOn()
        {
            Ints[22]++;
            CompileString();
        }
        public void SFEXT_O_AltHoldOff()
        {
            Ints[23]++;
            CompileString();
        }
        public void SFEXT_O_BombLaunch()
        {
            Ints[24]++;
            CompileString();
        }
        public void SFEXT_O_CanopyClosed()
        {
            Ints[25]++;
            CompileString();
        }
        public void SFEXT_O_CanopyOpen()
        {
            Ints[26]++;
            CompileString();
        }
        public void SFEXT_O_CanopyBreak()
        {
            Ints[27]++;
            CompileString();
        }
        public void SFEXT_O_CanopyRepair()
        {
            Ints[28]++;
            CompileString();
        }
        public void SFEXT_O_LaunchFromCatapult()
        {
            Ints[29]++;
            CompileString();
        }
        public void SFEXT_O_CruiseEnabled()
        {
            Ints[30]++;
            CompileString();
        }
        public void SFEXT_O_CruiseDisabled()
        {
            Ints[31]++;
            CompileString();
        }
        public void SFEXT_O_FlapsOff()
        {
            Ints[32]++;
            CompileString();
        }
        public void SFEXT_O_FlapsOn()
        {
            Ints[33]++;
            CompileString();
        }
        public void SFEXT_G_LaunchFlare()
        {
            Ints[34]++;
            CompileString();
        }
        public void SFEXT_O_GearUp()
        {
            Ints[35]++;
            CompileString();
        }
        public void SFEXT_G_GearDown()
        {
            Ints[36]++;
            CompileString();
        }
        public void SFEXT_O_GunStartFiring()
        {
            Ints[37]++;
            CompileString();
        }
        public void SFEXT_O_GunStopFiring()
        {
            Ints[38]++;
            CompileString();
        }
        public void SFEXT_O_HookDown()
        {
            Ints[39]++;
            CompileString();
        }
        public void SFEXT_O_HookUp()
        {
            Ints[40]++;
            CompileString();
        }
        public void SFEXT_O_LimitsOn()
        {
            Ints[41]++;
            CompileString();
        }
        public void SFEXT_O_LimitsOff()
        {
            Ints[42]++;
            CompileString();
        }
        public void SFEXT_G_SmokeOn()
        {
            Ints[43]++;
            CompileString();
        }
        public void SFEXT_G_SmokeOff()
        {
            Ints[44]++;
            CompileString();
        }
        public void SFEXT_O_ReSupply()
        {
            Ints[45]++;
            CompileString();
        }
        public void SFEXT_O_JoystickGrabbed()
        {
            Ints[46]++;
            CompileString();
        }
        public void SFEXT_O_JoystickDropped()
        {
            Ints[47]++;
            CompileString();
        }
        public void SFEXT_O_ThrottleGrabbed()
        {
            Ints[48]++;
            CompileString();
        }
        public void SFEXT_O_ThrottleDropped()
        {
            Ints[49]++;
            CompileString();
        }
        public void SFEXT_O_LowFuel()
        {
            Ints[50]++;
            CompileString();
        }
        public void SFEXT_O_NoFuel()
        {
            Ints[51]++;
            CompileString();
        }
        public void SFEXT_O_EnterVTOL()
        {
            Ints[52]++;
            CompileString();
        }
        public void SFEXT_O_ExitVTOL()
        {
            Ints[53]++;
            CompileString();
        }
        public void SFEXT_O_Explode()
        {
            Ints[54]++;
            CompileString();
        }
        public void SFEXT_G_ReAppear()
        {
            Ints[55]++;
            CompileString();
        }
        public void SFEXT_O_MoveToSpawn()
        {
            Ints[56]++;
            CompileString();
        }
        public void SFEXT_G_TouchDown()
        {
            Ints[57]++;
            CompileString();
        }
        public void SFEXT_G_TouchDownWater()
        {
            Ints[58]++;
            CompileString();
        }
        public void SFEXT_G_TakeOff()
        {
            Ints[59]++;
            CompileString();
        }
        public void SFEXT_G_AfterburnerOn()
        {
            Ints[60]++;
            CompileString();
        }
        public void SFEXT_G_AfterburnerOff()
        {
            Ints[61]++;
            CompileString();
        }
        public void SFEXT_G_ReSupply()
        {
            Ints[62]++;
            CompileString();
        }
        public void SFEXT_O_NotLowFuel()
        {
            Ints[63]++;
            CompileString();
        }
        public void SFEXT_O_NotNoFuel()
        {
            Ints[64]++;
            CompileString();
        }
        public void SFEXT_G_RespawnButton()
        {
            Ints[65]++;
            CompileString();
        }
        public void SFEXT_G_BulletHit()
        {
            Ints[66]++;
            CompileString();
        }
        public void SFEXT_G_MissileHit25()
        {
            Ints[67]++;
            CompileString();
        }
        public void SFEXT_G_MissileHit50()
        {
            Ints[68]++;
            CompileString();
        }
        public void SFEXT_G_MissileHit75()
        {
            Ints[69]++;
            CompileString();
        }
        public void SFEXT_G_MissileHit100()
        {
            Ints[70]++;
            CompileString();
        }
        public void SFEXT_O_GotKilled()
        {
            Ints[71]++;
            CompileString();
        }
        public void SFEXT_O_GotAKill()
        {
            Ints[72]++;
            CompileString();
        }
        public void SFEXT_O_DoorsClosed()
        {
            Ints[73]++;
            CompileString();
        }
        public void SFEXT_O_DoorsOpened()
        {
            Ints[74]++;
            CompileString();
        }
        public void SFEXT_G_EnterWater()
        {
            Ints[75]++;
            CompileString();
        }
        public void SFEXT_G_ExitWater()
        {
            Ints[76]++;
            CompileString();
        }
    }
}