# XInput port status

The first implementation step is an isolated XInput reader in
`DS4Windows/DS4Library/InputDevices/XInputStateReader.cs`.

## Implemented in this branch

- Reads XInput user slots 0–3 through the Windows `XInputGetState` API.
- Converts face buttons, D-pad, bumpers, stick clicks, Back/Start/Guide,
  analog triggers and both sticks into the existing `DS4State` shape.
- Keeps the native XInput structures and mapping isolated from the HID-based
  `DS4Device` implementation.
- Exposes the mapping routine internally so it can be unit-tested separately
  from a connected controller.

## Not yet integrated / limitations

This is an input backend foundation, **not yet a complete usable XInput port**.
The current controller lifecycle in `ControlService` and `DS4Devices` is built
around HID-backed `DS4Device` instances. The next step is to introduce a
shared input-device abstraction (or a carefully scoped XInput adapter) and
register XInput slots in the existing hotplug, report, removal and profile
loading pipeline.

XInput does not expose a physical DS4 touchpad, gyro/accelerometer, lightbar,
battery telemetry, or DS4-specific extra controls. Rumble output needs a
separate `XInputSetState` implementation and must be wired to the output
feedback path; it is not included in this first step.

No build or hardware test has been run from this GitHub editing session. The
reader should be compiled and tested on Windows before the lifecycle work is
merged.
