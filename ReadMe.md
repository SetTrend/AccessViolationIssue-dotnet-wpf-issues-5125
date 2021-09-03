# Test Case For Reproducing .NET 5 WPF Access Violation Issue

This repository provides an MVCE for WPF issue ["CroppedBitmap crashes with AccessViolationException"](https://github.com/dotnet/wpf/issues/5125).

It comprises a small WPF .NET 5 Visual Studio project with a single window, containing
all code necessary to reproduce the issue.

![MainWindow screenshot](.doc/images/1%20-%20Screenshot.png)

## Steps To Reproduce

- Download this repository
- Open, compile and debug the Visual Studio solution contained therein
- After the compiled program's main window opens, click the "Save" toolbar button located
  in the upper right corner of the main window:
	![MainWindows Save toolbar button](.doc/images/2%20-%20Save%20Image.png)
	This will call the `MainWindow::SaveImage()` method.
- Select a destination to save the expected PNG file to:
  ![Save As dialog window](.doc/images/3%20-%20Save%20As.png)
	The program will now try to convert the main window's content to a PNG file.

	The resulting PNG file is supposed to have a size of `310,000 * 2,000` pixels.
- The WPF Runtime will refuse to encode a PNG image being this large:
  ![WPF cannot save a PNG file this large](.doc/images/4%20-%20Cannot%20save%20large%20PNG%20file.png)
	The [PNG standard](https://www.w3.org/TR/2003/REC-PNG-20031110/#11IHDR), however, allows image sizes of up to 2<sup>32</sup> * 2<sup>32</sup>
	pixels.
- In the message dialog above, hit `[Yes]` to have the program split the image into slices of
  approx. 20,000 pixels width each.
- The WPF Runtime will then run into an [`AccessViolationException`](https://docs.microsoft.com/dotnet/api/system.accessviolationexception):
  ![WPF Runtime AccessViolationException](.doc/images/5%20-%20.NET%20Runtime%20exception.png)
- Despite the fact that the corresponding member function is being decorated with the
  [`HandleProcessCorruptedStateExceptionsAttribute`](https://docs.microsoft.com/dotnet/api/system.runtime.exceptionservices.handleprocesscorruptedstateexceptionsattribute),
	the exception is not getting caught.

## WPF Issues Demonstrated By This Example

1. Large PNG files cannot be encoded/created by `PngBitmapEncoder`.
1. Saving a PNG file larger than `20,000 * 2,000` pixels raises an `AccessViolationException`.
1. The `AccessViolationException` cannot be caught. The `try-catch` construct I added to `SaveTiledImage` doesn't catch the exception although I have added the
  [`HandleProcessCorruptedStateExceptionsAttribute`](https://docs.microsoft.com/dotnet/api/system.runtime.exceptionservices.handleprocesscorruptedstateexceptionsattribute)
	to the corresponding method. The program is just getting aborted without any chance for me
	to catch the exception.

Eventually, this could be a security vulnerability.