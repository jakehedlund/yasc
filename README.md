# yasc (beta) [![paypal](https://img.shields.io/badge/Donate-PayPal-green.svg)](https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=UCK87CLYLLRF2&currency_code=USD)
Yet Another Streaming Control

## About
yasc is a high-level, fully-featured, low-latency, fast, lightweight, adjective-laden, streaming (and recording) .NET UserControl based around [GStreamer](https://gstreamer.freedesktop.org/documentation/index.html). It is written for .NET and WinForms in C#. Support for WPF is planned. It is intended as an easy-to-use, drop-in video streaming control for programmers who just want to program and not deal with video rendering/display or learning GStreamer. 

### Demo app screenshot

![demo_app](/res/cap1.png)

## Features
Initial release supports h.264 and MJPG via RTSP (anything [decodebin](https://gstreamer.freedesktop.org/documentation/playback/decodebin.html) can handle) and built-in/USB webcam streaming, text overlays, realtime preview, recording, and more. It is designed to be as simple and easy to use as possible with drop-and-go functionality. 

* Current features: 
  * Streaming via RTSP or local USB webcam (built-in laptop cam works too).
  * OSD: on screen display - text overlays (supports colors, fonts, and frequent updates [>10Hz]).
  * Stream recording (file splitting at time and size thresholds also supported). 
  * Fullscreen (double click or programmatically). 
  * Snapshots (frame-grabs). 
  * Test video source (videotestsrc). 
  * Low-latency (150ms or better using RTSP). 
  * Simple to use. 
  * Events for preview started/stopped, etc.
  * Fast (GStreamer is written entirely in C). 
  * Extensible and flexible backend.
  * And more... 
* Planned:
  - [x] 64-bit support. 
  - [ ] File playback (only live sources implemented for now). 
  - [ ] WPF support. 
  - [ ] Nuget package.
  - [ ] Recording stitching (pause/resume recordings with same file). 
  - [ ] Audio previewing (and recording).
  - [ ] PIP (picture-in-picture). 
  - [ ] Multicast streaming source support. (multiple clients using multicast URI)
  - [ ] Separate preview and record streams (lower quality preview, HD recording).
  - [ ] Separate/different preview and record overlays. 
  - [ ] Subtitle overlays (raw stream without overlay, rendered by player [e.g. VLC] as subtitles, saved at record time as .srt). 
  - [ ] Better flexibility for different pipelines and stream formats. 
  - [ ] Framerate manipulation (e.g. drop every other frame).
  - [ ] Statistics reporting (current framerate, encoding speed, etc.).
  - [ ] Graphical overlays (cairooverlay or rsvgoverlay).
  - [ ] Merge modules included? (eliminate external gst dependency). 
  - [ ] RTSP server support (re-streaming with OSD).
  
As this is a relatively simple wrapper around the great GStreamer, many more features are possible and easily implementable. Behind the scenes, a gst Pipeline is created and manipulated to render the preview source and all the other operations. Therefore, this can be used as an ~~comprehensive~~ advanced C# example program for GStreamer. 
  
## Installation
1. Install GStreamer (use the MinGW version here: https://gstreamer.freedesktop.org/download/ either 32 or 64 should work, 1.14.0 or higher).
1. Start a new .NET WinForms project. 
1. Install GstSharp (gstreamer wrapper for C#) from Nuget.
1. Copy the yasc dll into your directory of choosing. 
1. "Add Reference..." to yasc.dll. 

## Usage 
1. Add the yasc DLL to your toolbox (right-click -> Choose Items...)
1. Drag a YascControl onto your form. 
1. Add buttons and event handlers as necessary.
1. Set YascControl.CamType to the correct camera type. 
1. Set streaming source index or URI. 
1. Call StartPreview().
1. The included example app (YascTestApp.sln) demonstrates nearly all the features and can function as a basic streaming/recording app. 

## Motivation
I could not for the life of me find a low-latency, self-contained, low-cost plugin/UserControl/ActiveX control that met all of my criteria. Every control I came across either was not low enough latency, couldn't handle h.264 via RTSP easily, or was out of budget. My criteria were: 

* **Critical:** Low-latency (150ms or less glass-to-glass). Initial use-case is a local RTSP stream for a tethered remote-controlled vehicle.
* Text overlays (graphical is a plus), updateable at 1Hz or better.
* Handles RTSP at 720p or above. 
* Able to record to disk and grab frames (snapshot). 
* Works relatively easily in C# WinForms (existing app constraint). 
* Not outrageously expensive for something that may or may not work (i.e. no demo available).
* Decent framerate (30 fps ideal).

Not being able to find something that met all these criteria (yes I tried ffmpeg and VLC libs - latency and flexibility were the main sticking points), I finally bit the bullet and learned GStreamer. After I learned enough to implement the desired functionality, I wrote the control which became yasc. 

Furthermore, I found the existing landscape around video previewing and rendering to be quite confusing and not at all well documented for newcomers like me. DirectShow is cumbersome (and not supported anymore, depending who you ask), ffmpeg is...interesting..., libvlc is slow and inflexible.... The primary goal of this project is to make the hard easy and complex simple to lower the barrier of entry for creative programmers looking for a simple streaming solution. 

Windows 10 and WPF seems to be making moves in this domain, but most options I found didn't meet one or more of my critera above. In addition they were mostly black boxes without much room for extensibility. Hence, yet another streaming control enters the arena. 

## Contributing 
I'm sure there are many better ways to do things; please shoot me a message and/or a pull request. Always open to suggestions and improvements. Also, if anyone is aware of an existing program/control that meets all of my critera, let me know. 

## Acknowledgements
Thanks to the GStreamer team and everyone smarter than me who puts in the effort to make great software that I can barely understand enough to use. 

[![paypal](https://img.shields.io/badge/Donate-PayPal-green.svg)](https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=UCK87CLYLLRF2&currency_code=USD)
