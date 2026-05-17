namespace Tests;

using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Medo;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Win32_Tests {

    [TestMethod]
    public void Clipboard() {
        lock (Helpers.FullTestLock) {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                var textP = "P-" + RandomNumberGenerator.GetHexString(8);
                var textC = "C-" + RandomNumberGenerator.GetHexString(8);

                var clipboardProvider = new PTClipboardWin32Provider();
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
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                var textP = "P-" + RandomNumberGenerator.GetHexString(8);
                var textC = "C-" + RandomNumberGenerator.GetHexString(8);

                var clipboardProvider = new PTClipboardWin32Provider();
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
