using System;
using System.Security.Cryptography;
using Medo;

namespace PTClipboardTest;

internal static class Program {
    private static void Main() {
        if (Console.IsInputRedirected) {  // if input is redirected, just show the current clipboard
            WriteClipboardText();
            Console.WriteLine();
            WriteSelectionText();
            Console.WriteLine();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("<C> Set clipboard text");
        Console.WriteLine("<S> Set selection text");
        Console.WriteLine("<V> Show current clipboard");
        Console.ResetColor();
        Console.WriteLine();

        while (true) {
            var key = Console.ReadKey(true);
            switch (key.Key) {
                case ConsoleKey.Escape:
                case ConsoleKey.Q: return;

                case ConsoleKey.C:
                    SetClipboardText("C-" + RandomNumberGenerator.GetHexString(6) + "-C");
                    Console.WriteLine();
                    break;

                case ConsoleKey.S:
                    SetSelectionText("S-" + RandomNumberGenerator.GetHexString(6) + "-S");
                    Console.WriteLine();
                    break;

                case ConsoleKey.V:
                    WriteClipboardText();
                    Console.WriteLine();
                    WriteSelectionText();
                    Console.WriteLine();
                    break;
            }
        }
    }

    private static void SetClipboardText(string text) {
        PTClipboard.Main.SetText(text);

        Console.WriteLine("Set clipboard text:");
        Console.ForegroundColor = ConsoleColor.DarkGreen;
        Console.WriteLine(PTClipboard.Main.GetText());
        Console.ResetColor();
    }

    private static void SetSelectionText(string text) {
        PTClipboard.Selection.SetText(text);

        Console.WriteLine("Set primary selection text:");
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine(PTClipboard.Selection.GetText());
        Console.ResetColor();
    }

    private static void WriteClipboardText() {
        var clipboardText = PTClipboard.Main.GetText();

        Console.WriteLine("Clipboard text:");
        if (string.IsNullOrEmpty(clipboardText)) {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("(empty)");
        } else {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(clipboardText);
        }
        Console.ResetColor();
    }

    private static void WriteSelectionText() {
        var selectionText = PTClipboard.Selection.GetText();

        Console.WriteLine("Primary selection text:");
        if (string.IsNullOrEmpty(selectionText)) {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("(empty)");
        } else {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(selectionText);
        }
        Console.ResetColor();
    }

}
