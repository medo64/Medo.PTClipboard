Medo.PTClipboard
================

Plain-text clipboard that supports Windows and Linux (both X11 and Waylands).
This library provides essential functions for retrieving and setting UTF-8 text
content for both the standard clipboard and the primary selection. It allows for
straightforward clipboard interactions without the overhead of more
comprehensive libraries.

Features:
* Works on both Windows and Linux (Wayland, X11)
* Supports clipboard and primary selection buffers
* .NET AOT support

You can find packaged library at [NuGet][nuget_ptclipboard]


## Usage

To write and read clipboard:
~~~csharp
using Medo;

PTClipboard.SetText("My text.");
var text = PTClipboard.GetText();
~~~

To write and read X11 primary selection (aka, middle-click clipboard):
~~~csharp
using Medo;

PTClipboard.Selection.SetText("My text.");
var selectionText = PTClipboard.Selection.GetText();
~~~


[nuget_ptclipboard]: https://www.nuget.org/packages/Medo.PTClipboard/
