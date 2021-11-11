FROM continuumio/miniconda3:latest

# .NET 6
RUN apt-get update && \
    apt-get -y upgrade \
                       libsasl2-2 && \
    apt-get -y install \
                       wget \
                       pgp \
                       vim \
                       # Needed by luasocket and luasec
                       gcc libssl-dev \
                       apt-transport-https && \
    apt-get clean && rm -rf /var/lib/apt/lists/

RUN wget -qO- https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.asc.gpg && \
    mv microsoft.asc.gpg /etc/apt/trusted.gpg.d/ && \
    wget -q https://packages.microsoft.com/config/debian/9/prod.list && \
    mv prod.list /etc/apt/sources.list.d/microsoft-prod.list && \
    chown root:root /etc/apt/trusted.gpg.d/microsoft.asc.gpg && \
    chown root:root /etc/apt/sources.list.d/microsoft-prod.list && \
    apt-get -y update && \
    apt-get -y install dotnet-sdk-3.1 dotnet-sdk-5.0 dotnet-sdk-6.0 && \
    apt-get clean && rm -rf /var/lib/apt/lists/

RUN conda install --yes \
    -c conda-forge \
    # Jupyter dependencies
    notebook jupyterlab \
    # Lua dependencies 
    xeus-lua luarocks

# Additional .NET and PowerShell tooling
RUN dotnet tool install -g PowerShell && \
    dotnet tool install -g \
        --add-source "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json" \
        Microsoft.dotnet-interactive && \
    # Call using explicit path, since tools aren't on PATH yet.
    ${HOME}/.dotnet/tools/dotnet-interactive jupyter install

# We install Julia to avoid bugs in the Debian and conda-forge packages.
RUN wget https://raw.githubusercontent.com/abelsiqueira/jill/master/jill.sh && \
    bash jill.sh --yes && \
    rm jill.sh && \
    julia -E "using Pkg; Pkg.add(\"IJulia\"); Pkg.add(\"JSON\")"

# Additional Lua tooling
RUN luarocks install luasec && \
    luarocks install f-strings && \
    luarocks install luasocket && \
    luarocks install moonjson && \
    luarocks install uuid && \
    luarocks install luaunit
