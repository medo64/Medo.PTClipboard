Medo.PTClipboard
================

Plain-text clipboard that supports Windows and Linux (X11-only at this time but
it does work on some Wayland distributions too, e.g. Ubuntu). This library provides
essential functions for getting and setting UTF-8 text content for both primary
selection and standard clipboard. This makes it ideal for applications that
require straightforward clipboard interactions without the overhead of more
comprehensive libraries.

Features:
* Works on both Windows and Linux
* Supports both clipboard and primary selection buffers


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
