module App

open CloudFlareWorkers

let worker (request: IHttpRequest) =
    async {
        match request.method, request.path with
        | HttpMethod.GET, "/" ->
            return Response.create(body="Hello from F# Cloudflare Worker!", status=200)
        | HttpMethod.GET, "/health" ->
            let body = """{"status":"healthy","timestamp":"2024-01-01T00:00:00Z"}"""
            let headers = Map.ofList [ "content-type", "application/json" ]
            return Response.create(body=body, status=200, headers=headers)
        | HttpMethod.GET, "/api/hello" ->
            return Response.create(body="F# + Cloudflare = Success!", status=200)
        | HttpMethod.POST, "/echo" ->
            let! body = request.body()
            return Response.create(body=body, status=200)
        | otherwise ->
            let body = """{"message":"Not Found"}"""
            let headers = Map.ofList [ "content-type", "application/json" ]
            return Response.create(body, status=404, headers=headers)
    }

Worker.initialize worker 