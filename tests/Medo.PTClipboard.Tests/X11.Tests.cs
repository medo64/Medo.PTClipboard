namespace Tests;

using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Medo;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class X11_Tests {

    [TestMethod]
    public void Clipboard() {
        lock (Helpers.FullTestLock) {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                var textP = "P-" + RandomNumberGenerator.GetHexString(8);
                var textC = "C-" + RandomNumberGenerator.GetHexString(8);

                var clipboardProvider = new PTClipboardX11Provider();
                clipboardProvider.SetSelectionText(textP);
                clipboardProvider.SetClipboardText(textC);
                clipboardProvider.SetSelectionText(textP);

                Assert.AreEqual(textP, clipboardProvider.GetSelectionText());
                Assert.AreEqual(textC, clipboardProvider.GetClipboardText());
            } else {
                Assert.Inconclusive("Only supported on Linux.");
            }
        }
    }

    [TestMethod]
    public void Selection() {
        lock (Helpers.FullTestLock) {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                var textP = "P-" + RandomNumberGenerator.GetHexString(8);
                var textC = "C-" + RandomNumberGenerator.GetHexString(8);

                var clipboardProvider = new PTClipboardX11Provider();
                clipboardProvider.SetClipboardText(textC);
                clipboardProvider.SetSelectionText(textP);
                clipboardProvider.SetClipboardText(textC);

                Assert.AreEqual(textP, clipboardProvider.GetSelectionText());
                Assert.AreEqual(textC, clipboardProvider.GetClipboardText());
            } else {
                Assert.Inconclusive("Only supported on Linux.");
            }
        }
    }

}
