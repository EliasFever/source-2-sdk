# Source 2 SDK

![New Editor](https://media.discordapp.net/attachments/1450076545153765512/1473649233323102354/sbox-dev_AzDk0jdc8f.png?ex=6996fa4c&is=6995a8cc&hm=7884a1742e83e337361cf01a1504b49ca8dbaeebad168202efe3c23ec771beaa&=&format=webp&quality=lossless&width=1645&height=864)

## What.

Source 2 SDK is a fork of Facepunch's Source 2 engine, available [here](https://github.com/Facepunch/sbox-public). This is not affiliated with Valve in anyway (besides being a fork of a fork of a Half-Life: Alyx engine), and is simply a joke name. 

This fork is used by QuantumStop (title pending), giving it modifications we require while developing our games.

## Getting the Engine
### Compiling from Source

If you want to build from source, this repository includes all the necessary files to compile the engine yourself.

#### Prerequisites

* [Git](https://git-scm.com/install/windows)
* [Visual Studio 2026](https://visualstudio.microsoft.com/)
* [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download)

#### Building

```bash
# Clone the repo
git clone https://github.com/EliasFever/source-2-sdk.git
```
Once you've cloned the repo simply run `Bootstrap.bat` which will download dependencies and build the engine.
The game and editor can be run from the binaries in the game folder.


## License

The s&box engine source code is licensed under the [MIT License](LICENSE.md).

Certain native binaries in `game/bin` are not covered by the MIT license. These binaries are distributed under the s&box EULA. You must agree to the terms of the EULA to use them.

This project includes third-party components that are separately licensed.
Those components are not covered by the MIT license above and remain subject
to their original licenses as indicated in `game/thirdpartylegalnotices`.
