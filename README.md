# GECO
GECO, a mobile sustainability companion that uses Gemini to provide tailored recommendations and foster eco-friendly habits.

## Features
- **Chat**: Communicate with a sustainability-tuned Gemini for your sustainability-related troubles.
- **Search**: Find topics while incorporating sustainability into the search using Brave Search and Gemini.
- **Habit Tracking**: Track your mobile phone habits, you will receive notifications when you've been doing unsustainable actions. Once a week has passed, you will receive a weekly report showcasing your habits from the previous week.

## Showcase
| Chat | Search | Weekly Report
| :---: | :---: | :---: |
| ![](https://media4.giphy.com/media/v1.Y2lkPTc5MGI3NjExaXVyOWczd2l1dnRzbnFvM3B3d2V4bHQ1Njl6a2g4bzNoc25xdXl4YiZlcD12MV9pbnRlcm5hbF9naWZfYnlfaWQmY3Q9Zw/uj8XTgcF9nUgsBsHi6/giphy.gif) | ![](https://media1.giphy.com/media/v1.Y2lkPTc5MGI3NjExcGt4Nmx5dXA2N2Zwa25qMmFobXZmMmw5ZjVlb2N4djI5Z2FlOHZ2eiZlcD12MV9pbnRlcm5hbF9naWZfYnlfaWQmY3Q9Zw/6BGc1mKM21dxaq5QrF/giphy.gif) | ![](https://media1.giphy.com/media/v1.Y2lkPTc5MGI3NjExOHg5N2dlOXF0c2U5aDlvYTh4YnNrY3lqNjBtZDhubnBueGVqZHRidCZlcD12MV9pbnRlcm5hbF9naWZfYnlfaWQmY3Q9Zw/aqntn5A20nx9fT6tnR/giphy.gif) |

## Setup and Installation

### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
- [Gemini API Key](https://aistudio.google.com/apikey)
- [Brave Search API Key (Data for AI)](https://brave.com/search/api)
- [JDK 11 or above](https://www.oracle.com/java/technologies/downloads/)

### .NET MAUI SDK Installation (Skip if Already Installed)
To install .NET MAUI SDK, run the following command:
```sh
> dotnet workload install maui
```

### Cloning the project
You can either download the project or run the following command:
```sh
> git clone https://github.com/MemExplorer/Geco.git
```
After cloning, the project folder name should be `Geco`. If you downloaded it directly from github, the folder name should be `Geco-main`. Navigate to the project directory by running the following command.
```sh
> cd Geco
```

### Assets
The font used in the application is not publicly available. You may visit the font's website for more information by clicking [here](https://fontawesome.com/). The font should be placed inside `Geco/Resources/Fonts`.

### Replace API Keys
Before compiling, create a file called `GecoSecrets.cs`. Place the file in the GECO folder that is inside the solution directory. The Contents of the file should look like this:
```cs
namespace Geco;

internal partial class GecoSecrets
{
    internal static partial string GEMINI_API_KEY => "Your Gemini API key";
    internal static partial string BRAVE_SEARCH_API_KEY => "Your Brave API Key";
}
```

### Building
Before proceeding, replace `"Your JDK folder path"` with your actual JDK installation path. Ensure the path is enclosed in double quotes.  

#### 1. Installing Android SDK (Skip if Already Installed)  

If you haven't installed the Android SDK, you need to do so before building the app. Run the following command:  

```sh
> dotnet build -c Release -t:InstallAndroidDependencies -p:AndroidSdkDirectory="C:\android\android-sdk" -p:JavaSdkDirectory="Your JDK folder path" -p:AcceptAndroidSdkLicenses=True
```  

After execution, an error may appear. You can safely ignore it and proceed to the next step.  

#### 2. Building the App  

If the Android SDK is already installed, update the command with your actual Android SDK path. Then, run:  

```sh
> dotnet build -c Release -p:AndroidSdkDirectory="C:\android\android-sdk" -p:JavaSdkDirectory="Your JDK folder path"
```  

Once the build is complete, the generated APK file will be located at `Geco/bin/Release/net9.0-android/com.ssbois.geco-Signed.apk`.
