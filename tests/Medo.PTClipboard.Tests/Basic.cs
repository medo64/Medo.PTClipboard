namespace Tests;

using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Medo;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Basic_Tests {

    [TestMethod]
    public void Clipboard() {
        lock (Helpers.FullTestLock) {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                PTClipboard.Initialize();

                var textP = "P-" + RandomNumberGenerator.GetHexString(8);
                var textC = "C-" + RandomNumberGenerator.GetHexString(8);

                PTClipboard.Selection.SetText(textP);
                PTClipboard.Main.SetText(textC);
                PTClipboard.Selection.SetText(textP);

                Assert.AreEqual(textP, PTClipboard.Selection.GetText());
                Assert.AreEqual(textC, PTClipboard.Main.GetText());

                PTClipboard.Terminate();
            } else {
                Assert.Inconclusive("Platform not supported.");
            }
        }
    }

    [TestMethod]
    public void Selection() {
        lock (Helpers.FullTestLock) {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                PTClipboard.Initialize();

                var textP = "P-" + RandomNumberGenerator.GetHexString(8);
                var textC = "C-" + RandomNumberGenerator.GetHexString(8);

                PTClipboard.Main.SetText(textC);
                PTClipboard.Selection.SetText(textP);
                PTClipboard.Main.SetText(textC);

                Assert.AreEqual(textP, PTClipboard.Selection.GetText());
                Assert.AreEqual(textC, PTClipboard.Main.GetText());

                PTClipboard.Terminate();
            } else {
                Assert.Inconclusive("Platform not supported.");
            }
        }
    }

    [TestMethod]
    public void DoubleInitialize() {
        lock (Helpers.FullTestLock) {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                PTClipboard.Initialize();
                PTClipboard.Initialize();

                var textP = "P-" + RandomNumberGenerator.GetHexString(8);
                var textC = "C-" + RandomNumberGenerator.GetHexString(8);

                PTClipboard.Main.SetText(textC);
                PTClipboard.Selection.SetText(textP);
                PTClipboard.Main.SetText(textC);

                Assert.AreEqual(textP, PTClipboard.Selection.GetText());
                Assert.AreEqual(textC, PTClipboard.Main.GetText());

                PTClipboard.Terminate();
            } else {
                Assert.Inconclusive("Platform not supported.");
            }
        }
    }

    [TestMethod]
    public void DoubleTerminate() {
        lock (Helpers.FullTestLock) {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                PTClipboard.Initialize();

                var textP = "P-" + RandomNumberGenerator.GetHexString(8);
                var textC = "C-" + RandomNumberGenerator.GetHexString(8);

                PTClipboard.Main.SetText(textC);
                PTClipboard.Selection.SetText(textP);
                PTClipboard.Main.SetText(textC);

                Assert.AreEqual(textP, PTClipboard.Selection.GetText());
                Assert.AreEqual(textC, PTClipboard.Main.GetText());

                PTClipboard.Terminate();
                PTClipboard.Terminate();
            } else {
                Assert.Inconclusive("Platform not supported.");
            }
        }
    }

}
