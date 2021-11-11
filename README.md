# Embedding Q\# in other host languages

This repository demonstrates how to use libraries provided with the Quantum Development Kit to embed Q# into other host languages, such as Julia, PowerShell, and even Lua. To do so, IQ# as a library to implement an interoperability server similar to the IQ# kernel for Jupyter, but that is easier to call from languages without existing Jupyter clients.

> **NOTE**: This repository is meant as a demonstration of the relevant techniques _only_, and should not be used in production.

The code in this repository is split into several different components:

- [**command-stream**](./command-stream): A .NET library for writing lightweight JSON-based servers over command-line or TCP/IP.
- [**qsharp-server**](./qsharp-server): A .NET application that uses **command-stream** to provide interoperability services to different languages, using IQ# as a library to provide access to the Q# compiler and runtime.
- [**examples/julia**](./examples/julia): A small Julia library that calls into **qsharp-server** to provide Q# + Julia interoperability, and an example of using this library to deploy a Q# application to the open systems simulator from Julia.
- [**examples/powershell**](./examples/julia): A small PowerShell library that calls into **qsharp-server** to provide Q# + Julia interoperability, and an example of using this library to deploy a Q# application to the full-state simulator from PowerShell.
- [**examples/lua**](./examples/lua): A small Lua library that calls into **qsharp-server** to provide Q# + Lua interoperability, and an example of using this library to deploy a Q# application to the full-state and open-systems simulators from Lua, and capturing diagnostics into Lua objects.

# Installing

This repository has a devcontainer configuration designed for use with Visual Studio Code devcontainers, or with GitHub Codespaces. For more information, check out https://code.visualstudio.com/docs/remote/create-dev-container.

## Manual installation (Julia)

The `QSharp.jl` package in this repository depends on two Julia libraries. To install them, run:

```julia
using Pkg
Pkg.add("IJulia")
Pkg.add("JSON")
```

## Manual installation (Lua)

**NB: Lua + Q# integration is not currently supported on Windows.**

To use Lua and Q# together using the Anaconda distribution, create an environment that contains the `luarocks` package manager, and install required Lua dependencies:

```
conda create -n qsharp-lua -c conda-forge xeus-lua notebook luarocks
conda activate qsharp-lua
luarocks install f-strings
luarocks install luasocket
luarocks install moonjson
luarocks install uuid
luarocks install luaunit
```
