# Technical instructions

## Contributing

[Pull requests](https://github.com/HebaruSan/Astrogator/pulls) are welcome!

## Installation

### Manual

Unpack the [zip file](https://github.com/HebaruSan/Astrogator/releases) in your GameData folder.

### CKAN

```sh
mono ckan install Astrogator
```

## Compiling

### Linux (and maybe MacOS)

```sh
git clone git@github.com:HebaruSan/Astrogator.git
cd Astrogator
ln -s /path/to/KSP/KSP_x64_Data src
make
```

If you have KSP installed via Steam, you may be able to skip the `ln -s` step, as the Makefile attempts to find it in the standard location.

### Windows

I assume opening the `csproj` file in Visual Studio would work, but I haven't tried it.

## Packing a release

1. Commit all changes intended for release
1. Update [TODO list](TODO.md) to reflect changes
1. Update version in [AVC file](Astrogator.version)
1. Update version in [DLL assembly](src/Properties/AssemblyInfo.cs)
1. Commit and push to Github
1. Create a new release on Github
1. `make clean && make`
1. Attach zip file to Github release
1. Release the release
1. Watch CKAN for updates
