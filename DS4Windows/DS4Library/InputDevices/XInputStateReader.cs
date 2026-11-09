/*
DS4Windows
Copyright (C) 2026 DS4Windows contributors

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/

using System;
using System.Runtime.InteropServices;

namespace DS4Windows.InputDevices
{
    /// <summary>
    /// Reads a standard XInput controller and translates its controls into the
    /// common DS4State representation used by DS4Windows' mapping pipeline.
    ///
    /// This is deliberately independent from DS4Device: XInput controllers do
    /// not expose a DS4-compatible HID report, serial number, touchpad or gyro.
    /// </summary>
    public static class XInputStateReader
    {
        private const uint ErrorSuccess = 0;
        private const ushort DpadUp = 0x0001;
        private const ushort DpadDown = 0x0002;
        private const ushort DpadLeft = 0x0004;
        private const ushort DpadRight = 0x0008;
        private const ushort Start = 0x0010;
        private const ushort Back = 0x0020;
        private const ushort LeftThumb = 0x0040;
        private const ushort RightThumb = 0x0080;
        private const ushort LeftShoulder = 0x0100;
        private const ushort RightShoulder = 0x0200;
        private const ushort Guide = 0x0400;
        private const ushort A = 0x1000;
        private const ushort B = 0x2000;
        private const ushort X = 0x4000;
        private const ushort Y = 0x8000;

        /// <summary>
        /// Reads one of the four XInput user slots and fills a DS4State.
        /// Returns false when the slot is disconnected or the native call fails.
        /// </summary>
        public static bool TryRead(int userIndex, DS4State destination)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            if (userIndex < 0 || userIndex > 3)
                throw new ArgumentOutOfRangeException(nameof(userIndex), "XInput user index must be between 0 and 3.");

            XInputState nativeState;
            uint result;
            try
            {
                result = XInputGetState((uint)userIndex, out nativeState);
            }
            catch (DllNotFoundException)
            {
                // xinput1_4.dll is part of supported Windows versions. A missing
                // DLL means this machine cannot use this input backend.
                return false;
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }

            if (result != ErrorSuccess)
                return false;

            MapState(nativeState, destination);
            return true;
        }

        /// <summary>
        /// Pure mapping step, kept separate so it can be unit-tested without
        /// requiring a physical XInput controller.
        /// </summary>
        internal static void MapState(XInputState source, DS4State destination)
        {
            XInputGamepad pad = source.Gamepad;
            ushort buttons = pad.Buttons;

            destination.PacketCounter = source.PacketNumber;
            destination.ReportTimeStamp = DateTime.UtcNow;

            // Xbox face buttons follow the familiar Nintendo-style layout:
            // A/B/X/Y correspond to Cross/Circle/Square/Triangle.
            destination.Cross = Has(buttons, A);
            destination.Circle = Has(buttons, B);
            destination.Square = Has(buttons, X);
            destination.Triangle = Has(buttons, Y);

            destination.DpadUp = Has(buttons, DpadUp);
            destination.DpadDown = Has(buttons, DpadDown);
            destination.DpadLeft = Has(buttons, DpadLeft);
            destination.DpadRight = Has(buttons, DpadRight);

            destination.L1 = Has(buttons, LeftShoulder);
            destination.R1 = Has(buttons, RightShoulder);
            destination.L3 = Has(buttons, LeftThumb);
            destination.R3 = Has(buttons, RightThumb);
            destination.Share = Has(buttons, Back);
            destination.Options = Has(buttons, Start);
            destination.PS = Has(buttons, Guide);

            destination.L2 = pad.LeftTrigger;
            destination.R2 = pad.RightTrigger;
            destination.L2Raw = pad.LeftTrigger;
            destination.R2Raw = pad.RightTrigger;
            destination.L2Btn = pad.LeftTrigger > 0;
            destination.R2Btn = pad.RightTrigger > 0;

            destination.LX = ToAxis(pad.LeftThumbX, invert: false);
            destination.LY = ToAxis(pad.LeftThumbY, invert: true);
            destination.RX = ToAxis(pad.RightThumbX, invert: false);
            destination.RY = ToAxis(pad.RightThumbY, invert: true);

            // XInput does not provide DS4 touchpad, motion, lightbar, battery,
            // mute, or additional paddle data. Leave those fields neutral.
            destination.Touch1 = false;
            destination.Touch2 = false;
            destination.TouchButton = false;
            destination.TrackPadTouch0 = default;
            destination.TrackPadTouch1 = default;
            destination.Motion = new SixAxis(0, 0, 0, 0, 0, 0, 0.0);
        }

        private static bool Has(ushort value, ushort flag) => (value & flag) != 0;

        private static byte ToAxis(short value, bool invert)
        {
            int normalized = value + 32768;
            if (invert)
                normalized = 65535 - normalized;

            return (byte)((normalized * 255L + 32767L) / 65535L);
        }

        [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState", CallingConvention = CallingConvention.StdCall)]
        private static extern uint XInputGetState(uint userIndex, out XInputState state);

        [StructLayout(LayoutKind.Sequential)]
        internal struct XInputState
        {
            public uint PacketNumber;
            public XInputGamepad Gamepad;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct XInputGamepad
        {
            public ushort Buttons;
            public byte LeftTrigger;
            public byte RightTrigger;
            public short LeftThumbX;
            public short LeftThumbY;
            public short RightThumbX;
            public short RightThumbY;
        }
    }
}
