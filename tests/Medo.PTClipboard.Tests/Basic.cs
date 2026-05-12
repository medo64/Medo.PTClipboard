namespace Tests;

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Medo;
using System.Security.Cryptography;

[TestClass]
public class Basic {

    private static readonly object Lock = new();

    [TestMethod]
    public void Primary() {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                var textP = "P-" + RandomNumberGenerator.GetHexString(8);
                var textC = "C-" + RandomNumberGenerator.GetHexString(8);
                PTClipboard.Main.SetText(textC);
                PTClipboard.Selection.SetText(textP);
                PTClipboard.Main.SetText(textC);
                Assert.AreEqual(textP, PTClipboard.Selection.GetText());
                Assert.AreEqual(textC, PTClipboard.Main.GetText());
        } else {
            Assert.Inconclusive("Only supported on Linux.");
        }
    }

    [TestMethod]
    public void Clipboard() {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                var textP = "P-" + RandomNumberGenerator.GetHexString(8);
                var textC = "C-" + RandomNumberGenerator.GetHexString(8);
                PTClipboard.Selection.SetText(textP);
                PTClipboard.Main.SetText(textC);
                PTClipboard.Selection.SetText(textP);
                Assert.AreEqual(textP, PTClipboard.Selection.GetText());
                Assert.AreEqual(textC, PTClipboard.Main.GetText());
        } else {
            Assert.Inconclusive("Only supported on Linux.");
        }
    }

}
