# YAudio

[![GitHub](https://img.shields.io/github/license/naudio/NAudio)](https://github.com/naudio/NAudio/blob/master/license.txt) [![Nuget](https://img.shields.io/nuget/v/YAudio)](https://www.nuget.org/packages/YAudio/)

> **これはNAudioのフォークです。**  
> YAudioは[NAudio](https://github.com/naudio/NAudio)をベースに、.NET 10対応とパッケージ名の変更を行ったフォーク版です。  
> 本家NAudioは[Mark Heath](https://markheath.net)によって開発されたオープンソースの.NETオーディオライブラリです。

YAudio is a fork of NAudio, an open source .NET audio library originally written by [Mark Heath](https://markheath.net)

## Features

YAudio provides the same comprehensive audio functionality as NAudio, including playback, recording, file format support, audio manipulation, MIDI support, and more. For detailed feature information, please refer to the [original NAudio README](https://github.com/naudio/NAudio/blob/master/README.md).

**YAudio-specific features:**
* .NET 10 support
  * Updated to target .NET 10.0 (net10.0 and net10.0-windows)
  * Windows x64 runtime support

## Getting Started

The easiest way to install YAudio into your project is to install the latest [YAudio NuGet package](https://www.nuget.org/packages/YAudio/).

### Installation

**Package Manager:**
```
Install-Package YAudio
```

**dotnet CLI:**
```
dotnet add package YAudio
```

**PackageReference:**
```xml
<PackageReference Include="YAudio" Version="1.0.6" />
```

## Documentation

For tutorials, examples, and detailed documentation, please refer to the [original NAudio documentation](https://github.com/naudio/NAudio/blob/master/README.md). The API is identical to NAudio, so all NAudio documentation and examples apply to YAudio as well.

## FAQ

**What is YAudio?**

YAudio is a fork of NAudio, an open source audio API for .NET originally written in C# by Mark Heath, with contributions from many other developers. YAudio provides .NET 10 support and maintains compatibility with the original NAudio API. It is intended to provide a comprehensive set of useful utility classes from which you can construct your own audio application.

**What's the difference between YAudio and NAudio?**

YAudio is a fork of NAudio that has been updated to target .NET 10.0. The package name has been changed to YAudio to distinguish it from the original NAudio package. All functionality and APIs remain the same, so you can use YAudio as a drop-in replacement for NAudio in .NET 10 projects.

**How can I get help?**

Since YAudio is a fork of NAudio, most of the documentation and community resources for NAudio apply to YAudio as well. You can:

1. Raise an issue on this GitHub repository for YAudio-specific issues or .NET 10 compatibility questions.
2. Ask on StackOverflow and [tag your question with naudio](http://stackoverflow.com/questions/tagged/naudio) - the API is the same, so NAudio answers will work for YAudio too.
3. For original NAudio support, you can contact [Mark Heath](https://markheath.net) directly.

**How do I submit a patch?**

Contributions to YAudio are welcome. Since YAudio is a fork of NAudio, please keep the following guidelines in mind:

* Your submission must be your own work, and able to be released under the MIT license.
* You will need to make sure your code conforms to the layout and naming conventions used elsewhere in the codebase.
* Remember that there are many existing users of NAudio/YAudio. A patch that changes the public interface is not likely to be accepted unless it's necessary for .NET 10 compatibility.
* Try to write "clean code" - avoid long functions and long classes. Try to add a new feature by creating a new class rather than putting loads of extra code inside an existing one.
* Please write unit tests (using NUnit) if at all possible. If not, give a clear explanation of how your feature can be unit tested and provide test data if appropriate. Tell us what you did to test it yourself, including what operating systems and soundcards you used.
* If you are adding a new feature, please consider writing a short tutorial on how to use it.
* Unless your patch is a small bugfix, it will be code reviewed and you will need to be willing to make the recommended changes before it can be integrated.
* Patches should be provided using the Pull Request feature of GitHub.
* Please also bear in mind that when you add a feature to YAudio, that feature will generate future support requests and bug reports. Are you willing to stick around and help out people using it?

For more FAQ and information, please refer to the [original NAudio README](https://github.com/naudio/NAudio/blob/master/README.md).
