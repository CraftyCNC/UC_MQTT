# UC_MQTT

A [UCCNC](https://cncdrive.com) plugin (VB.NET) that publishes machine state to an MQTT broker, with Home Assistant discovery support.

## Prebuilt binaries
`bin/` contains a Release build: `UC_MQTT.dll` plus its `M2Mqtt.Net.dll` and `Newtonsoft.Json.dll` dependencies. Copy `UC_MQTT.dll` into UCCNC's `Plugins` folder, and copy `M2Mqtt.Net.dll` and `Newtonsoft.Json.dll` into the UCCNC install folder itself (one level up from `Plugins`, next to `UCCNC.exe`). The proprietary UCCNC files are not included.

## Build
1. Install UCCNC, or locate its `Plugininterface.dll`.
2. Open `UC_MQTT.sln` in Visual Studio (NuGet restores M2Mqtt and Newtonsoft.Json).
3. Build, pointing at your UCCNC folder if it isn't `C:\UCCNC`:
   `msbuild UC_MQTT.sln /p:UCCNCDir="C:\path\to\UCCNC"`
4. Copy `UC_MQTT.dll` into UCCNC's `Plugins` folder, and `M2Mqtt.Net.dll` and `Newtonsoft.Json.dll` into the UCCNC install folder (one level up, next to `UCCNC.exe`).

## Configuration
Broker address, port, machine name and auto-connect options are set in the plugin's UI (defaults: `localhost:1883`).

`Plugininterface.dll` is proprietary to UCCNC and is not included in this repo.

## MQTT topics

`<machine>` is the **Machine name** set in the plugin (default `UCCNC`).

### State topics (published)
State topics are published every update interval (set in the plugin, in seconds) at the QoS chosen in the plugin, not retained.

| Topic | Payload values | Enabled by |
|---|---|---|
| `<machine>/estop_status/state` | `Estop`, `Cleared` | *Estop/Reset* checkbox |
| `<machine>/limit_status/state` | `Hard Limit`, `Soft Limit`, `Cleared` | *Limit Switch Hit* checkbox |
| `<machine>/run_status/state` | `Running`, `Idle` | *Finished (Cycle done)* checkbox |
| `<machine>/spindle_status/state` | `Running`, `Idle` | *Spindle on/off* checkbox |
| `<machine>/current_line/state` | current G-code line number | always |
| `<machine>/percent_complete/state` | percent of job complete (2 decimals), or `NA` when not running | always |
| `<machine>/run_time/state` | job run time as shown on the UCCNC job status page | always |

### Home Assistant discovery
Each of the entities above is announced as a retained `sensor` (QoS 1) on
`homeassistant/sensor/<machine>/<entity>/config`, with `state_topic` set to the matching state topic and a device named `<machine>`.

### Other topics
- `DustCollector` publishes `OFF` when a G-code program finishes.

  The author runs a dust collector that also listens on MQTT on the `DustCollector` topic. It is switched on manually; the plugin sends `OFF` automatically when a program finishes, so the dust collector shuts down after a long unsupervised run. The dust collector itself is not part of this repo; if you don't have one, the message is harmless and can be ignored.
- The plugin's test tab can subscribe to, publish to and clear any topic you enter.

## License
MIT, see [LICENSE](LICENSE).
