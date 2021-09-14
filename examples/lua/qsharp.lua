local qsharp = {}

-- require packages from luarocks
local socket = require "socket"
local f = require "F"
local json = require "json"
local uuid = require "uuid"
local lu = require "luaunit"

local process = nil
local client = nil

function qsharp.send_command(command, payload)
    context = uuid.new()
    local encoded = json.encode({
        command = command,
        payload = payload,
        context = context
    })
    client:send(encoded .. "\n")
    return context
end

function qsharp.exit(self)
    qsharp.send_command("exit", {})
end

function qsharp.start(port)
    host = "*"
    port = port or 8005
    s = assert(socket.bind(host, port))

    cmd = f"dotnet run --no-build --project ../../qsharp-server/src/qsharp-server.csproj -- --port {port} --use tcpip --log-file qsharp-lua.json --log-json"
    server = io.popen(cmd)

    connection = s:accept()

    process = server
    client = connection
end

-- TODO: Implement buffer logic, context filtering from other language frontends.
function qsharp.listen(context)
    local line = client:receive("*l")
    return json.decode(line)
end

function qsharp.compile(code)
    local context = qsharp.send_command("compile", {
        source = code
    })
    -- TODO: check for errors, warnings
    return qsharp.listen(context).data.compiled_callables
end

function qsharp.invoke(callable, simulator, input)
    simulator = simulator or "QuantumSimulator"
    input = input or {}
    local context = qsharp.send_command("simulate", {
        operation = callable,
        input = input,
        simulator = simulator
    })
    while true
    do
        local next_msg = qsharp.listen(context)
        if next_msg.data ~= nil and next_msg.data.output ~= nil then
            return next_msg.data.output
        elseif next_msg.display_output ~= nil then
            local disp = lu.prettystr(next_msg.display_output)
            print(f"Displayable output: {disp}")
        elseif next_msg.console_message ~= nil then
            if next_msg.stream == "StandardOut" then
                print(next_msg.console_message)
            elseif next_msg.stream == "StandardError" then
                io.stderr:write(next_msg.console_message)
            else
                print(f"Unrecognized stream {next_msg.stream}.")
            end
        else
            print(f"Unrecognized message {next_msg}.")
        end
    end
end

return qsharp