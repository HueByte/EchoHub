# Notification Sounds

EchoHub can play a notification sound when someone @mentions you. This is **disabled by default** and must be enabled in your profile settings.

## Enabling Notifications

Open your profile (`/profile`) and check the **"Notification sound on @mention"** checkbox, then save. You can also adjust the **Volume** (0-100, default 30). All settings are persisted in `~/.echohub/config.json`.

## Customizing the Sound

The client ships with a default `Notification.mp3` in the `Assets` folder. To use your own notification sound, replace the file at:

```text
<app-directory>/Assets/Notification.mp3
```

The file must be a valid `.mp3` or `.wav` audio file. The replacement takes effect on the next app launch.

Alternatively, set a custom path in `~/.echohub/config.json`:

```json
{
  "notifications": {
    "enabled": true,
    "volume": 30,
    "soundFile": "/path/to/your/sound.mp3"
  }
}
```

When `soundFile` is set, EchoHub uses that file instead of the bundled default.

## Disabling Notifications

Uncheck the option in your profile, or edit the config directly:

```json
{
  "notifications": {
    "enabled": false
  }
}
```
