# UC_MQTT

A [UCCNC](https://cncdrive.com) plugin (VB.NET) that publishes machine state to an MQTT broker, with Home Assistant discovery support.

## Build
1. Install UCCNC, or locate its `Plugininterface.dll`.
2. Open `UC_MQTT.sln` in Visual Studio (NuGet restores M2Mqtt and Newtonsoft.Json).
3. Build, pointing at your UCCNC folder if it isn't `C:\UCCNC`:
   `msbuild UC_MQTT.sln /p:UCCNCDir="C:\path\to\UCCNC"`
4. Copy `UC_MQTT.dll` and its dependencies into UCCNC's `Plugins` folder.

## Configuration
Broker address, port, machine name and auto-connect options are set in the plugin's UI (defaults: `localhost:1883`).

`Plugininterface.dll` is proprietary to UCCNC and is not included in this repo.

## License
MIT, see [LICENSE](LICENSE).
