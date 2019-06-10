# yasc
Yet Another Streaming Control

## About
yasc is a fully-featured, low-latency, fast streaming (and recording) .NET UserControl based on GStreamer. It was originally written for .NET and WinForms in C#. Support for WPF is planned. 

### Features
Initial release supports h.264 via RTSP (MJPG coming soon) and built-in/USB webcam streaming, text overlays, realtime preview, recording, and more. It is designed to be as simple and easy to use as possible. 
* Base features: 
  * Streaming via RTSP or local USB webcam (built-in laptop cam works too).
  * Text overlays (supports colors, fonts, and frequent updates).
  * Stream recording (file splitting at time and size limits also supported). 
  * Fullscreen (double click or programmatically). 
  * Test video source (videotestsrc). 
  * Low-latency.
  * Simple.
  * Fast (GStreamer is written entirely in C). 
  * Extensible and flexible backend.
  * And more... 
* Planned: 
  * WPF support. 
  * MJPG support. 
  * Recording stitching (pause/resume recording). 
  * Audio recording. 
  * PIP (picture-in-picture). 
  * Multicast streaming source support.
  
As this is a relatively simple wrapper around the great GStreamer, many more features are possible and easily implementable. Behind the scenes, a gst Pipeline is created and manipulated to render the preview source and all the other operations. In addition, this can be used as a very thorough C# example program for GStreamer. 
  
## Installation
1. Install GStreamer. 
1. Start a new .NET WinForms project. 
1. Install GstSharp (gstreamer wrapper for C#) from Nuget.
1. Copy the yasc dll into your directory of choosing. 
1. Add Reference to yasc.dll. 

## Usage 
1. Add the yasc DLL to your toolbox. 
1. Drag a YascControl onto your form. 
1. Add buttons and event handlers as necessary.
1. Set streaming source index or URI. 
1. Call StartPreview().
1. The included example app demonstrates nearly all the features. 

## Motivation
I could not for the life of me find a low-latency, self-contained, low-cost plugin/UserControl/ActiveX control that met all of my criteria. Every other control I came across either was not low enough latency, couldn't handle h.264 via RTSP easily, or was out of budget. My criteria were: 
* **Critical:** Low-latency (150ms) or less (initial use-case is a local RTSP stream) glass-to-glass.
* Text overlays (graphical is a plus), updateable at >1Hz.
* Handles RTSP at 720p or above. 
* Able to record to disk and grab frames (snapshot). 
* Works easily in C# (existing app constraint). 
* Not outrageously expensive for something that may or may not work ($500USD or less).

Not being able to find something that met all these criteria (yes I tried ffmpeg and VLC libs - latency and flexibility were the main sticking points), I finally bit the bullet and figured out GStreamer. After I learned enough to implement my desired functionality, I wrote a control which became yasc. Being a one-man-software-shop I didn't have a whole team (or enough time) to write something entirely from scratch; thus I hope others in a similar situation find this quick and easy to use.

Furthermore, I found the existing landscape around video previewing and rendering to be quite confusing and not at all well documented for newcomers like me. DirectShow is cumbersome (and not supported anymore, depending who you ask), ffmpeg is...interesting..., libvlc is slow.... The primary goal of this project is to make the hard easy and complex simple to lower the barrier of entry for creative programmers looking for a simple solution. 

## Contributing 
I'm sure there are many better ways to do things; please shoot me a message and/or a pull request. Always open to suggestions and improvements. 

## Acknowledgements
Thanks to the GStreamer team and everyone smarter than me who puts in the effort to make great software that I can barely understand enough to use. 
