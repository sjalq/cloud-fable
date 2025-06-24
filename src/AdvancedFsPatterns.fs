module AdvancedFSharpPatterns

open Fable.Core
open Fable.Core.JsInterop
open System

// =============================================================================
// COMPUTATION EXPRESSIONS for Request Pipeline
// =============================================================================

type RequestResult<'T> = 
    | Success of 'T
    | ValidationError of string list
    | NotFound of string
    | Unauthorized of string

type RequestBuilder() =
    member _.Return(x) = Success x
    member _.ReturnFrom(x) = x
    
    member _.Bind(result, f) =
        match result with
        | Success x -> f x
        | ValidationError errs -> ValidationError errs
        | NotFound msg -> NotFound msg
        | Unauthorized msg -> Unauthorized msg

let request = RequestBuilder()

// =============================================================================
// RAILWAY-ORIENTED PROGRAMMING
// =============================================================================

module Validation =
    let validateRequired field value =
        if String.IsNullOrWhiteSpace(value) then
            ValidationError [$"{field} is required"]
        else 
            Success value

    let validateEmail email =
        if email.Contains("@") then Success email
        else ValidationError ["Invalid email format"]

    let validatePositive field value =
        if value <= 0 then ValidationError [$"{field} must be positive"]
        else Success value

    let combine results =
        let errors = 
            results 
            |> List.choose (function ValidationError errs -> Some errs | _ -> None)
            |> List.concat
        
        if List.isEmpty errors then
            results |> List.choose (function Success x -> Some x | _ -> None) |> Success
        else
            ValidationError errors

// =============================================================================
// ACTIVE PATTERNS for URL Parsing
// =============================================================================

let (|Int|_|) (str: string) =
    match System.Int32.TryParse(str) with
    | true, i -> Some i
    | false, _ -> None

let (|Guid|_|) (str: string) =
    match System.Guid.TryParse(str) with
    | true, g -> Some g
    | false, _ -> None

let (|Email|_|) (str: string) =
    if str.Contains("@") then Some str else None

// =============================================================================
// FUNCTION COMPOSITION for Middleware
// =============================================================================

type HttpContext = {
    Request: HttpRequest
    User: User option
    Claims: Map<string, string>
    StartTime: DateTime
}

type Middleware = HttpContext -> RequestResult<HttpContext>

module Middleware =
    let logging : Middleware = fun ctx ->
        printfn $"[{DateTime.UtcNow}] {ctx.Request.Method} {ctx.Request.Path}"
        Success ctx

    let timing : Middleware = fun ctx ->
        Success { ctx with StartTime = DateTime.UtcNow }

    let cors : Middleware = fun ctx ->
        // Add CORS headers logic here
        Success ctx

    let authenticate : Middleware = fun ctx ->
        // Simple auth check - in real app, validate JWT/token
        let authHeader = ctx.Request.RawRequest?headers?authorization
        if isNull authHeader then
            Unauthorized "No authorization header"
        else
            let user = { Id = 1; Name = "Authenticated User"; Email = "user@example.com" }
            Success { ctx with User = Some user }

    let compose (middlewares: Middleware list) : Middleware =
        fun ctx ->
            let rec apply middlewares ctx =
                match middlewares with
                | [] -> Success ctx
                | middleware :: rest ->
                    request {
                        let! newCtx = middleware ctx
                        return! apply rest newCtx
                    }
            apply middlewares ctx

// =============================================================================
// TYPE-SAFE ROUTING with Discriminated Unions
// =============================================================================

type ApiRoute =
    | Home
    | Health  
    | Users of UsersRoute
    | Products of ProductsRoute
    | NotFound

and UsersRoute =
    | AllUsers
    | UserById of int
    | CreateUser
    | UpdateUser of int

and ProductsRoute = 
    | AllProducts
    | ProductById of int
    | SearchProducts of string

module RouteParser =
    let parse (method: HttpMethod) (segments: string list) : ApiRoute =
        match method, segments with
        | GET, [] -> Home
        | GET, ["health"] -> Health
        
        // Users
        | GET, ["api"; "users"] -> Users AllUsers
        | GET, ["api"; "users"; Int id] -> Users (UserById id)
        | POST, ["api"; "users"] -> Users CreateUser
        | PUT, ["api"; "users"; Int id] -> Users (UpdateUser id)
        
        // Products  
        | GET, ["api"; "products"] -> Products AllProducts
        | GET, ["api"; "products"; Int id] -> Products (ProductById id)
        | GET, ["api"; "products"; "search"; query] -> Products (SearchProducts query)
        
        | _ -> NotFound

// =============================================================================
// HANDLERS with Beautiful F# Async
// =============================================================================

type Handler = HttpContext -> Async<obj>

module Handlers =
    let home : Handler = fun ctx -> async {
        return Response.json """{"message":"Welcome to F# API!"}"""
    }

    let health : Handler = fun ctx -> async {
        let uptime = DateTime.UtcNow - ctx.StartTime
        let health = {| 
            status = "healthy"
            uptime = uptime.TotalSeconds
            timestamp = DateTime.UtcNow
            user = ctx.User |> Option.map (fun u -> u.Name)
        |}
        return Response.json (Json.serialize health)
    }

    let getAllUsers : Handler = fun ctx -> async {
        // Simulate async database call
        do! Async.Sleep(10) 
        
        return request {
            let! users = Users.getAll()
            return Response.json (Json.serialize users)
        } |> function
            | Success response -> response
            | ValidationError errs -> Response.badRequest (Json.serialize {| errors = errs |})
            | NotFound msg -> Response.notFound (Json.serialize {| error = msg |})
            | Unauthorized msg -> Response.create msg 401 Json Map.empty
    }

    let getUserById (id: int) : Handler = fun ctx -> async {
        return request {
            let! user = Users.getById(id)
            return Response.json (Json.serialize user)
        } |> function
            | Success response -> response
            | other -> Response.fromApiResult other
    }

    let createUser : Handler = fun ctx -> async {
        // In real app, get body from request
        let! bodyText = ctx.Request.RawRequest?text() |> Async.AwaitPromise
        
        return request {
            // Validate input
            let! validatedName = Validation.validateRequired "name" "Alice"
            let! validatedEmail = Validation.validateEmail "alice@test.com"
            
            // Create user logic here
            let newUser = { Id = 99; Name = validatedName; Email = validatedEmail }
            return Response.json (Json.serialize newUser)
        } |> function
            | Success response -> response
            | ValidationError errs -> Response.badRequest (Json.serialize {| errors = errs |})
            | other -> Response.fromApiResult other
    }

// =============================================================================
// BEAUTIFUL COMPOSITION: Putting It All Together
// =============================================================================

let routeToHandler : ApiRoute -> Handler = function
    | Home -> Handlers.home
    | Health -> Handlers.health
    | Users AllUsers -> Handlers.getAllUsers
    | Users (UserById id) -> Handlers.getUserById id
    | Users CreateUser -> Handlers.createUser
    | Users (UpdateUser id) -> fun ctx -> async { return Response.ok "Update not implemented" }
    | Products _ -> fun ctx -> async { return Response.ok "Products not implemented" }
    | NotFound -> fun ctx -> async { return Response.notFound """{"error":"Route not found"}""" }

let pipeline = 
    Middleware.compose [
        Middleware.logging
        Middleware.timing
        Middleware.cors
        // Middleware.authenticate  // Uncomment for protected routes
    ]

let handleRequest (rawRequest: obj) : JS.Promise<obj> =
    async {
        // Parse request
        let request = Request.fromRaw rawRequest
        let route = RouteParser.parse request.Method (Router.splitPath request.Path)
        
        // Create context
        let baseContext = {
            Request = request
            User = None
            Claims = Map.empty
            StartTime = DateTime.UtcNow
        }
        
        // Apply middleware pipeline
        match pipeline baseContext with
        | Success ctx ->
            // Route to handler
            let handler = routeToHandler route
            let! response = handler ctx
            return response
            
        | ValidationError errs ->
            return Response.badRequest (Json.serialize {| errors = errs |})
        | NotFound msg ->
            return Response.notFound (Json.serialize {| error = msg |})
        | Unauthorized msg ->
            return Response.create msg 401 Json Map.empty
            
    } |> Async.StartAsPromise

// Wire it up (still only one ugly line!)
addEventListener (fun event ->
    let responsePromise = handleRequest event?request
    event?respondWith(responsePromise) |> ignore
)
