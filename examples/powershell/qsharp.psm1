
# Private commands

[System.Diagnostics.Process]
$script:serverProcess = $null;
$script:buffer = New-Object 'System.Collections.Generic.List[object]]';

function Start-QSharpServer() {
    $startInfo =  New-Object -TypeName System.Diagnostics.ProcessStartInfo;
    $startInfo.FileName = Get-Command dotnet;
    $startInfo.ArgumentList.Add("run");
    $startInfo.ArgumentList.Add("--no-build");
    $startInfo.ArgumentList.Add("--project");
    $startInfo.ArgumentList.Add((Join-Path $PSScriptRoot "../../qsharp-server/src/qsharp-server.csproj"));
    $startInfo.RedirectStandardInput = $true;
    $startInfo.RedirectStandardOutput = $true;

    $script:serverProcess = [System.Diagnostics.Process]::Start($startInfo);
    $script:serverProcess | Write-Output
}

function Stop-QSharpServer() {
    Stop-Process $script:serverProcess;
}

function Require-QSharpServer() {
    # TODO: check if still running.
    if ($null -eq $script:serverProcess) {
        Start-QSharpServer | Write-Verbose
    }
}

function Write-Command() {
    param(
        [Parameter(Mandatory=$true)]
        [string]
        $Command,

        [hashtable]
        $Payload = @{}
    );

    $context = [System.Guid]::NewGuid().Guid;

    $json = @{
        "command" = $Command;
        "payload" = $Payload;
        "context" = $context;
    } | ConvertTo-Json -Compress;
    Write-Verbose "[PS → Q#] $json"
    $script:serverProcess.StandardInput.WriteLine($json);
}

function _FilterForContext() {
    param(
        [string]
        $Context
    )

    return ("$Context" -eq "") `
           ? { param($MsgContext) $true } `
           : { param($MsgContext) $MsgContext -eq $Context };
}

function Read-NextMessage() {
    param(
        [string]
        $Context
    );

    $filter = _FilterForContext $Context;
    $nextMessage = $script:serverProcess.StandardOutput.ReadLine();
    Write-Verbose "[PS ← Q#] $nextMessage";
    $nextMessage = $nextMessage | ConvertFrom-Json;
    if ($filter.Invoke($nextMessage.Context)) {
        return $nextMessage;
    } else {
        $script:buffer.Add($nextMessage);
    }
}

function Read-Response() {
    param(
        [string]
        $Context
    );

    $filter = _FilterForContext $Context;
    foreach ($item in $script:buffer) {
        if ($filter.Invoke($item.Context)) {
            $script:buffer.Remove($item);
            return $item;
        }
    }

    # No messages on the buffer yet.
    while ($true) {
        $nextMessage = Read-NextMessage -Context $Context;
        if ($null -ne $nextMessage) {
            return $nextMessage;
        }
    }
    
}

function Build-QuantumProgram() {
    param(
        [string]
        $Source
    );
    Require-QSharpServer

    $context = Write-Command `
        -Command "compile" `
        -Payload @{
            "source" = $Source
        };
    $response = Read-Response -Context $context;
    Write-Verbose "Compile response: $response"
    foreach ($warning in $response.warnings) {
        Write-Warning $warning;
    }
    $response.compiled_callables | Write-Output;
}

function Invoke-QuantumProgram() {
    param(
        [string]
        $CallableName,
        [string]
        $Simulator = $null
        <# TODO: allow input #>
    );
    Require-QSharpServer

    $context = Write-Command `
        -Command "simulate" `
        -Payload @{
            "operation" = $CallableName;
            "input" = @{};
            "simulator" = $Simulator
        };
    while ($true) {
        $nextMsg = Read-Response -Context $context;
        $nextMsg | Write-Verbose
        if ($null -ne $nextMsg.data -and $null -ne $nextMsg.data.output) {
            return $nextMsg.data.output;
        } elseif ($null -ne $nextMsg.display_output) {
            Write-Host "Displayable output: ${$nextMsg.display_output}"
        } elseif ($null -ne $nextMsg.console_message) {
            if ($null -eq $nextMsg.stream -or $nextMsg.stream -eq "StandardOut") {
                Write-Host $nextMsg.console_message;
            } elseif ($nextMsg.stream -eq "StandardError") {
                Write-Error $nextMsg.console_message;
            } else {
                Write-Warning "Unrcognized stream ${$nextMsg.stream}."
            }
        } else {
            Write-Warning "Unrcognized message ${$nextMsg}."
        }
    }
}