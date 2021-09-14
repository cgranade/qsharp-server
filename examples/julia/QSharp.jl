module QSharp
    using JSON
    using UUIDs

    struct Message
        context :: Union{String, Nothing}
        data :: Any
    end

    mutable struct QSharpServer
        Server :: Base.Process
        MessageBuffer :: Vector{Message}

        QSharpServer(cmd :: Cmd) = begin
            new(
                open(cmd, read = true, write = true),
                Vector{Dict{Any}}()
            )
        end
    end

    # Internal methods

    function start() :: QSharpServer
        QSharpServer(`dotnet run --no-build --project ../../qsharp-server/src/qsharp-server.csproj`)
    end

    function send_command(server :: QSharpServer, command :: String, payload :: Any = Dict()) :: String
        context = string(UUIDs.uuid4())
        JSON.print(server.Server, Dict(
            :command => command,
            :payload => payload,
            :context => context
        ))
        write(server.Server, "\n")
        context
    end

    function stop(server :: QSharpServer)
        send_command(server, "exit")
    end

    # TODO: Make async!
    function readnext(server :: QSharpServer, filter)
        next_msg_json = JSON.parse(server.Server)
        context = haskey(next_msg_json, "context") ? pop!(next_msg_json, "context") :  nothing
        next_msg = Message(context, next_msg_json)
        if filter(next_msg)
            next_msg
        else
            push!(server.MessageBuffer, next_msg)
            nothing
        end
    end

    # TODO: Make async!
    function listen(server :: QSharpServer, context :: Union{String, Nothing})
        filter = if isnothing(context)
            msg -> true
        else
            msg -> msg.context == something(context)
        end
        # Do we already have a message in the buffer?
        idx_msg = findfirst(filter, server.MessageBuffer)
        if isnothing(idx_msg)
            # Need to read the next one.
            while true
                next = readnext(server, filter)
                if !isnothing(next)
                    return next.data
                end
            end
        else
            return popat!(server.MessageBuffer, idx_msg).data
        end


    end

    struct Callable
        full_name :: String
    end

    # TODO: allow passing input
    function invoke(callable :: Callable; simulator :: Union{String, Nothing} = nothing)
        global server
        context = send_command(server, "simulate", Dict(
            "operation" => callable.full_name,
            "input" => Dict(),
            "simulator" => simulator
        ))
        while true
            next_msg = listen(server, context)
            if haskey(next_msg, "data") && haskey(next_msg["data"], "output")
                # Actual output, so we can return.
                return next_msg["data"]["output"]
            elseif haskey(next_msg, "display_output")
                # TODO: Process as Jupyter display!
                println("Displayable output: $(next_msg["display_output"])")
            elseif haskey(next_msg, "console_message")
                stream = get(next_msg, "stream", "StandardOut")
                if stream == "StandardOut"
                    println(next_msg["console_message"])
                elseif stream == "StandardError"
                    println(stderr, next_msg["console_message"])
                else
                    println("Unrecognized stream $stream.")
                end
            else
                println("Unrecognized message $next_msg")
            end
        end
    end

    # Public API

    server = start()

    function compile(code :: String)
        global server
        context = send_command(server, "compile", Dict(
            :source => code
        ))
        response = listen(server, context)
        # TODO: Handle warnings
        [Callable(callable_name) for callable_name in response["data"]["compiled_callables"]]
    end

end
