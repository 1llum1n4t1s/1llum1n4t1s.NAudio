# 1llum1n4t1s.NAudio

[![GitHub](https://img.shields.io/github/license/naudio/NAudio)](https://github.com/naudio/NAudio/blob/master/license.txt) [![Nuget](https://img.shields.io/nuget/v/1llum1n4t1s.NAudio)](https://www.nuget.org/packages/1llum1n4t1s.NAudio/)

> **これはNAudioのフォークです。**  
> 1llum1n4t1s.NAudioは[NAudio](https://github.com/naudio/NAudio)をベースに、.NET 10対応、パッケージ名の変更、および本家でマージされていなかったプロセスループバックキャプチャの実装の追加を行ったフォーク版です。  
> また、多数のバグ修正を含んでいるため、本家と動作が異なる場合があります。  
> 本家NAudioは[Mark Heath](https://markheath.net)によって開発されたオープンソースの.NETオーディオライブラリです。

1llum1n4t1s.NAudio is a fork of NAudio, an open source .NET audio library originally written by [Mark Heath](https://markheath.net)

## Features

1llum1n4t1s.NAudio provides the same comprehensive audio functionality as NAudio, including playback, recording, file format support, audio manipulation, MIDI support, and more. For detailed feature information, please refer to the [original NAudio README](https://github.com/naudio/NAudio/blob/master/README.md).

**1llum1n4t1s.NAudio-specific features:**
* .NET 10 support
  * Updated to target .NET 10.0 (net10.0 and net10.0-windows)
  * Windows x64 runtime support
* Process Loopback Capture
  * Added implementation for capturing audio from specific processes, which was not merged in the original NAudio.
  * See [ProcessLoopbackCapture.md](Docs/ProcessLoopbackCapture.md) for details.

## Getting Started

The easiest way to install 1llum1n4t1s.NAudio into your project is to install the latest [1llum1n4t1s.NAudio NuGet package](https://www.nuget.org/packages/1llum1n4t1s.NAudio/).

### Installation

**Package Manager:**
```
Install-Package 1llum1n4t1s.NAudio
```

**dotnet CLI:**
```
dotnet add package 1llum1n4t1s.NAudio
```

**PackageReference:**
```xml
<PackageReference Include="1llum1n4t1s.NAudio" Version="1.0.30" />
```

## Documentation

For tutorials, examples, and detailed documentation, please refer to the [original NAudio documentation](https://github.com/naudio/NAudio/blob/master/README.md). The API is identical to NAudio, so all NAudio documentation and examples apply to 1llum1n4t1s.NAudio as well.

## FAQ

**What is 1llum1n4t1s.NAudio?**

1llum1n4t1s.NAudio is a fork of NAudio, an open source audio API for .NET originally written in C# by Mark Heath, with contributions from many other developers. 1llum1n4t1s.NAudio provides .NET 10 support and maintains compatibility with the original NAudio API. It is intended to provide a comprehensive set of useful utility classes from which you can construct your own audio application.

**What's the difference between 1llum1n4t1s.NAudio and NAudio?**

1llum1n4t1s.NAudio is a fork of NAudio that has been updated to target .NET 10.0. The package name has been changed to 1llum1n4t1s.NAudio to distinguish it from the original NAudio package. All functionality and APIs remain the same, so you can use 1llum1n4t1s.NAudio as a drop-in replacement for NAudio in .NET 10 projects.

**How can I get help?**

Since 1llum1n4t1s.NAudio is a fork of NAudio, most of the documentation and community resources for NAudio apply to 1llum1n4t1s.NAudio as well. You can:

1. Raise an issue on this GitHub repository for 1llum1n4t1s.NAudio-specific issues or .NET 10 compatibility questions.
2. Ask on StackOverflow and [tag your question with naudio](http://stackoverflow.com/questions/tagged/naudio) - the API is the same, so NAudio answers will work for 1llum1n4t1s.NAudio too.
3. For original NAudio support, you can contact [Mark Heath](https://markheath.net) directly.

**How do I submit a patch?**

Contributions to 1llum1n4t1s.NAudio are welcome. Since 1llum1n4t1s.NAudio is a fork of NAudio, please keep the following guidelines in mind:

* Your submission must be your own work, and able to be released under the MIT license.
* You will need to make sure your code conforms to the layout and naming conventions used elsewhere in the codebase.
* Remember that there are many existing users of NAudio/1llum1n4t1s.NAudio. A patch that changes the public interface is not likely to be accepted unless it's necessary for .NET 10 compatibility.
* Try to write "clean code" - avoid long functions and long classes. Try to add a new feature by creating a new class rather than putting loads of extra code inside an existing one.
* Please write unit tests (using NUnit) if at all possible. If not, give a clear explanation of how your feature can be unit tested and provide test data if appropriate. Tell us what you did to test it yourself, including what operating systems and soundcards you used.
* If you are adding a new feature, please consider writing a short tutorial on how to use it.
* Unless your patch is a small bugfix, it will be code reviewed and you will need to be willing to make the recommended changes before it can be integrated.
* Patches should be provided using the Pull Request feature of GitHub.
* Please also bear in mind that when you add a feature to 1llum1n4t1s.NAudio, that feature will generate future support requests and bug reports. Are you willing to stick around and help out people using it?

For more FAQ and information, please refer to the [original NAudio README](https://github.com/naudio/NAudio/blob/master/README.md).
