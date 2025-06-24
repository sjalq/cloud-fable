module Worker

open Fable.Core
open Fable.Core.JsInterop
open System

// Manual JS interop - the proven approach that works
[<Emit("addEventListener('fetch', $0)")>]
let addEventListener (handler: obj -> unit) : unit = jsNative

[<Emit("new Response($0, {status: $1})")>]
let newResponse (body: string) (status: int) : obj = jsNative

[<Emit("new Response($0, {status: $1, headers: $2})")>]
let newResponseWithHeaders (body: string) (status: int) (headers: obj) : obj = jsNative

// Request helper to extract URL path
[<Emit("(new URL($0)).pathname")>]
let getPath (url: string) : string = jsNative

// Simple routing function
let handleRequest (request: obj) =
    let url = request?url |> unbox<string>
    let method = request?method |> unbox<string>
    let path = getPath url
    
    match method, path with
    | "GET", "/" ->
        newResponse "Hello from F# Cloudflare Worker!" 200
    | "GET", "/health" ->
        let timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
        let body = sprintf """{"status":"healthy","timestamp":"%s"}""" timestamp
        let headers = createObj [ "content-type" ==> "application/json" ]
        newResponseWithHeaders body 200 headers
    | "GET", "/api/hello" ->
        newResponse "F# + Cloudflare = Success!" 200
    | _ ->
        let body = """{"message":"Not Found"}"""
        let headers = createObj [ "content-type" ==> "application/json" ]
        newResponseWithHeaders body 404 headers

// Register the fetch event listener
addEventListener (fun event ->
    let request = event?request
    let response = handleRequest request
    event?respondWith(response) |> ignore
) 