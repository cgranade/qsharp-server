# qsharp.lua

## Installing

(NB: Windows is not yet supported due to the dependency on luarocks.)

```
conda create -n qsharp-lua -c conda-forge xeus-lua notebook luarocks
conda activate qsharp-lua
luarocks install f-strings
luarocks install luasocket
luarocks install moonjson
luarocks install uuid
luarocks install luaunit
```
