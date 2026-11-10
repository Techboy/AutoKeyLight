# Cam On Key Light

Automatically turns on and off your Elgato lights when you turn on and off your webcam.

## What are Elgato lights?

[Elgato](https://www.elgato.com/) makes Wi-Fi LED panel lights, such as the Key Light, Key Light Air and Key Light Mini, for people who present, stream or work on camera. You can adjust their brightness and colour temperature from your computer or phone.

## Why use them on calls?

Most rooms light you badly for video. A window behind you leaves your face in shadow, and overhead lights cast harsh shadows. A Key Light set up in front of you gives even, soft light on your face, so you look clear and natural on Teams, Zoom, Google Meet and other calls.

## What Cam On Key Light does

You shouldn't have to remember to switch your lights on before every call and off again afterwards. Cam On Key Light runs in the Windows system tray and watches for any app using your webcam. When a call turns your camera on, it turns your lights on. When the camera goes off, it turns them off again after a short delay. You can still switch them on or off by hand from the tray at any time.

## Get cashback on an Elgato light

Buying an Elgato light? Sign up through my [TopCashback referral link](https://www.topcashback.co.uk/ref/techboy) to get cashback on your purchase.

![Cam On Key Light main window](docs/autokeylight-window.png)

The app will try finding your lights IPs from an existing Elgato Control Center installation. If that fails, please add them manually.

If you have a "phantom" IP where after removing the IP, it comes back after restarting the app, it's most likely due to the Elgato control center app being installed with the IP in its settings. To fix that, open %APPDATA%\Elgato\ControlCenter\Settings.xml and remove any \<Acessory\> tags inside \<Accessories\>, or just delete the file altogether.

To find your keylight IP:

![image](https://user-images.githubusercontent.com/15985195/174424927-f0acc48a-5e97-4d59-9b7b-bdebf4a085aa.png)

If you know your way around your router, you could get the lights fixed IPs to guarantee reliability.

This fork adds appearance, light control, shortcuts, per-light status, light discovery, quiet hours, per-app camera rules, light groups, notifications, and updates from **Techboy/CamOnKeyLight** stable releases. It does not change Windows networking, VPN, routes, or company security software.

Cam On Key Light is based on [astutejoe/AutoKeyLight](https://github.com/astutejoe/AutoKeyLight). It is MIT licensed. Copyright (c) 2022 Gabriel Garcia. See [LICENSE](LICENSE).

## Light control

**Auto / On / Off** is in the window and on the tray icon menu. The choice is saved for this Windows user.

- **Auto** is the original behavior and the default. Lights follow the webcam. The on delay and the off delay apply. An existing install stays on Auto.
- **On** turns every configured light on and keeps it on, even when the webcam turns off. The app restores On the next time it starts.
- **Off** turns every configured light off immediately and keeps it off, even when the webcam turns on. The app restores Off the next time it starts.
- Choosing **Auto** again sets the lights from the webcam's current state immediately. Delays apply only to webcam changes after that.
- The webcam monitor keeps running so the window can show the real camera state. While the mode is On or Off, that monitor does not change the lights.
- The tray icon still shows whether the camera is on. The window title, the status line, and the check mark on the tray menu show Auto, On, or Off.

## Off delay

The **Off delay (ms)** box is the pause after the camera turns off before automatic mode turns the lights off. It is there so switching camera apps does not flicker the lights.

- New installs use **1500 ms**.
- If you already saved a value, including 0, that saved value is kept.
- Set it to 0 for an immediate automatic off. The maximum is 60 seconds.
- If the camera comes back before the delay ends, the pending off is cancelled and the lights stay as they were.
- **Manual Off does not wait.** It turns the lights off as soon as you choose it.

## Keyboard shortcuts

Shortcuts are optional and **none are set by default**, so the app does not take keys another program already uses.

Open **Shortcuts** from the window or the tray menu. Choose Set, then press Ctrl, Alt, or Shift together with a letter, number, or function key. Esc cancels. Clear removes one. A common choice that rarely clashes is Ctrl+Alt+Shift with a letter.

The shortcut works while the window is closed. The tray menu shows it next to Auto, On, or Off. Common editing and Windows shortcuts, such as Ctrl+C and Alt+F4, are refused. If Windows says another program already owns the shortcut, the app tells you and leaves the previous shortcut in place. Shortcuts are released when you exit from the tray menu.

## Light status

Each light has its own status: **On**, **Off**, **Unreachable**, **Unexpected reply**, or **Checking…**. The summary line does not say the lights are on after a call fails. The **Description** column is saved with the address, for example "Light on the left". Use **Add** or **Edit**, double-click a row, or press Enter or F2. Delete removes the selected light.

**Retry** checks every light again. A light that does not answer within 5 seconds is shown as unreachable. That is only a connection check. It does not change VPN settings, routes, or security software.

**Find** searches the local network for Elgato lights (`_elg._tcp.local`) and lists the name, address, and port. Lights you already added are skipped. If none answer, the dialog says so. A light that was added from Find remembers its name and serial. If that address later stops responding, the app looks for the same light and updates the address when the match is unique.

The optional **Group** on a light shows in the list and on the tray. The tray can turn one group on or off, or set its brightness to 25, 50, 75, or 100 percent. Automatic on and off still follows the camera for every light unless Options limits that to chosen groups.

## Options

**Options** on the menu bar, and **Options…** on the tray, holds the settings that are not on the main window.

- **Quiet hours** have a start, an end, and days of the week. A range such as 22:00 to 07:00 crosses midnight. While quiet hours are active, automatic mode does not turn lights on. Tray On and Off still work. The tray tooltip and menu show whether quiet hours are active.
- **Camera apps** can stay on **All apps**, which is the existing behavior. **Only these apps** or **All except these apps** uses the apps Windows reports for the camera, shown by exe name, plus names you type yourself.
- **Notifications** for a light going offline or coming back are on by default. A change has to hold for about 20 seconds before a balloon is shown, and the first check after startup is quiet. A notice after an update is installed is off until you turn it on.

## Appearance

**Light**, **Dark**, or **Follow Windows**. The choice is saved. Follow Windows tracks the app theme. High contrast keeps Windows system colors.

## Install

### Which file do I download?

- **`CamOnKeyLight-Setup.exe`**: download this one. It's the normal installer for most people.
- **`CamOnKeyLight-win-x64.zip`**: the portable version. Unzip it and run it without installing. The automatic updater also uses this file.
- **`.sha256` files**: checksums for checking a download. You can ignore them.

The build is not code-signed. There is no paid certificate. Windows SmartScreen may say "Windows protected your PC" the first time you open the installer or the program. Choose **More info**, then **Run anyway**.

### Per-user installer

1. Open the latest stable release: https://github.com/Techboy/CamOnKeyLight/releases/latest
2. Download `CamOnKeyLight-Setup.exe`. `CamOnKeyLight-Setup.exe.sha256` is the checksum for that file.
3. Run the setup. It does not ask for administrator approval. It installs to `%LOCALAPPDATA%\Programs\CamOnKeyLight`, adds a **Cam On Key Light** shortcut to your Start menu, and adds a per-user uninstall entry in Windows Settings. If an older Auto Key Light install is there, setup closes it, removes that install, its Start menu shortcut, and its Apps & features entry, and keeps your settings.
4. If Cam On Key Light or Auto Key Light is already running, the setup asks to close it before replacing the program files.
5. Uninstall removes the program, the Start menu shortcut, and a sign-in startup entry that still points at this install. It leaves `%LOCALAPPDATA%\CamOnKeyLight` in place, which is where your settings and update backup live.

### Portable zip

The zip is still published for people who want a folder they can move.

1. Download `CamOnKeyLight-win-x64.zip` and, if you want to check it, `CamOnKeyLight-win-x64.zip.sha256`.
2. Extract it to a normal folder you can write to. Do not run it from inside the zip, and do not put it in `C:\Windows`.
3. The .NET runtime is included.

The automatic updater always downloads that zip, whether you installed with setup or extracted the zip. It replaces the program files in the folder you are running, and it does not switch you from a portable folder to the Start menu install. The setup program is not an automatic update.

## Settings

Light addresses, descriptions, groups, delays, theme, light mode, shortcuts, quiet hours, camera-app rules, notification choices, and the update checkbox are stored in `%LOCALAPPDATA%\CamOnKeyLight\user-settings.json`. That file is outside the program folder, so installing, updating, or moving the program does not erase it. A settings file from an older version still loads; the new choices use their defaults.

The first time this version starts, if that file is missing and `%LOCALAPPDATA%\AutoKeyLight\user-settings.json` exists, it copies the old file and leaves the original in place. It also copies an existing `user.config` from the older per-folder settings location when that file still has your lights. A saved off delay of 0 stays 0. Descriptions start blank until you edit them.

## Sign-in

**Start when I sign in** follows the current user's Startup apps list (`HKCU\...\Run`). It runs after you sign in. It does not run before Windows login, and it does not need administrator approval. The command is stored in quotes under the name `CamOnKeyLight`. An older `AutoKeyLight` or `MyApp` entry that pointed at this app is moved to that name and updated to the copy you are running, so a previous folder is not launched after you move or install the app. If sign-in startup was off, it stays off.

## Updates

Installed copies check only **stable** releases of **Techboy/CamOnKeyLight** and download `CamOnKeyLight-win-x64.zip`. They do not update from the upstream [astutejoe/AutoKeyLight](https://github.com/astutejoe/AutoKeyLight) project, and they do not use a token or other credential. A copy still on 0.6.0 looks for `AutoKeyLight-win-x64.zip` on the previous repository name; that file is also attached to the release so it can move across.

**Install updates automatically** is on by default. Uncheck it to stop background checks and unattended installs. **Check for updates** still works, and **Install update** appears only after a package has been downloaded and checked.

An update is not applied while the webcam is on. Automatic install also waits until the camera has been off for 15 seconds. If GitHub cannot be reached, the app keeps the version you already have and says so.

A download is accepted only from this repository's GitHub release. The zip is checked against GitHub's asset digest, when GitHub provides one, and against the published SHA-256 file. The zip is then extracted with path checks. The previous program files are copied to `%LOCALAPPDATA%\CamOnKeyLight\updates\backup` before they are replaced, and restored if the copy fails or the new program's version does not match the release. The app restarts into the tray after a successful update.

## Releasing a new version

This uses the free `windows-latest` runner on this public repository. It does not use a paid runner, a code-signing certificate, an Actions cache, or a stored Actions artifact.

For 0.7.0, publish the release while the repository is still named **Techboy/AutoKeyLight**, then rename it to **Techboy/CamOnKeyLight**. v0.6.0 only accepts download URLs under `/Techboy/AutoKeyLight/releases/download/`. GitHub's release API uses the current repository name in those URLs, so renaming first would make v0.6.0 refuse the package.

1. Pick the next version, for example `0.7.0`.
2. On GitHub, create a release in **Techboy/AutoKeyLight**. Tag it `v0.7.0`. Leave it as a normal release, not a pre-release and not a draft.
3. Publish the release. The release workflow attaches `CamOnKeyLight-win-x64.zip`, `CamOnKeyLight-win-x64.zip.sha256`, `AutoKeyLight-win-x64.zip`, `AutoKeyLight-win-x64.zip.sha256`, `CamOnKeyLight-Setup.exe`, and `CamOnKeyLight-Setup.exe.sha256`.
4. Wait until those files are listed. People installing for the first time run the setup program. Copies already on 0.7.0 or later download `CamOnKeyLight-win-x64.zip`. Copies still on 0.6.0 download `AutoKeyLight-win-x64.zip`.
5. Rename the repository to **Techboy/CamOnKeyLight**. GitHub redirects the old repository URLs. Do this after the 0.6.0 copies you care about have updated, or give those PCs the new setup program.

The version in the program comes from that tag. The workflow builds the zip and the setup from the same folder. Do not attach those files by hand unless the workflow failed; the app expects the checksum and GitHub's digest to describe the same zip.

## Build

The logic tests run on .NET 8:

```bash
dotnet test CamOnKeyLight.Core.Tests/CamOnKeyLight.Core.Tests.csproj -c Release
```

The Windows app itself builds on Windows:

```powershell
dotnet publish CamOnKeyLight/CamOnKeyLight.csproj -c Release -r win-x64 --self-contained true
```
