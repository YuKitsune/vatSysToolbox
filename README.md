<h1 align="center">
  vatSys Toolbox
</h1>

<h3 align="center">
  A class library that makes it easier to develop vatSys plugins.

  [![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/yukitsune/vatSysToolbox/build.yml?branch=main)](https://github.com/YuKitsune/vatSysToolbox/actions/workflows/build.yml)
  [![License](https://img.shields.io/github/license/YuKitsune/vatSysToolbox)](https://github.com/YuKitsune/vatSysToolbox/blob/main/LICENSE)
  [![Latest Release](https://img.shields.io/github/v/release/YuKitsune/vatSysToolbox?include_prereleases)](https://github.com/YuKitsune/vatSysToolbox/releases)
  [![built with Fallout](https://img.shields.io/badge/built%20with-Fallout-F5C800?logo=data%3Aimage%2Fsvg%2Bxml%3Bbase64%2CPHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAyNCAyNCI%2BPGNpcmNsZSBjeD0iMTIiIGN5PSIxMiIgcj0iMTIiIGZpbGw9IiNGNUM4MDAiLz48ZyBmaWxsPSIjMTExIj48Y2lyY2xlIGN4PSIxMiIgY3k9IjEyIiByPSIzLjEiLz48cGF0aCBkPSJNOS45OSA5LjAyQTMuNiAzLjYgMCAwIDEgMTQuMDEgOS4wMkwxOC40OSAyLjM4QTExLjYgMTEuNiAwIDAgMCA1LjUxIDIuMzhaTTE1LjU5IDExLjc1QTMuNiAzLjYgMCAwIDEgMTMuNTggMTUuMjRMMTcuMDkgMjIuNDNBMTEuNiAxMS42IDAgMCAwIDIzLjU3IDExLjE5Wk0xMC40MiAxNS4yNEEzLjYgMy42IDAgMCAxIDguNDEgMTEuNzVMMC40MyAxMS4xOUExMS42IDExLjYgMCAwIDAgNi45MSAyMi40M1oiLz48L2c%2BPC9zdmc%2B)](https://github.com/Fallout-build/Fallout)
</h3>

## Overview

vatSys Toolbox contains the code that many vatSys plugins need. It has these features:

- **Windows:** Show WPF views in native vatSys windows.
- **Styles and controls:** WPF styles and controls inherit the style defined in the loaded vatSys profile.
- **Workload management:** Background work and queue abstractions.
- **Messages between plugins:** Send messages, requests, and broadcasts to other plugins in the same vatSys process.
- **Configuration:** Find configuration files in the vatSys profile.
- **DPI awareness:** Make sure that WPF content in vatSys has the correct scale.

## Installation

The toolbox is available from the GitHub Packages NuGet feed.

1. Make a GitHub personal access token (classic) with the `read:packages` scope.
2. Add the feed to the `nuget.config` file of your plugin:

    ```xml
    <?xml version="1.0" encoding="utf-8"?>
    <configuration>
      <packageSources>
        <add key="YuKitsune" value="https://nuget.pkg.github.com/YuKitsune/index.json" />
      </packageSources>
      <packageSourceCredentials>
        <YuKitsune>
          <add key="Username" value="%GITHUB_USERNAME%" />
          <add key="ClearTextPassword" value="%GITHUB_TOKEN%" />
        </YuKitsune>
      </packageSourceCredentials>
    </configuration>
    ```

3. Add the package to your plugin project:

    ```shell
    dotnet add package vatSysToolbox
    ```

The toolbox targets .NET Framework 4.7.2, and it references `vatSys.exe`. Set the `VatSysPath` MSBuild property to the vatSys installation directory if vatSys is not installed in the default location.

> [!IMPORTANT]
> Merge the toolbox into your plugin assembly (for example, with ILRepack). Other plugins can use different versions of the toolbox. If you do not merge the toolbox, vatSys can load the incorrect version.

## Usage

### Services

Register the toolbox services with `Microsoft.Extensions.DependencyInjection`. You must also register the logging services.

```csharp
var services = new ServiceCollection()
    .AddLogging()
    .AddVatSysToolbox("My Plugin", "dev.example.my-plugin");
```

The plugin name shows with the errors in the vatSys error window. Other plugins use the mailbox name to send messages to your plugin. Use a reverse DNS name for the mailbox name.

To use a different implementation of a service, register it before you call `AddVatSysToolbox`. You can also register each service with its own method (`AddClock`, `AddErrorReporter`, `AddWindowManager`, `AddWorkQueue`, and `AddPluginMessenger`).

### Windows

Use `WindowManager` to open a WPF view in a vatSys window. Each window has a key. The window manager keeps the last position of each key.

```csharp
windowManager.FocusOrCreateWindow(
    "my-plugin.settings",
    "Settings",
    windowHandle => new SettingsView(windowHandle));
```

- `FocusOrCreateWindow` focuses the window if it is already open.
- `TryCreateWindow` does nothing if the window is already open.
- `CloseWindow` closes the window.

The view receives an `IWindowHandle`. Use it to close or focus the window, or to know when the window closes or moves.

### Styles and controls

Add the styles to the resources of your view:

```xml
<UserControl.Resources>
    <ResourceDictionary Source="/vatSysToolbox;component/Styles.xaml" />
</UserControl.Resources>
```

Use the `VatSysColour` markup extension to get a colour from the vatSys profile:

```xml
<TextBlock Foreground="{toolbox:VatSysColour InteractiveText}" />
```

`VatSysFont` gives the font of the vatSys windows. `Bevel` gives the brushes and the sizes of the vatSys bevels.

The toolbox also contains these controls: `BeveledBorder`, `BeveledLine`, `Chevron`, `CustomScrollBar`, and `TextInput`.

### Error reporting

`IErrorReporter` writes errors to the log, and shows them in the vatSys error window. The error window shows each error message one time each minute at most.

```csharp
errorReporter.ReportError(exception, "Could not load the configuration");
```

### Background work

`WorkQueue` does work items one at a time, in the sequence that they were added. Work items that change the same state do not need a lock if they use the same queue.

```csharp
workQueue.Enqueue(async () => await UpdateSequence());
```

`BackgroundTask` runs a long-running worker that you can start and stop.

### Messages between plugins

`IPluginMessenger` sends messages to other plugins in the same vatSys process. The messages stay in memory.

- `Send` sends a message to one plugin.
- `Broadcast` sends a message to all the other plugins.
- `Request` sends a request to one plugin, and waits for the reply.

Each plugin has an inbound queue. If the recipient is not ready, the message stays in its queue until the recipient calls `Start`. Add your handlers before you call `Start`.

```csharp
var json = new JsonMessageSerializer();

messenger.Subscribe("dev.example.my-plugin.sequence-updated", (message, cancellationToken) =>
{
    var sequence = message.Deserialize<Sequence>(json);
    return Task.CompletedTask;
});

messenger.HandleRequests("dev.example.my-plugin.get-sequence", (request, cancellationToken) =>
    Task.FromResult(json.ToContent(GetSequence())));

messenger.Start();

await messenger.Broadcast("dev.example.my-plugin.sequence-updated", sequence, json);
var reply = await messenger.Request("OtherPlugin", "dev.example.other-plugin.get-status", query, json);
```

Use a reverse domain name as the prefix of each message type (for example, `dev.example.my-plugin.sequence-updated`). This prevents conflicts between plugins.

The message data is bytes. Each message has a content type, so plugins can use different formats. The toolbox contains `JsonMessageSerializer`. To use a different format (for example, MessagePack), implement `IMessageSerializer`. If a handler accepts more than one format, give all the serializers to `Deserialize`:

```csharp
var sequence = message.Deserialize<Sequence>(json, messagePack);
```

If a plugin receives a direct message or a request that it has no handler for, it shows an error in the vatSys error window. The requester receives a `MessagingException`.

### Configuration

`ConfigurationLocator` finds a configuration file. It searches these directories in this sequence:

1. `Profile\Plugins\Configs\<Plugin Name>`
2. `Profile\Plugins\Configs`
3. `Profile\Plugins`
4. `Profile`
5. The directory of the plugin assembly

```csharp
var configurationFilePath = ConfigurationLocator.LocateConfigurationFile(this, "MyPlugin.json");
```

### DPI awareness

WPF content in vatSys has the correct scale only if Windows runs vatSys as a DPI-unaware application. Call `DpiAwareness.EnsureDpiAwareness()` when your plugin starts. This method sets the Windows compatibility setting for `vatSys.exe`. If the setting changes, the method restarts vatSys.

## Building

Run the build script from the root of the repository:

```shell
./build.sh Compile
```

The build downloads vatSys if you do not set the `VatSysPath` parameter.
