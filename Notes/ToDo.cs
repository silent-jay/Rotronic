/*
Centralized TODO list.

Keep implementation notes (what/why) here and keep code comments focused on intent.

-------------------------------------------------------------------------------
Calibration / workflow
-------------------------------------------------------------------------------
- Add timeout / "close enough" option when chamber cannot reach setpoints
  (0°C and 5%RH are difficult).
- If calibration procedure is interrupted, restore probe to snapshot taken.
- Validation "Update values" doesn't do anything to the chamber.
- Restore probe to factory settings before taking any adjustment points.
- A no warning mode with some warnings prior to making adjustments/changes, etc.

-------------------------------------------------------------------------------
UI / reporting
-------------------------------------------------------------------------------
- UI user management
- Calibration Certificates
  (recovery if program fails/crashes/interrupted).

-------------------------------------------------------------------------------
Safety / validation
-------------------------------------------------------------------------------
- Temperature/Humidity min/max for procedure and chamber inputs.

-------------------------------------------------------------------------------
Mirror / temperature terminology
-------------------------------------------------------------------------------
- MirrorTemp and ExternalTemp are used interchangeably/incorrectly.
  ExternalTemp is the PRT for the mirror; MirrorTemp is the mirror internal temp
  which determines the reported dew/frost point.
  For now continue to use MirrorTemp as the "reference" temperature.
- Ensure mirror temp matches expected probe format (decimal precision, etc.).
- Make sure mirror control turns off when not in use.
- Mirror data collector probably needs some adjustment. Right now only collects
  minimal amount of data - external temp and humidity to get UI functioning.

-------------------------------------------------------------------------------
Misc
-------------------------------------------------------------------------------
- Global Celsius setting troubleshooting.
- Auto-detect new devices during runtime.
- Help file, manuals, etc.
- Alert that calibration is complete. Change button to close form.
- Probe cal and due dates not updating.

            
            
            
*/   
