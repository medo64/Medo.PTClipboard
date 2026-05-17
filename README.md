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


## Usage

To write and read clipboard:
~~~csharp
using System;
using Medo;

PTClipboard.SetText("My text.");
Console.WriteLine(PTClipboard.GetText());
~~~

To write and read X11 primary selection (aka, middle-click clipboard):
~~~csharp
using System;
using Medo;

PTClipboard.Selection.SetText("My text.");
Console.WriteLine(PTClipboard.Selection.GetText());
~~~
