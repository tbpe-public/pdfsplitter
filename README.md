# PDF Splitter
This C# program uses Ghostcript to read PDFs into images so that ZXing can see if any of the pages have QR codes that have a "document-type" key as decoded by NewtonSoft's Json package. If it finds a QR code on a page, that page is considered a separator page marking the start of a document of that indicated type. It then builds separate documents using the iTextSharp library.
## Splitter Pages
There's a qrcodes folder for our general document types. Just print these out and insert on top of each document type in the pile you're scanning in.

# Building it
This is meant for Windows 10. There are no un-included dependencies. Build this for x64 using Visual Studio and deploy using the files in the "publish/Application Files/pdfsplit_version_blah/" folder.
## Deployment notes
When you build this for deployment, don't use the dump setup.exe file that is generated. Also uncheck the box that appends ".deploy" to all your files.  Make sure that the 64-bit DLL for Ghostscript is included.

# Running it
Call the executable from a batch file in the directory where you have a PDF file you'd like to split.

# Errata
I threw this together in a couple of days so it's pretty hodgepodge. But it works and is good for demo purposes and as a proof of concept and can be incorporated into other applications.
