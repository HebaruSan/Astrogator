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
2. Update [TODO list](TODO.md) to reflect changes
3. Update version in [AVC file](Astrogator.version)
4. Update version in [DLL assembly](src/Properties/AssemblyInfo.cs)
5. `make clean && make`
6. Take screenshots
7. Commit and push to Github
8. Create a new release on Github
9. Attach zip file to Github release
10. Publish the release
11. Watch CKAN for updates
