/* Create class to send and read responses to Rotronic probes
 * Create class to send and read responses from chilled mirror
 * create class to send and read responses from Rotronic Hygrogen Chamber
 * Need to explore best way to gracefully handle multiple device connections (USB, Serial, Ethernet)
 * Need to be able to send commands to multiple devices and read responses asynchronously
 * Need to be able to handle serial connections gracefully (open/close ports as needed, without causing conflict)
 * Use Responses from various devices to populate variables/objects that can be used for UI display and calibration
 * Create UI to create and load calibration scripts
 * Calibration UI must be able to run calibration sequences, log and save results to excel, save calibration constants to device
 * UI should be able to detect when new devices are connected.
 * Multiple calibration sequences should be able to run simultaneously on different devices.
 * calculated offset is difference between probe's actual measurement and 0°C, not mirror-probe.
To calculate the new offset we need to use the probe's temperature conversion value (RotProbe.TempConversion) and the calculated R0 value (probe resistance @ 0 °C).



mirror control response examples:
DP? dew point
22.4971
IDN? 473
ID? DPM 473r2
SN? serial number
13-0418
Tm? 15.1914
Stable?
1 (1 = stable, 0=not stable)
Control = 1 (turns mirror on)

baud settings:
Baud Rate: 9600
Data Bits: 8
Stop Bits: 1
Handshaking: None

 */