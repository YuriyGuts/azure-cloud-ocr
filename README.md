# AzureCloudOCR

A simple distributed cloud OCR (Optical Character Recognition) application that uses
Windows Azure Web and Worker Roles, and stores data in Windows Azure Blobs, Tables, and Queues.

__General workflow:__

1. User uploads an image on a web page served by the Web Role, specifies an email address, and enters a CAPTCHA.
2. Web Role uploads the image to Blob Storage, registers the job in an Azure Table and creates an OCR queue item.
3. OCR Worker Role pulls messages from the OCR queue, recognizes the images using Google Tesseract, stores the recognized text to Blob Storage, and creates Email queue items.
4. Email Worker Role pulls messages from the Email queue, reads the recognized text from Blob Storage, and emails it via SendGrid.
5. User receives the recognized text as an email with a text attachment.

![Architecture](/assets/architecture-small.png)

## Project Setup

In order to work with the source code, you'll need the following:

1. Visual Studio 2012.
2. NuGet package manager.
3. Windows Azure SDK 2.0 + Windows Azure Tools for Visual Studio 2012.
4. Windows Azure account (if you want to deploy the app to Azure).
5. Recaptcha key pair.
6. SendGrid account (to send emails).

## Debugging the project locally

1. Edit `ServiceConfiguration.Local.cscfg`:
   * Specify your Recaptcha key pair in WebRole configuration.
   * Specify your SendGrid credentials in EmailWorkerRole configuration.

2. Set `AzureCloudOCR` as the startup project and press F5. The application will be launched in Windows Azure Compute Emulator.

## Deploying to Windows Azure

Edit `ServiceConfiguration.Cloud.cscfg`:
   * Update Windows Azure Storage connection strings to match your storage account name and key.
   * Specify your Recaptcha key pair in WebRole configuration.
   * Specify your SendGrid credentials in EmailWorkerRole configuration.

The application can be deployed from Visual Studio 2012. Right-click the `AzureCloudOCR` project and select `Publish...`.

## Troubleshooting & Useful Tools

1. Build errors on fresh `git clone`:
   * Make sure that NuGet package restore is enabled and that NuGet is able to download missing files from the Internet.
   * Make sure you have Windows Azure SDK 2.0 installed.
2. You can use [Azure Storage Explorer](http://azurestorageexplorer.codeplex.com/) to browse the content of blobs, tables, and queues.

## License

The code is licensed under [the Apache License, version 2.0](http://www.apache.org/licenses/LICENSE-2.0).
