To compile into a library:

In src folder:
```
mcs /target:library /out:../bin/ndn-iot-dot-net.dll -r:../bin/ndn-dot-net.dll bootstrap/*.cs consumer/*.cs discovery/*.cs ../contrib/*.cs
```

To work with the compiled binary, in examples folder
```
mcs -r:../bin/ndn-dot-net.dll -r:../bin/ndn-iot-dot-net.dll [example.cs]
```