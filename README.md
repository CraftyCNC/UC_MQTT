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

## MQTT topics

`<machine>` is the **Machine name** set in the plugin (default `UCCNC`).

### State topics (published)
State topics are published every update interval (set in the plugin, in seconds) at the QoS chosen in the plugin, not retained.

| Topic | Payload values | Enabled by |
|---|---|---|
| `<machine>/estop_status/state` | `Estop`, `Cleared` | *Estop/Reset* checkbox |
| `<machine>/limit_status/state` | `Hard Limit`, `Soft Limit`, `Cleared` | *Limit Switch Hit* checkbox |
| `<machine>/run_status/state` | `Running`, `Idle` | *Finished (Cycle done)* checkbox |
| `<machine>/spindle_status/state` | `Running`, `Idle` | *Finished (Cycle done)* checkbox |
| `<machine>/current_line/state` | current G-code line number | always |
| `<machine>/percent_complete/state` | percent of job complete (2 decimals), or `NA` when not running | always |
| `<machine>/run_time/state` | job run time as shown on the UCCNC job status page | always |

### Home Assistant discovery
Each of the entities above is announced as a retained `sensor` (QoS 1) on
`homeassistant/sensor/<machine>/<entity>/config`, with `state_topic` set to the matching state topic and a device named `<machine>`.

### Other topics
- `DustCollector` publishes `OFF` when a G-code program finishes.
- The plugin's test tab can subscribe to, publish to and clear any topic you enter.

## License
MIT, see [LICENSE](LICENSE).
