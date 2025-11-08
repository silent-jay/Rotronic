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
 */